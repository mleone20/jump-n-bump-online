using MLAPI;
using MLAPI.Messaging;
using MLAPI.SceneManagement;
using MLAPI.Transports.UNET;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crea una UI per la gestione del network manager
/// </summary>
public class NetworkLobbyUI : NetworkBehaviour
{
    /// <summary>
    /// Menu da mostrare
    /// </summary>
    public enum CurrentMenuEnum
    {
        /// <summary>
        /// Sceglie che cosa deve fare con questo client di gioco (server,host,client ... )
        /// </summary>
        SelectGameClientType,

        /// <summary>
        /// Siamo un server
        /// </summary>
        ServerScreen,

        /// <summary>
        /// Siamo un host
        /// </summary>
        HostScreen,

        /// <summary>
        /// Siamo un client
        /// </summary>
        ClientScreen,

        /// <summary>
        /// Customize screen
        /// </summary>
        CustomizeScreen,
    }

    /// <summary>
    /// Menù da mostrare
    /// </summary>
    private CurrentMenuEnum CurrentScreen = CurrentMenuEnum.SelectGameClientType;

    /// <summary>
    /// Container delle lobby
    /// </summary>
    public Transform LobbiesContainer;

    /// <summary>
    /// Prefab per le lobby
    /// </summary>
    public Button LobbiesPrefab;

    /// <summary>
    /// Alert da utilizzare per i messaggi importanti
    /// </summary>
    public AlertUI Alert;

    /// <summary>
    /// Siamo pronti?
    /// </summary>
    public bool IsReady;

    /// <summary>
    /// IP input
    /// </summary>
    public InputField IpInput;

    /// <summary>
    /// Lista dei giocatori connessi
    /// </summary>
    private Dictionary<ulong, bool> Players;

    private void Start()
    {
        // Setup degli eventi
        SetupEvents(true);

#if UNITY_SERVER
        StartServer();
#else
        this.IpInput.text = PlayerPrefs.GetString("ServerIp", "159.69.179.33");
#endif

        // Aggiorna lo screen
        UpdateScreen();
    }

    private void OnDisable()
    {
        SetupEvents(false);
    }

    /// <summary>
    /// Imposta gli eventi
    /// </summary>
    /// <param name="Add">Aggiunge o rimuove gli eventi</param>
    private void SetupEvents(bool Add)
    {
        if (Add)
        {
            if (!eventsAdded)
            {
                eventsAdded = true;
                NetworkManager.Singleton.OnServerStarted += OnServerStarted;
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }
        else
        {
            if (eventsAdded)
            {
                eventsAdded = false;
                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
                    NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                    NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
                }
            }
        }
    }
    private bool eventsAdded = false;

    /// <summary>
    /// Evento lanciato quando il server parte
    /// </summary>
    private void OnServerStarted()
    {
        Debug.Log("[SERVER] Server in listening");
        // Andiamo al prossimo screen
        this.CurrentScreen = IsHost ? CurrentMenuEnum.HostScreen : CurrentMenuEnum.ServerScreen;
        UpdateScreen();
    }

    /// <summary>
    /// Evento lanciato quando il client perde la connessione
    /// </summary>
    /// <param name="playerId"></param>
    private void OnClientDisconnected(ulong playerId)
    {
        if (IsClient)
        {
            Debug.Log("[CLIENT] Client di gioco disconnesso");

            // Connessi?
            this.CurrentScreen = CurrentMenuEnum.SelectGameClientType;

            // Aggiorniamo lo screen
            UpdateScreen();
        }
        else
        {
            Debug.Log("[SERVER] Client disconnesso: " + playerId);
            if (Players.ContainsKey(playerId))
                Players.Remove(playerId);
        }
    }

    /// <summary>
    /// Evento lanciato quando il client si collega con successo al server
    /// </summary>
    /// <param name="playerId"></param>
    private void OnClientConnected(ulong playerId)
    {
        if (IsClient)
        {
            Debug.Log("[CLIENT] Client di gioco connesso");

            // Connessi?
            this.CurrentScreen = CurrentMenuEnum.ClientScreen;

            // Aggiorniamo lo screen
            UpdateScreen();
        }
        else
        {
            Debug.Log("[SERVER] Nuovo client connesso: " + playerId);

            // Se il player non esisteva, aggiungilo
            if (Players.ContainsKey(playerId))
                Players.Add(playerId, false);
        }
    }

