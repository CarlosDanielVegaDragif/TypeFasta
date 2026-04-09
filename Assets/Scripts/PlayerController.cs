using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class PlayerController : NetworkBehaviour
{
    [Header("UI local (solo este jugador ve esto)")]
    [SerializeField] private TMPro.TMP_Text inputText;
    [SerializeField] private TMPro.TMP_Text wordText;

    // ------------------------------------------------------------------
    // SyncVars
    // ------------------------------------------------------------------

    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName = "Jugador";

    [SyncVar(hook = nameof(OnScoreChanged))]
    private int _score = 0;
    public int Score => _score;

    [SyncVar(hook = nameof(OnSlotAssigned))]
    public int slotIndex = -1;

    [SyncVar(hook = nameof(OnCurrentWordChanged))]
    private string _currentWord = "";

    private bool _waitingForServer = false;

    // ------------------------------------------------------------------
    // Mirror lifecycle
    // ------------------------------------------------------------------

    public override void OnStartServer()
    {
        // Esperar un frame para que NetworkGameManager.Instance este listo
        StartCoroutine(RegisterNextFrame());
    }

    private System.Collections.IEnumerator RegisterNextFrame()
    {
        yield return null;
        GameManager.Instance?.RegisterPlayer(this);
    }

    public override void OnStopServer()
    {
        GameManager.Instance?.UnregisterPlayer(this);
    }

    public override void OnStartClient()
    {
        // Si el jugador spawneo despues de que el slot ya fue asignado,
        // el hook no dispara — activar el slot manualmente.
        Debug.Log($"[Client] OnStartClient - slotIndex: {slotIndex}, HUDManager: {HUDManager.Instance}");
        if (slotIndex >= 0)
            StartCoroutine(ActivateSlotNextFrame());
    }

    private System.Collections.IEnumerator ActivateSlotNextFrame()
    {
        // Esperar a que HUDManager.Instance este disponible
        float timeout = 3f;
        while (HUDManager.Instance == null && timeout > 0f)
        {
            yield return null;
            timeout -= Time.deltaTime;
        }
        Debug.Log($"[Client] ActivateSlot - slot: {slotIndex}, nombre: {playerName}, HUD: {HUDManager.Instance}");
        if (HUDManager.Instance != null)
            HUDManager.Instance.ActivateSlot(slotIndex, playerName);
        else
            Debug.LogError("[Client] HUDManager sigue siendo null despues de 3 segundos!");
    }

    public override void OnStartLocalPlayer()
    {
        if (inputText != null) inputText.text = "";
        CmdSetName("Jugador " + netId);
    }

    public override void OnStopClient()
    {
        if (slotIndex >= 0)
            HUDManager.Instance?.DeactivateSlot(slotIndex);
    }

    // ------------------------------------------------------------------
    // Input
    // ------------------------------------------------------------------

    private void Update()
    {
        if (!isLocalPlayer) return;
        if (GameManager.Instance == null) return;
        //If this is not the InGameScene return
        if (SceneManager.GetActiveScene().name != "InGame") return;

        if (isServer && Input.GetKeyDown(KeyCode.F2))
        {
            GameManager.Instance.StartGame();
            return;
        }

        if (GameManager.Instance.CurrentState != GameManager.GameState.InGame) return;
        if (!Input.anyKeyDown) return;

        foreach (char c in Input.inputString)
        {
            if (c == '\b')
            {
                if (inputText != null && inputText.text.Length > 0)
                    inputText.text = inputText.text[..^1];
            }
            else if (c == '\n' || c == '\r')
            {
                if (_waitingForServer || inputText == null || inputText.text.Length == 0) return;
                string submitted = inputText.text.Trim().ToLower();
                inputText.text = "";
                _waitingForServer = true;
                CmdSubmitWord(submitted);
            }
            else
            {
                if (inputText != null) inputText.text += c;
            }
        }
    }

    // ------------------------------------------------------------------
    // Commands
    // ------------------------------------------------------------------

    [Command]
    private void CmdSubmitWord(string submitted)
    {
        if (GameManager.Instance == null ||
            GameManager.Instance.CurrentState != GameManager.GameState.InGame)
        {
            TargetWordResult(connectionToClient, false);
            return;
        }

        bool correct = submitted.Equals(_currentWord, System.StringComparison.OrdinalIgnoreCase);

        if (correct) { _score += 100; ServerAssignNewWord(); }
        else           _score = Mathf.Max(0, _score - 50);

        GameManager.Instance.NotifyScoreChanged();
        TargetWordResult(connectionToClient, correct);
    }

    [Command]
    private void CmdSetName(string newName) => playerName = newName;

    // ------------------------------------------------------------------
    // TargetRPCs
    // ------------------------------------------------------------------

    [TargetRpc]
    private void TargetWordResult(NetworkConnectionToClient target, bool correct)
    {
        _waitingForServer = false;
        Debug.Log(correct ? "Correcto!" : "Incorrecto.");
    }

    // ------------------------------------------------------------------
    // Server helpers
    // ------------------------------------------------------------------

    [Server]
    public void ServerAssignNewWord()
    {
        if (WordsManager.Instance == null) return;
        _currentWord = WordsManager.Instance.GetRandomWord(WordsManager.Instance.currentLanguage);
    }

    // ------------------------------------------------------------------
    // SyncVar hooks
    // ------------------------------------------------------------------

    private void OnSlotAssigned(int oldIndex, int newIndex)
    {
        if (newIndex < 0) return;
        StartCoroutine(ActivateSlotNextFrame());
    }

    private void OnScoreChanged(int oldVal, int newVal)
    {
        if (slotIndex < 0) return;
        HUDManager.Instance?.UpdateSlot(slotIndex, playerName, newVal);
        // Ya no llamar RefreshHUDLeader acá — el servidor lo hace via RpcSetLeader
    }

    private void OnNameChanged(string oldName, string newName)
    {
        if (slotIndex < 0) return;
        HUDManager.Instance?.UpdateSlot(slotIndex, newName, _score);
    }

    private void OnCurrentWordChanged(string oldWord, string newWord)
    {
        if (!isLocalPlayer) return;
        if (wordText != null) wordText.text = newWord;
    }
}