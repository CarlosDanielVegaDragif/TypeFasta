using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using Mirror;

/// <summary>
/// Maneja la UI del menu. Conecta los botones con SteamLobbyManager.
/// Asignar en el Inspector los campos correspondientes.
/// </summary>
public class MenuManager : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject panelMain;
    [SerializeField] private GameObject panelLobby;

    [Header("Panel Main")]
    [SerializeField] private Button btnHost;
    [SerializeField] private TMP_Text localPlayerNameText; // Muestra tu nombre de Steam

    [Header("Panel Lobby")]
    [SerializeField] private Button btnInvite;
    [SerializeField] private Button btnStartGame;  // Solo visible para el host
    [SerializeField] private Button btnLeave;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text lobbyStatusText;

    private void Start()
    {
        // Mostrar nombre de Steam del jugador local
        if (SteamManager.Initialized && localPlayerNameText != null)
            localPlayerNameText.text = SteamFriends.GetPersonaName();

        // Bindear botones
        btnHost.onClick.AddListener(OnHostPressed);
        btnInvite.onClick.AddListener(OnInvitePressed);
        btnStartGame.onClick.AddListener(OnStartGamePressed);
        btnLeave.onClick.AddListener(OnLeavePressed);

        ShowPanel(panelMain);
    }

    private void Update()
    {
        // Actualizar contador de jugadores y visibilidad del boton de start
        if (panelLobby.activeSelf && SteamLobbyManager.Instance.InLobby)
        {
            int count = SteamMatchmaking.GetNumLobbyMembers(SteamLobbyManager.Instance.CurrentLobbyID);
            playerCountText.text = count + " / 7";

            // Solo el host ve el boton de start
            btnStartGame.gameObject.SetActive(NetworkServer.active);
            btnStartGame.interactable = count >= 2;

            lobbyStatusText.text = NetworkServer.active ? "Hosting" : "Connected";
        }
    }

    // ------------------------------------------------------------------
    // Handlers
    // ------------------------------------------------------------------

    private void OnHostPressed()
    {
        SteamLobbyManager.Instance.HostLobby();
        ShowPanel(panelLobby);
    }

    private void OnInvitePressed()
    {
        SteamLobbyManager.Instance.InviteFriends();
    }

    private void OnStartGamePressed()
    {
        var nm = NetworkManager.singleton as CustomNetworkManager;
        if (nm != null)
            nm.StartGameScene();
    }

    private void OnLeavePressed()
    {
        SteamLobbyManager.Instance.LeaveLobby();
        ShowPanel(panelMain);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private void ShowPanel(GameObject target)
    {
        panelMain.SetActive(target == panelMain);
        panelLobby.SetActive(target == panelLobby);
    }
}