using MLAPI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Rappresenta il mondo di gioco
/// </summary>
public class GameWorld : MonoBehaviour
{
    /// <summary>
    /// Istanza del mondo di gioco
    /// </summary>
    public static GameWorld Instance { get; private set; }
     
    /// <summary>
    /// Stati del mondo di gioco
    /// </summary>
    private readonly List<WorldState> States = new List<WorldState>();

    /// <summary>
    /// Rappresenta lo stato del mondo
    /// </summary>
    private class WorldState
    {
        /// <summary>
        /// Stato di un'entità all'interno del mondo
        /// </summary>
        public class PlayerState
        {
            /// <summary>
            /// ID del giocatore
            /// </summary>
            public readonly ulong PlayerId;

            /// <summary>
            /// Oggetto
            /// </summary>
            public readonly NetworkControllerBunny Controller;

            /// <summary>
            /// Character enabled?
            /// </summary>
            public readonly bool CharacterEnabled;

            /// <summary>
            /// Posizione
            /// </summary>
            public readonly Vector3 Position;

            /// <summary>
            /// Crea lo stato
            /// </summary>
            /// <param name="player"></param>
            public PlayerState(NetworkControllerBunny player)
            {
                PlayerId = player.NetworkObjectId;
                Controller = player;
                Position = player.transform.position;
                CharacterEnabled = !player.IsDead.Value; 
            }
        }

        /// <summary>
        /// Frame index
        /// </summary>
        public int Frame;

        /// <summary>
        /// Tempo di gioco
        /// </summary>
        public float Time;

        /// <summary>
        /// Stato delle entità
        /// </summary>
        public readonly List<PlayerState> Entities = new List<PlayerState>();
    }

    private void OnEnable()
    {
        lastUpdateTime = CurrentNetworkTime;

        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Network time
    /// </summary>
    private float CurrentNetworkTime => NetworkManager.Singleton.NetworkTime;

    /// <summary>
    /// Genera lo stato del mondo di gioco
    /// </summary>
    private void UpdateWorldState()
    {
        // Se abbiamo già degli stati generati 
        if (States.Count > 0)
        {
            // Aspettiamo che cambi frame
            if (Time.frameCount == States[States.Count - 1].Frame)
                return;
        }

        // Prendiamo tutti i giocatori
        var Players = GameObject.FindObjectsOfType<NetworkControllerBunny>();

        var maxTime = CurrentNetworkTime;
        var step = 1 / 60f;
        for (; lastUpdateTime < maxTime; lastUpdateTime += step)
        {
            // Applichiamo i vari input per il frame successivo
            foreach (var p in Players.OrderByDescending(x => x.MinInputTime))
                p.UpdateInputsToTime(lastUpdateTime);

            // Applichiamo gli eventi fino al tempo specificato
            for (var i = 0; i < EventsList.Count; i++)
            {
                var e = EventsList[i];

                if (e.Time <= lastUpdateTime)
                {
                    e.Callback();
                    EventsList.RemoveAt(i);
                    i--;
                }
            }

            // Crea lo stato di gioco
            WorldState state = new WorldState
            {
                Frame = Time.frameCount,
                Time = CurrentNetworkTime
            };

            // Per ogni client...
            foreach (var c in FindObjectsOfType<NetworkControllerBunny>())
            {
                // Salva l'entità nel mondo di gioco
                state.Entities.Add(new WorldState.PlayerState(c));

                // Invia la posizione a tutti i clients
                c.SendPositionToAllClients(state.Time);
            }

            // Salva lo stato
            States.Add(state);
        }

        // Lasciamo in memoria 10 secondi di gioco
        while (States.Count > 2 && States[States.Count - 1].Time - States[0].Time >= 10)
            States.RemoveAt(0);
    }
    private float lastUpdateTime = 0;

    /// <summary>
    /// Funzione di update
    /// </summary>
    private void Update()
    {

        // Se siamo il server o l'host
        if (NetworkManager.Singleton.IsServer ||
            NetworkManager.Singleton.IsHost)
        {
            // Genera lo stato di gioco 
            UpdateWorldState();

            foreach(var s in States)
            {
                foreach(var e in s.Entities)
                {
                    Debug.DrawLine(e.Position, e.Position + Vector3.down * 3, e.Controller.PlayerColor.Value, 3f);
                }
            }
        }
    }


    /// <summary>
    /// Effettua un rollback al tempo specificato. Il metodo lavora in questo modo:
    /// - cerca lo snapshot al tempo specificato
    /// - sposta tutti gli oggetti alla posizione di quello snapshot interpolata con il successivo
    /// - richiama il metodo di callback
    /// - risposta tutti gli oggetti
    /// </summary>
    /// <param name="Time"></param>
    /// <param name="Callback"></param>
    public void RollbackPositionsAtTime(float Time, System.Action Callback)
    {
        // Se siamo il client, non andiamo mai indietro nel tempo
        if (NetworkManager.Singleton.IsClient)
        {
            Callback();
            return;
        }

        // Stati precedenti
        Dictionary<GameObject, WorldState.PlayerState> oldStates = new Dictionary<GameObject, WorldState.PlayerState>();

        // Per ogni stato
        for (var i = States.Count - 1; i >= 0; i--)
        {
            // Stiamo cercando lo stato con il tempo richiesto
            if (States[i].Time > Time)
                continue;
             
            var currState = States[i];
             
            // Interpliamo tutte le posizioni
            foreach (var e in currState.Entities)
            {
                // Salviamo la posizione
                oldStates.Add(e.Controller.gameObject, new WorldState.PlayerState(e.Controller));
                 
                // Calcoliamo la nuova posizione
                var newPosition = currState.Entities.Where(x => x.PlayerId == e.PlayerId).FirstOrDefault().Position; 

                // Debug
                Debug.DrawLine(newPosition, newPosition + Vector3.down * 3, Color.red, 5f);

                // Impostiamo la posizione
                e.Controller.transform.position = newPosition;
            }

            // Eseguiamo la callbackw
            Callback();

            // Reset di tutte le posizioni
            foreach (var o in oldStates)
                o.Key.transform.position = o.Value.Position;

            // Usciamo dal ciclo perchè abbiamo finito
            return;
        }
    }

    /// <summary>
    /// Schedula l'esecuzione di una funzione per essere eseguita al tempo specificato nel tick di gioco
    /// </summary>
    /// <param name="NetworkTime"></param>
    /// <param name="Callback"></param>
    public void RunAtTime(float NetworkTime, System.Action Callback)
    {
        EventsList.Add(new EventToTriggerAtTime(NetworkTime, Callback));
    }

    /// <summary>
    /// Lista di eventi da triggerare
    /// </summary>
    private readonly List<EventToTriggerAtTime> EventsList = new List<EventToTriggerAtTime>();

    /// <summary>
    /// Rappresenta un evento da triggerare
    /// </summary>
    private class EventToTriggerAtTime
    {
        public readonly float Time;
        public readonly System.Action Callback; 

        public EventToTriggerAtTime(float Time, System.Action Callback)
        {
            this.Time = Time;
            this.Callback = Callback;
        }
    }

}