    /// <summary>
    /// Avvia l'hosting di una partita
    /// </summary>
    public void StartHost()
    {
        // Siamo un host quindi avviamo la lobby
        StartLobby();

        // Avvia l'hosting
        NetworkManager.Singleton.StartHost();
    }

    /// <summary>
    /// Avvia i giocatori
    /// </summary>
    private void StartLobby()
    {
        // Crea la lista dei giocatori
        Players = new Dictionary<ulong, bool>();
        foreach (var p in NetworkManager.Singleton.ConnectedClients)
        {
            Players.Add(p.Key, false);
        } 
    }


    /// <summary>
    /// Avvia il gioco come se fosse un client
    /// </summary>
    public void StartClient()
    { 
        // Ottieni il trasporto e impostiamo l'up
        var transport = MLAPI.NetworkManager.Singleton.GetComponent<UNetTransport>();
        transport.ConnectAddress = this.IpInput.text;
        PlayerPrefs.SetString("ServerIp", this.IpInput.text);
        PlayerPrefs.Save();

        // Avviamo il client di gioco
        Alert.Open("Connessione in corso, attendere ... ");
        NetworkManager.Singleton.StartClient();
        StartCoroutine(ConnectCoroutine(3));
    }

    /// <summary>
    /// Coroutine di connessione per il client. Gestisce il timeout
    /// </summary>
    /// <returns></returns>
    private IEnumerator ConnectCoroutine(int Timeout)
    {
        var w = new WaitForSeconds(1);
        for(var i = 0; i < Timeout && !NetworkManager.IsConnectedClient; i++)
            yield return w;

        // Non siamo connessi
        if(!NetworkManager.IsConnectedClient)
        {
            // Terminiamo la connessione
            NetworkManager.Shutdown();

            // Errore di connessione
            Alert.Open("Impossibile connettersi al server scelto.", "OK");
        }
    }

    /// <summary>
    /// Avvia il server di gioco
    /// </summary>
    public void StartServer()
    { 
        // Avvia la lobby
        StartLobby();

        // Avvia il server
        NetworkManager.Singleton.StartServer(); 
    }
     
    /// <summary>
    /// Aggiorna lo screen da mostrare
    /// </summary>
    private void UpdateScreen()
    {
        // Chiudi l'alert
        Alert.Close();

        // Scelgiamo il prossimo screen
        SelectGameClientTypeScreen.SetActive(CurrentMenuEnum.SelectGameClientType == CurrentScreen);
        switch (CurrentScreen)
        {
            case CurrentMenuEnum.CustomizeScreen:
                CustomizeScreen.SetActive(true);
                break;
            case CurrentMenuEnum.HostScreen:
                HostScreen.SetActive(true);
                break;
            case CurrentMenuEnum.ClientScreen:
                ClientScreen.SetActive(true);
                break;
            case CurrentMenuEnum.ServerScreen:
                ServerScreen.SetActive(true);
                break;
        }

        // Ready
        ReadyScreen.SetActive(IsReady);

        // Sono un host o un server?
        if (NetworkManager.IsServer || NetworkManager.IsHost)
        { 
            if(Players.Count > 0)
            {
                var allReady = Players.Where(c => c.Value).Count() == Players.Count;

                if (allReady)
                {
                    Debug.Log("READY");
                 
                    // Carichiamo la scena della mappa
                    NetworkSceneManager.SwitchScene("GameScene");
                }
            }
        }
    }

    /// <summary>
    /// Imposta il ready
    /// </summary>
    /// <param name="IsReady"></param>
    public void SetReady(bool IsReady)
    {
        // Imposta lo stato
        this.IsReady = IsReady;

        // Aggiorna lo screen
        UpdateScreen();

        // Inviamo l'RPC al server
        if (IsClient)
            SetIsReadyOnServerRpc(NetworkManager.LocalClientId, IsReady);
    }

    /// <summary>
    /// Imposta il ready state sul server
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="IsReady"></param>
    [ServerRpc(RequireOwnership = false)]
    protected void SetIsReadyOnServerRpc(ulong uid, bool IsReady)
    {
        Debug.Log("[SERVER] Client " + uid + " ready state: " + IsReady);

        if (Players.ContainsKey(uid))
            Players[uid] = IsReady;
        else
            Players.Add(uid, IsReady);

        // Aggiorna lo screen
        UpdateScreen();
    }

    public GameObject SelectGameClientTypeScreen;
    public GameObject HostScreen;
    public GameObject ClientScreen;
    public GameObject ServerScreen;
    public GameObject CustomizeScreen;
    public GameObject ReadyScreen;
}
