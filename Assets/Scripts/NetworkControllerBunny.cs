using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine;

/// <summary>
/// Crea un coniglietto sulla mappa controllato da un giocatore o da un bot
/// </summary>
public class NetworkControllerBunny : NetworkBunny
{
    /// <summary>
    /// Distanza per l'attacco
    /// </summary>
    public float MinAttackDistance = 5;

    /// <summary>
    /// Controller che da gli input
    /// </summary>
    public AbstractPlayerInput InputController;

    /// <summary>
    /// Maschera per i giocatori
    /// </summary>
    public LayerMask PlayerMask;

    /// <summary>
    /// Velocità di gioco in FPS per il networking
    /// </summary>
    [Range(4, 64)]
    public int NetworkGameSpeed = 30;

    /// <summary>
    /// Velocità di gioco
    /// </summary>
    public float NetworkFrameSpeed => 1f / NetworkGameSpeed;

    /// <summary>
    /// Valore per considerare la posizione in errore rispetto al server
    /// </summary>
    public float CorrectPositionThreshold = 0.5f;

    /// <summary>
    /// Collider
    /// </summary>
    public CharacterController CharacterController;

    /// <summary>
    /// Velocità di movimento massima
    /// </summary>
    public float speed = 5;

    /// <summary>
    /// Altezza massima del salto
    /// </summary>
    public float jumpHeight = 50;


