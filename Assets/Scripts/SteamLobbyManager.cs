using UnityEngine;
using Mirror;
using Steamworks;

/// <summary>
/// Maneja la creacion, union y cierre de lobbies de Steam.
/// Debe estar en la escena Menu junto al SteamManager y NetworkManager.
/// </summary>
public class SteamLobbyManager : MonoBehaviour
{
    public static SteamLobbyManager Instance { get; private set; }

    [Header("Configuracion")]
    [SerializeField] private int maxPlayers = 7;

    // Callbacks de Steam
    private Callback<LobbyCreated_t>          _lobbyCreated;
    private Callback<GameLobbyJoinRequested_t> _joinRequested;
    private Callback<LobbyEnter_t>             _lobbyEntered;

    public CSteamID CurrentLobbyID { get; private set; }
    public bool InLobby => CurrentLobbyID.IsValid() && CurrentLobbyID != CSteamID.Nil;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (!SteamManager.Initialized) return;

        _lobbyCreated  = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        _joinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
        _lobbyEntered  = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    // ------------------------------------------------------------------
    // API publica
    // ------------------------------------------------------------------

    /// <summary>Crear un lobby nuevo y hostear.</summary>
    public void HostLobby()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("[Steam] Steam no inicializado.");
            return;
        }

        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxPlayers);
    }

    /// <summary>Abrir el overlay de Steam para invitar amigos al lobby actual.</summary>
    public void InviteFriends()
    {
        if (!InLobby)
        {
            Debug.LogWarning("[Steam] No estas en un lobby todavia.");
            return;
        }

        SteamFriends.ActivateGameOverlayInviteDialog(CurrentLobbyID);
    }

    /// <summary>Salir del lobby y desconectar.</summary>
    public void LeaveLobby()
    {
        if (InLobby)
        {
            SteamMatchmaking.LeaveLobby(CurrentLobbyID);
            CurrentLobbyID = CSteamID.Nil;
        }

        NetworkManager.singleton.StopHost();
        NetworkManager.singleton.StopClient();
    }

    // ------------------------------------------------------------------
    // Callbacks de Steam
    // ------------------------------------------------------------------

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("[Steam] Error al crear el lobby: " + callback.m_eResult);
            return;
        }

        CurrentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        // Guardar el Steam ID del host en los datos del lobby
        // para que los clientes puedan conectarse
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, "HostSteamID",
            SteamUser.GetSteamID().ToString());

        SteamMatchmaking.SetLobbyData(CurrentLobbyID, "GameName", "TypeFasta");

        Debug.Log("[Steam] Lobby creado: " + CurrentLobbyID);

        // Arrancar el host de Mirror
        NetworkManager.singleton.StartHost();
    }

    private void OnJoinRequested(GameLobbyJoinRequested_t callback)
    {
        // El jugador aceptó una invitación desde el overlay de Steam
        Debug.Log("[Steam] Union solicitada al lobby: " + callback.m_steamIDLobby);
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        CurrentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log("[Steam] Entraste al lobby: " + CurrentLobbyID);

        // Si sos el servidor (host), no hacer nada mas — ya arrancastes con StartHost()
        if (NetworkServer.active) return;

        // Si sos cliente, obtener el Steam ID del host y conectarte
        string hostSteamIDStr = SteamMatchmaking.GetLobbyData(CurrentLobbyID, "HostSteamID");

        if (string.IsNullOrEmpty(hostSteamIDStr))
        {
            Debug.LogError("[Steam] No se encontro el Steam ID del host en el lobby.");
            return;
        }

        // FizzySteamworks usa el Steam ID como direccion de red
        NetworkManager.singleton.networkAddress = hostSteamIDStr;
        NetworkManager.singleton.StartClient();

        Debug.Log("[Steam] Conectando al host: " + hostSteamIDStr);
    }
}