using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine;

/// <summary>
/// Rappresenta un bunny sulla rete
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(AudioSource))]
public abstract class NetworkBunny : NetworkBehaviour
{
    /// <summary>
    /// Nome del giocatore
    /// </summary>
    public NetworkVariableString PlayerName = new NetworkVariableString(new NetworkVariableSettings()
    {
        WritePermission = NetworkVariablePermission.OwnerOnly,
        ReadPermission = NetworkVariablePermission.Everyone,
    });

    /// <summary>
    /// Colore del giocatore
    /// </summary>
    public NetworkVariableColor PlayerColor = new NetworkVariableColor(new NetworkVariableSettings()
    {
        WritePermission = NetworkVariablePermission.OwnerOnly,
        ReadPermission = NetworkVariablePermission.Everyone,
    });

    /// <summary>
    /// Punteggio accumulato
    /// </summary>
    public NetworkVariableInt Points = new NetworkVariableInt(new NetworkVariableSettings()
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone,
    });

    /// <summary>
    /// Siamo morti?
    /// </summary>
    public NetworkVariableBool IsDead = new NetworkVariableBool(new NetworkVariableSettings()
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone,
        SendTickrate = 5,
    });

    /// <summary>
    /// Timer di invicibilità
    /// </summary>
    private float InvicibileTimer = 2;

    /// <summary>
    /// Ultima posizione conosciuta
    /// </summary>
    protected Vector3 LatestPosition = new Vector3();

    /// <summary>
    /// Ultima velocità conosciuta
    /// </summary>
    protected Vector3 LatestVelocity = new Vector3();

    /// <summary>
    /// Suono da riprodurre quando salta
    /// </summary>
    public AudioSource Audio;

    /// <summary>
    /// Suono da riprodurre quando salta
    /// </summary>
    public AudioClip AudioClipJump;

    /// <summary>
    /// Suono da riprodurre quando si muore
    /// </summary>
    public AudioClip AudioClipDeath;

    /// <summary>
    /// Prefab dell'esplosione
    /// </summary>
    public Transform explosionParticlesPrefab;

    /// <summary>
    /// Stats UI da creare
    /// </summary>
    public BunnyStatsUI statsUIPrefab;

    /// <summary>
    /// UI creata per lo stats
    /// </summary>
    public BunnyStatsUI statsUI;

    /// <summary>
    /// Animator
    /// </summary>
    public Animator Animator;

    /// <summary>
    /// Sprite renderer
    /// </summary>
    public SpriteRenderer SpriteRenderer; 
      
    /// <summary>
    /// Registra l'hit per un giocatore
    /// </summary>
    /// <param name="playerController2D"></param>
    /// <returns></returns>
    public virtual bool HittedBy(NetworkControllerBunny playerController2D)
    {
        // Richiama il metood RemoteHitBy su l'id di questo giocatore 
        if (InvicibileTimer == 0)
        {
            // Segnamo l'hit
            if (hittedBy == 0)
            {
                // Segnamo l'id dell'hit
                hittedBy = playerController2D.NetworkObjectId;

                // Esplodiamo!
                Explode(this.transform.position);

                // Disabilitiamo l'oggetto  
                SpriteRenderer.enabled = false;

                // Invochiamo l'RPC lato client
                SendHitToAllClientRpc(hittedBy, NetworkManager.NetworkTime, this.transform.position);

                // Segnamo il respawn tra 3 secondi
                Invoke(nameof(Respawn), 3);

                // OK!
                return true;
            }
        }

        // Non segnalato, hit non valido.
        return false;
    }
    private ulong hittedBy = 0;

    /// <summary>
    /// Notifica che siamo stati colpiti e dobbiamo esplodere. Questa notifica proviene dal server.
    /// </summary>
    /// <param name="playerController2D"></param>
    [ClientRpc]
    public virtual void SendHitToAllClientRpc(ulong FromId, float Time, Vector3 Position)
    {
        // Reset dell'hit
        if (hittedBy == FromId)
            hittedBy = 0;

        // Esplodiamo!
        Explode(this.transform.position);
         
        // Non più morto
        if (IsServer || IsHost)
        {
            // Ottieni la posizione del punto di spawn
            var position = JNBGameSession.Singleton.SpawnPoints[UnityEngine.Random.Range(0, JNBGameSession.Singleton.SpawnPoints.Length - 1)].position;

            // Andiamo in quel punto
            this.transform.position = position; 
        }

        // Segnamo il respawn tra 1 secondo
        Invoke(nameof(Respawn), (NetworkManager.NetworkTime - Time) + 3);
    }

    /// <summary>
    /// Effettua il respawn
    /// </summary>
    /// <param name="FromId"></param>
    protected virtual void Respawn()
    {
        // Reset dell'hit
        hittedBy = 0;

        // Diamo un secondo di invicibilità
        InvicibileTimer += 1;

        // Non più morto
        if (IsServer || IsHost)
        { 
            // Non più morto :)
            IsDead.Value = false;
        }

        // Disabilitiamo l'oggetto  
        SpriteRenderer.enabled = true;

    }

    /// <summary>
    /// Crea l'effetto dell'esplosione
    /// </summary>
    public virtual void Explode(Vector3 Position)
    {
        // Morto!
        if (IsServer || IsHost)
            this.IsDead.Value = true;

        // Riproduci il suono
        Audio.PlayOneShot(AudioClipDeath);

        // Crea l'esplosione
        if (explosionParticlesPrefab != null)
            Instantiate(explosionParticlesPrefab, Position, Quaternion.identity);

        // Disabilitiamo lo sprite rendere
        SpriteRenderer.enabled = false;
    }

    /// <summary>
    /// Riproducio l'audio del salto
    /// </summary>
    protected void PlayAudioJump()
    {
        // Riproduci il suono
        Audio.PlayOneShot(AudioClipJump);
    }

    /// <summary>
    /// Alla distruzione, clear degli oggetti collegati
    /// </summary>
    private void OnDestroy()
    {
        if (this.statsUI != null)
            GameObject.Destroy(this.statsUI.gameObject);
    }

    /// <summary>
    /// Aggiorna il coniglietto
    /// </summary>
    protected void Update()
    {
        // Aggiorna l'apparenza
        this.name = this.PlayerName.Value;
        this.SpriteRenderer.color = PlayerColor.Value;

        // Richiede l'aggiornamento locale
        BeforeUpdate();

        // Aggiorna l'input
        UpdateInput();

        // Se è un giocatore remoto
        if (IsLocalPlayer)
        {
            // Richiede l'aggiornamento locale
            UpdateLocal();
        }
        else
        {
            // Aggiorna il remote
            UpdateRemote();
        }

        // Invicibilità
        if (InvicibileTimer > 0)
        {
            // Aggiornaimo il timer di invicibilità
            InvicibileTimer -= Time.deltaTime;
            if (InvicibileTimer > 0)
                SpriteRenderer.color = new Color(SpriteRenderer.color.r, SpriteRenderer.color.g, SpriteRenderer.color.b, 0.5f);
            else
            {
                SpriteRenderer.color = new Color(SpriteRenderer.color.r, SpriteRenderer.color.g, SpriteRenderer.color.b, 1);
                InvicibileTimer = 0;
            }
        };

        // Salviamo l'ultima posizione
        LatestPosition.x = this.transform.position.x;
        LatestPosition.y = this.transform.position.y;
        LatestPosition.z = this.transform.position.z;
    }

    /// <summary>
    /// Fase iniziale dell'update
    /// </summary>
    protected virtual void BeforeUpdate()
    {
        // 
    }


    /// <summary>
    /// Aggiorna l'input per questo coniglietto
    /// </summary>
    protected virtual void UpdateInput()
    {
        // 
    }

    /// <summary>
    /// Update eseguito quando è un'istanza locale
    /// </summary>
    protected virtual void UpdateLocal()
    {
        // 
    }

    /// <summary>
    /// Update eseguito quando è un'istanza remota
    /// </summary>
    protected virtual void UpdateRemote()
    {
        // 
    }
}