    /// <summary>
    /// Start di questo script
    /// </summary>
    protected void Start()
    {
        // Se siamo il giocatore locale
        if (IsLocalPlayer)
        {
            // Aggiorniamo i nostri parametri
            this.PlayerName.Value = PlayerPrefs.GetString("Bunny.Name", "Bunny" + this.OwnerClientId);
            this.PlayerColor.Value = new Color(PlayerPrefs.GetFloat("Bunny.Color.R", 255),
                                            PlayerPrefs.GetFloat("Bunny.Color.G", 255),
                                            PlayerPrefs.GetFloat("Bunny.Color.B", 255));
        }


        // Se siamo il server o l'hosting
        if (IsServer || IsHost)
        {
            // Assicuriamoci di avere i parametri per contattare il singolo client
            if (TargetRpc.Send.TargetClientIds == null)
            {
                TargetRpc = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { OwnerClientId }
                    }
                };
            }
        }
    }

    /// <summary>
    /// Esplosione
    /// </summary>
    /// <param name="Position"></param>
    public override void Explode(Vector3 Position)
    {
        base.Explode(Position);

        // Siamo esplosi: cancelliamo tutti gli input
        this.Inputs.Clear();
        this.ProcessingInputIndex = 0;
        this.CharacterController.enabled = false;
    }

    /// <summary>
    /// Riabilitiamo il controller quando c'è il respawn
    /// </summary>
    protected override void Respawn()
    {
        base.Respawn();
        this.CharacterController.enabled = true;
    }

    /// <summary>
    /// Aggiorna l'intelligenza di questo coniglietto
    /// </summary>
    protected override void UpdateInput()
    {
        // Aggiorna gli input
        if (CharacterController.enabled && !IsDead.Value)
        {
            // Aggiorna l'input
            var currentInput = ReadInput();

            // Questo metodo può essere chiamato più volte per frame e noi dobbiamo registrarlo una volta sola!
            var diff = currentInput.Time - LatestInput.Time;
            if (diff >= NetworkFrameSpeed) // Fps
            {
                // Abbiamo un nuovo frame
                LatestInput = currentInput;

                // Registra l'input perchè nuovo! 
                if (IsOwner)
                    Inputs.Add(LatestInput);
            }
            else if (IsLocalPlayer && IsClient)
            {
                // Dobbiamo fare un lerp all'ultima posizione ok?
                var error = Vector3.Distance(LatestInput.ResultPosition, this.transform.position);
                if (LatestInput.ResultPosition != Vector3.zero && error >= CorrectPositionThreshold)
                {
                    var diffPosition = LatestInput.ResultPosition - this.transform.position;
                    CharacterController.Move(diffPosition * NetworkFrameSpeed);
                }
            }
        }
    }
     
    /// <summary>
    /// Aggiorna l'istanza remota
    /// </summary>
    protected override void UpdateRemote()
    {
        // Aggiorniamo la posizione attuale con quella ricevuta dal server
        if (IsClient && !IsHost)
        {
            // Lerp position 
            InterpolatePosition();
        }

        // In base alle informazioni disponibili
        if (this.SpriteRenderer != null)
            if (Mathf.Abs(LatestPosition.x - this.transform.position.x) >= 0.1f)
                this.SpriteRenderer.flipX = LatestPosition.x > this.transform.position.x;

        // Aggiorniamo le direzioni
        UpdateAnimations();
    }

    /// <summary>
    /// Prima dell'aggiornamento
    /// </summary>
    protected override void BeforeUpdate()
    {
        base.BeforeUpdate();

        // Crea la UI se non esiste ancora
        if (statsUI == null)
        {
            var StatsContainerUI = GameObject.Find("StatsContainerUI");
            if (StatsContainerUI != null)
            {
                // Crea l'istanza
                statsUI = Instantiate(statsUIPrefab, StatsContainerUI.transform);

                // Setup del coniglietto
                statsUI.Player = this;
            }
        }

        // Se siamo il giocatore locale e siamo un client
        if (IsLocalPlayer && IsClient)
        {
            // Applichiamo gli input
            UpdateInputsToTime(NetworkManager.NetworkTime);
        }
    }

    /// <summary>
    /// Applica gli input
    /// </summary>
    /// <param name="Time"></param>
    public void UpdateInputsToTime(float Time)
    {
        // Calcoliamo il tempo di un frame
        var frameTime = NetworkFrameSpeed;

        // Finché ci sono input da gestire
        while (ProcessingInputIndex < Inputs.Count)
        {
            // Input da gestire
            var inputToHandle = Inputs[ProcessingInputIndex];

            // Non andiamo oltre il tempo specificato
            if (inputToHandle.Time > Time)
                break;

            // Se abbiamo più valori, possiamo calcolare il frame time
            if (Inputs.Count >= 2 && ProcessingInputIndex >= 1)
                frameTime = Mathf.Max(NetworkFrameSpeed, Inputs[ProcessingInputIndex].Time - Inputs[ProcessingInputIndex - 1].Time);

            // Applica l'input in questione e ottiene la posizione di destinazione
            ApplyInput(inputToHandle, frameTime, ref LatestVelocity);

            // Se siamo il server o l'host, conferiamo il messaggio
            if (IsServer || IsHost)
            {
                // Se siamo l'host
                if (IsHost)
                {
                    // Consideriamolo come inviato ;)
                    inputToHandle.Sent = NetworkManager.NetworkTime;
                }

                // Diamo l'ok al client
                SendInputResultToClientRpc(inputToHandle.Time, inputToHandle.ResultPosition, LatestVelocity, TargetRpc);

                // C'è da testare qualche hit?
                if (inputToHandle.NetworkObjectHitted != 0)
                    CheckForHitsOnServer(inputToHandle.Time, inputToHandle.NetworkObjectHitted);
            }
            else
            {
                // Siamo il client: invia l'input precedente al server perchè lo abbiamo appena gestito 
                if (inputToHandle.Sent == null)
                {
                    // Ovviamente lo inviamo una volta sola
                    inputToHandle.Sent = NetworkManager.NetworkTime;

                    // Inviamo l'input 
                    SendInputToServerRpc(inputToHandle.Time, inputToHandle.Horizontal, inputToHandle.WantsToJump, inputToHandle.NetworkObjectHitted);
                }
            }

            // Andiamo al prossimo indice
            ProcessingInputIndex++;
        };

        // Aggiorna le animazioni
        UpdateAnimations();
    }

    /// <summary>
    /// Invia la posizione a tutti i client di gioco
    /// </summary>
    public void SendPositionToAllClients(float Time)
    {
        SendPositionToAllClientRpc(this.transform.position, Time);
    }

    /// <summary>
    /// Invia la posizione a tutti i client
    /// </summary>
    /// <param name="oldPosition"></param>
    /// <param name="newPosition"></param>
    /// <param name="newPositionTime"></param>
    [ClientRpc]
    protected void SendPositionToAllClientRpc(Vector3 newPosition, float newPositionTime)
    {
        // Aggiunge la posizione
        RemotePositionList.Add(new RemotePositionEntry()
        {
            Position = newPosition,
            Time = newPositionTime,
        });
    }

    [ClientRpc]
    protected void SendInputResultToClientRpc(float Time, Vector3 OutputPosition, Vector3 Speed, ClientRpcParams clientRpcParams = default)
    {
        // Trovato?
        bool found = false;

        // Cerchiamo il frame giusto
        int inputIndex;
        for (inputIndex = 0; inputIndex < this.Inputs.Count; inputIndex++)
        {
            // E' il time successivo?
            if (Inputs[inputIndex].Sent.HasValue && Inputs[inputIndex].Time >= Time)
            {
                found = true;
                break;
            }
        }

        // Non ho trovato l'input inviato dal server
        if (!found)
        {
            // Se siamo morti, reset
            if (IsDead.Value)
            {
                // Andiamo in quella posizione
                this.transform.position = OutputPosition;
            }
            else
            {
                Debug.LogError("[ACK] ACK dal server al tempo " + Time + " con pos = " + OutputPosition + " ma non ho un input registrato. " + 
                                "MinTime: " + this.Inputs.Where(x => x.Sent.HasValue).Min(x => x.Sent) + " " + this.Inputs.Where(x => x.Sent.HasValue).Max(x => x.Sent));

                // Andiamo a quella posizione
                this.CharacterController.enabled = false;
                this.transform.position = OutputPosition;
                this.CharacterController.enabled = !IsDead.Value;
                this.Inputs.Where(x => x.Time >= Time);
                this.ProcessingInputIndex = 0;
            }
            return;
        }

        // Se siamo l'hoster del server questo metodo ci è inutile in quanto sappiamo che la posizione coincide
        if (IsHost)
        {
            // Consideriamo tutti gli input fino a questo punto come "OK"
            Inputs.RemoveRange(0, inputIndex);

            // Ovviamente dobbiamo scalare anche l'index per il processing
            ProcessingInputIndex -= inputIndex;
            return;
        }

        // Qui sappiamo che Inputs[inputIndex].Time è immediatamente prima di Time, capiamo di quanto
        var diff = (Time - Inputs[inputIndex].Time) / (Time);

        // Prendiamo la mia posizione tra i due frame di input, se possibile
        var inputFrom = Inputs[inputIndex];                                                         // Frame di input precedente
        var inputTo = inputIndex < Inputs.Count - 1 ? Inputs[inputIndex + 1] : Inputs[inputIndex];  // Frame input successivo (potrebbe coincidere con il from se non abbiamo ancora generato un dato)

        // Interpoliamo la posizione su diff
        var localPosition = Vector3.Lerp(inputFrom.ResultPosition, inputTo.ResultPosition, diff);

        // Adesso abbiamo la posizione di destinazione. E' simile a quella che abbiamo inviato dal server?
        var error = Vector3.Distance(localPosition, OutputPosition);

        // Siamo in errore?
        if (error >= CorrectPositionThreshold)
        {
            var str = "[ACK] \n";
            str += "ST=" + Time + "\tLT=" + inputFrom.Time + "\tL2T=" + inputTo.Time + "\n";
            str += "SP=" + OutputPosition + "\tLP=" + inputFrom.ResultPosition + "\tL2P=" + inputTo.ResultPosition + "\n";
            str += "DIFF=" + diff + "LPOS=" + localPosition + "\n";
            str += "ERR=" + error + "\n";
            Debug.Log(str);

            // Ricalcoliamo la velocità che aveva il giocatore in quell'istante
            LatestVelocity.x = Speed.x;
            LatestVelocity.y = Speed.y;
            LatestVelocity.z = Speed.z;

            // Registriamo questo punto come quello corretto da raggiungere
            inputTo.ResultPosition = OutputPosition;
            CharacterController.enabled = false;
            this.transform.position = OutputPosition;
            CharacterController.enabled = true;
            var diffToApply = (OutputPosition - localPosition);
            for (var i = inputIndex + 1; i < Inputs.Count; i++)
            {
                // Applica l'input
                Inputs[i].ResultPosition += diffToApply;
                ApplyInput(Inputs[i], NetworkFrameSpeed, ref LatestVelocity);
            }
        }

        // Consideriamo tutti gli input fino a questo punto come "OK"
        Inputs.RemoveRange(0, inputIndex);

        // Ovviamente dobbiamo scalare anche l'index per il processing
        ProcessingInputIndex -= inputIndex;
    }

    /// <summary>
    /// Aggiorna le animazioni sul controller
    /// </summary>
    void UpdateAnimations()
    {
        // Questi 3 usano il character controller
        if (IsLocalPlayer || IsHost || IsServer)
        {
            Animator.SetFloat("MovX", Mathf.Abs(this.CharacterController.velocity.x));
            Animator.SetFloat("MovY", this.CharacterController.velocity.y);
            Animator.SetBool("IsGrounded", this.CharacterController.isGrounded);
        }
        else
        {
            bool isGrounded = Physics.CheckSphere(this.transform.position + Vector3.down, 0.5f);

            // Sta usando l'interpolazione
            Animator.SetFloat("MovX", Mathf.Abs(LatestVelocity.x));
            Animator.SetFloat("MovY", LatestVelocity.y);
            Animator.SetBool("IsGrounded", isGrounded);
        }
    }

    /// <summary>
    /// Colleziona gli input da parte dell'utente e applica il movimento.  
    /// </summary>
    private ControllerInput ReadInput()
    {
        // Aggiorna l'input in questo momento 
        InputController.UpdateInput();

        // Ritorna l'input
        return new ControllerInput()
        {
            Time = NetworkManager.NetworkTime,
            Horizontal = InputController.Horizontal,
            WantsToJump = InputController.WantsToJump,
        };
    }

    /// <summary>
    /// Applica l'input in questione e salva nell'input stesso il risultato
    /// </summary>
    /// <param name="Input"></param>
    /// <param name="BaseMotion"></param>
    void ApplyInput(ControllerInput Input, float deltaTime, ref Vector3 Velocity)
    {
        // Movimento sull'asse delle X
        float moveInput = Input.Horizontal;
        if (moveInput != 0)
        {
            // Se abbiamo un movimento, camminiamo
            Velocity.x = speed * moveInput;     // Siamo a terra, ci muoviamo più velocemente

            // Modifichiamo il flip
            SpriteRenderer.flipX = moveInput < 0;
        }
        else
        {
            // Fermiamoci
            Velocity.x = 0;
        }

        // Sono a terra?
        if (CharacterController.isGrounded)
        {
            // Non deve cadere più
            Velocity.y = 0;

            // Vuole saltare?
            if (Input.WantsToJump)
            {
                // Riproduci l'audio del salto
                PlayAudioJump();

                // Applica il movimento
                Velocity.y = Mathf.Sqrt(2f * jumpHeight * (10 * Physics.gravity.magnitude));
            }
        }

        // Qui applichiamo la gravità
        Velocity.y += 10 * Physics.gravity.y * deltaTime;

        // Spostamento finale 
        if (CharacterController.enabled)
        {
            var flag = CharacterController.Move(Velocity * deltaTime);

            // Iniziamo a cadere se tocchiamo qualcosa "sopra"
            if (flag == CollisionFlags.CollidedAbove)
                Velocity.y = 0;
        }
        else
        {
            Velocity.x = 0;
            Velocity.y = 0;
        }

        // Salva la posizione
        Input.ResultPosition = this.transform.position;

        // Se siamo il giocatore locale proviamo a "sparare" (solo se stiamo cadendo)
        if (this.LatestVelocity.y < 0)
        {
            // Se è il giocatore locale e non siamo il server
            if (IsLocalPlayer && !IsServer)
                CheckForHitOnInputOnClient(Input);
            // Se è l'host
            else if (IsHost)
                CheckForHitOnInputOnClient(Input);
        }
    }

    /// <summary>
    /// Effettua il controllo per "schiacciare" gli altri giocatori.
    /// </summary>
    private void CheckForHitOnInputOnClient(ControllerInput Input)
    {
        // Calcoliamo la distanza del colpo
        var distance = Mathf.Max(MinAttackDistance, MinAttackDistance + Mathf.Abs(this.CharacterController.velocity.y) * 1 / NetworkGameSpeed);

        // Ci sono hits?
        var other = HitsTest(Input.ResultPosition, distance);

        // Se abbiamo trovato qualcosa, registriamolo
        if (other != null)
        {
            // Effettuiamo il check lato server per l'hit
            Debug.Log("[CLIENT] Richiesto al server un hit su " + other.NetworkObjectId + " al tempo " + Input.Time);
            Input.NetworkObjectHitted = other.NetworkObjectId;
        }
    }

    /// <summary>
    /// Effettua il controllo per "schiacciare" gli altri giocatori lato server.
    /// Questo metodo è più semplicistico e controlla solo se l'azione è valida.
    /// </summary> 
    private void CheckForHitsOnServer(float Time, ulong HittedNetworkId)
    {
        Debug.Log("[CLIENT] Richiesto un hit su " + HittedNetworkId + " al tempo " + Time);
        GameWorld.Instance.RunAtTime(Time, () =>
        {
            // Rollback delle posizioni
            GameWorld.Instance.RollbackPositionsAtTime(Time, () =>
            {
                // Hit
                NetworkControllerBunny other = null;

                // Solo quel giocatore?
                var netObject = GetNetworkObject(HittedNetworkId);
                if (netObject != null)
                {
                    // Prendiamo il controller
                    other = netObject.GetComponent<NetworkControllerBunny>();
                    if (other != null)
                    {
                        // La posizione era coerente con l'hit? Praticamente vediamo se siamo sopra e se siamo sulla X simile
                        if (this.transform.position.y >= other.transform.position.y &&                  // Se snoo in testa
                            Mathf.Abs(this.transform.position.y - other.transform.position.y) <= 7 &&   // Se sono abbastanza vicino
                            Mathf.Abs(this.transform.position.x - other.transform.position.x) <= 15)    // Se sono "nella stessa colonna" visivamente parlando
                        {
                            // Tutto ok!
                        }
                        else
                        {
                            other = null;
                        }
                    }
                }


                // C'è altro?
                if (other == null)
                {
                    // Calcoliamo la distanza del colpo
                    var distance = Mathf.Max(MinAttackDistance, MinAttackDistance + Mathf.Abs(this.CharacterController.velocity.y) * 1 / NetworkGameSpeed);

                    // Cerchiamo un possibile hit
                    other = HitsTest(this.transform.position, distance);
                }

                // Se abbiamo trovato qualcosa, colpiamo!
                if (other != null)
                {
                    // Colpito?
                    if (other.HittedBy(this))
                    {
                        // Diamo il punto!
                        Points.Value++;
                    }
                }
            });
        });
    }


    /// <summary>
    /// Effettua un test per l'hit e ritorna il giocatore colpito.
    /// </summary>
    /// <param name="FirePosition">Posizione dal quale parte l'attacco</param>
    /// <param name="AttackDistance">Distanza dell'attacco</param>
    /// <returns></returns>
    public NetworkControllerBunny HitsTest(Vector3 FirePosition, float AttackDistance)
    {
        // Ci sono hits?
        var direction = Vector3.down;

        // Prendiamo gli hits 
        var anyHits = Physics.SphereCastAll(FirePosition, 3f, Vector3.down, AttackDistance)
                        .Where(x => x.transform != this.transform)
                        .OrderBy(x => x.distance).ToArray();

        // C'è un hit?
        if (anyHits.Length > 0)
        {
            var other = anyHits[0].collider.gameObject;

            // E' un giocatore nemico?
            var player = other.GetComponent<NetworkControllerBunny>();
            if (player != this &&
                player != null &&
                !player.IsDead.Value)
            {
                return player;
            }

        }

        // Non ci sono hit
        return null;
    }

    /// <summary>
    /// Lato server. Riceve l'input di questo controller
    /// </summary>
    /// <param name="Input"></param>
    [ServerRpc]
    private void SendInputToServerRpc(float ClientTime, float Horizontal, bool WantsToJump, ulong NetworkHittedId)
    {
        // Ottiene l'input
        var Input = new ControllerInput() { Time = ClientTime, Horizontal = Horizontal, WantsToJump = WantsToJump, NetworkObjectHitted = NetworkHittedId };

        // Registriamo l'input
        Inputs.Add(Input);

        // Prendiamo sempre il tempo dell'input ricevuto
        if (Input.Time > MinInputTime)
            MinInputTime = Input.Time;
    }

    /// <summary>
    /// Controller input
    /// </summary>
    private class ControllerInput
    {
        /// <summary>
        /// Pacchetto inviato?
        /// </summary>
        public float? Sent = null;

        /// <summary>
        /// Tempo di gioco nel quale viene generato il pacchetto
        /// </summary>
        public float Time;

        /// <summary>
        /// Pressione del tasto orizzontale
        /// </summary>
        public float Horizontal;

        /// <summary>
        /// Vuole saltare?
        /// </summary>
        public bool WantsToJump;

        /// <summary>
        /// Posizione risultante
        /// </summary>
        public Vector3 ResultPosition;

        /// <summary>
        /// ID di un oggetto colpito
        /// </summary>
        public ulong NetworkObjectHitted;
    }

    /// <summary>
    /// Lista degli input
    /// </summary>
    private readonly List<ControllerInput> Inputs = new List<ControllerInput>();

    /// <summary>
    /// Indice dell'input da processare
    /// </summary>
    private int ProcessingInputIndex = 0;

    /// <summary>
    /// Ultimo input registrato
    /// </summary>
    private ControllerInput LatestInput = new ControllerInput();

    /// <summary>
    /// Entry per una posizione
    /// </summary>
    private class RemotePositionEntry
    {
        public Vector3 Position;
        public float Time;
    }

    /// <summary>
    /// Posizioni remote
    /// </summary>
    private List<RemotePositionEntry> RemotePositionList = new List<RemotePositionEntry>();
     
    /// <summary>
    /// Tempo dell'input meno recente
    /// </summary>
    public float MinInputTime { get; internal set; }

    /// <summary>
    /// Parametri RPC per inviare solo al possessore
    /// </summary>
    private ClientRpcParams TargetRpc;

    /// <summary>
    /// Effettua l'interpolazione della posizione
    /// </summary>
    private void InterpolatePosition()
    {
        var interpolationSpeed = NetworkFrameSpeed;

        // Abbiamo almeno 2 posizioni?
        if (RemotePositionList.Count >= 2)
        {
            for (var i = 1; i < RemotePositionList.Count; i++)
            {
                // Posizione iniziale e finale
                var fromFrame = RemotePositionList[i - 1];
                var toFrame = RemotePositionList[i];

                // Tempo di interpolazione
                var interpolationTime = NetworkManager.NetworkTime - fromFrame.Time;

                // Andiamo al prossimo frame?
                if (interpolationTime >= interpolationSpeed)
                {
                    RemotePositionList.RemoveAt(0);
                    i--;
                    continue;
                }

                // Effettuiamo il lerp e impostiamo la posizione
                bool wasEnabled = CharacterController.enabled;
                CharacterController.enabled = false;
                this.transform.position = Vector3.Lerp(fromFrame.Position, toFrame.Position, interpolationTime);
                CharacterController.enabled = wasEnabled;

                // Approssiamiamo la velocità solo se ci si sposta di un pochino
                if (Time.time - latestPositioUsedForVelocity.Time >= 0.1f)
                {
                    latestPositioUsedForVelocity.Time = Time.time;
                    this.LatestVelocity = (toFrame.Position - latestPositioUsedForVelocity.Position) / interpolationSpeed;
                    latestPositioUsedForVelocity.Position = toFrame.Position;
                }
            }
        }
    }

    private readonly RemotePositionEntry latestPositioUsedForVelocity = new RemotePositionEntry();
}