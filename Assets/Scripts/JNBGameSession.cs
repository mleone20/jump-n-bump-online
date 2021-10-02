using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.SceneManagement; 
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Crea un controller per una sessione <see cref="JNBGameSession"/>
/// </summary> 
public class JNBGameSession : NetworkBehaviour
{
    /// <summary>
    /// Singleton
    /// </summary>
    public static JNBGameSession Singleton { get; private set; }

    /// <summary>
    /// Data di finie della partita
    /// </summary>
    public NetworkVariableLong EndsAt = new NetworkVariableLong(new NetworkVariableSettings()
    {
        SendTickrate = 1,
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });

    public System.DateTime EndsAtDateTime => System.DateTime.FromBinary(EndsAt.Value);

    /// <summary>
    /// Possibili stati per una sessione
    /// </summary>
    public enum SessionStateEnum : byte
    {
        /// <summary>
        /// Partita non ancora iniziata
        /// </summary>
        Waiting,

        /// <summary>
        /// Partita avviata
        /// </summary>
        Playing,

        /// <summary>
        /// Partita terminata
        /// </summary>
        Finished,
    }

    /// <summary>
    /// Possibili stati per la sessione
    /// </summary>
    public NetworkVariableByte CurrentState = new NetworkVariableByte(new NetworkVariableSettings()
    { 
        SendTickrate = 1,
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });

    /// <summary>
    /// Numero di bots da crearae
    /// </summary>
    public int Bots = 0;
      
    /// <summary>
    /// Prefab del giocatore
    /// </summary>
    public GameObject PlayerPrefab;

    /// <summary>
    /// Prefab del bot
    /// </summary>
    public GameObject BotPrefab;

    /// <summary>
    /// Punti di spawns
    /// </summary>
    public Transform[] SpawnPoints;

    /// <summary>
    /// Salviamo il singleton
    /// </summary>
    private void OnEnable()
    {
        if(Singleton != null)
        {
            DestroyImmediate(this);
            return;
        }

        Singleton = this;
    }

    /// <summary>
    /// Quando viene disabilitato
    /// </summary>
    private void OnDisable()
    {
        Singleton = null;
    }

    /// <summary> 
    /// All'avvio
    /// </summary>
    private void Start()
    {
        // Avvia la sessione di gioco
        if (IsServer || IsHost)
        {
            // Crea la sessione di gioco
            EndsAt.Value = System.DateTime.UtcNow.AddMinutes(3).ToBinary();
            CurrentState.Value = (byte)SessionStateEnum.Waiting;

            // Effettua lo spawn dei giocatori
            SpawnPlayers();
            SpawnBots();
        }

        // Eliminiamo gli spawns
        foreach (var s in SpawnPoints)
            s.gameObject.SetActive(false);
    }
       
    /// <summary>
    /// Effettua lo spawn del player
    /// </summary>
    private void SpawnPlayers()
    {
        if(IsServer || IsHost)
        {
            // Per ogni giocatore connesso
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                // Ottieni la posizione del punto di spawn
                var position = SpawnPoints[Random.Range(0, SpawnPoints.Length - 1)].position;

                // Crea il giocatore 
                var player = Instantiate(PlayerPrefab, position, Quaternion.identity);

                // Crea il giocatore
                player.GetComponent<NetworkObject>().SpawnAsPlayerObject(client.Key, null, true);
            }


        }
    }

    /// <summary>
    /// Effettua lo spawn dei bot
    /// </summary>
    private void SpawnBots()
    {
        // Ci sono bots da inserire?
        if (Bots <= 0)
            return;

        // Aggiungiamo dei bot
        for(var i = 0; i < Bots - NetworkManager.ConnectedClients.Count; i++)
        {
            // Ottieni la posizione del punto di spawn
            var position = SpawnPoints[Random.Range(0, SpawnPoints.Length - 1)].position;

            // Crea il giocatore 
            var bot = Instantiate(BotPrefab, position, Quaternion.identity);
            bot.GetComponent<NetworkBunny>().PlayerColor.Value = Color.magenta;
            bot.GetComponent<NetworkBunny>().PlayerName.Value = "Bot" + i;

            // Crea il giocatore
            bot.GetComponent<NetworkObject>().Spawn();
        }
    }

    /// <summary>
    /// Funzione di update
    /// </summary>
    private void Update()
    {
        // Se siamo il server o l'host
        if (IsServer || IsHost)
        { 
            // Game terminato?
            if (NetworkManager.ConnectedClients.Count == 0)
                StopGame();
            else
            {
                // Partita terminata
                var endsAt = System.DateTime.FromBinary(this.EndsAt.Value);

                // Partita terminta?
                if(System.DateTime.UtcNow >= endsAt)
                {
                    // Terminiamo la partita
                    this.CurrentState.Value = (byte)SessionStateEnum.Finished;
                }
                else
                {
                    // Pronti!
                    this.CurrentState.Value = (byte)SessionStateEnum.Playing;
                }
            }
        }
    }
     

    /// <summary>
    /// Lascia la sessione di gioco
    /// </summary>
    public void StopGame()
    {
        // Se siamo il server o l'host, mandiamo tutti al menù
        if (IsServer || IsHost)
        { 
            // Torniamo al menù per tutti quanti
            var switcher = NetworkSceneManager.SwitchScene("MenuScene");
            switcher.OnComplete += timeout =>
            {
                // Ferma tutto
                NetworkManager.Singleton.Shutdown();
            };
        }
        else
        {
            // Ferma il client di gioco
            NetworkManager.Singleton.StopClient();

            // Andiamo al menù di gioco
            SceneManager.LoadScene("MenuScene");
        } 
    }

}
