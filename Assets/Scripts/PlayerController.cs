using UnityEngine;
using Mirror;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerController : NetworkBehaviour
{
    private string _wpmKey;
    private string _accuracyKey;

    private int _typedWords = 0;
    private int _correctWords = 0;
    private float _sessionStartTime;

    private void Start()
    {
        if (MenuManager.Instance != null)
        {
            _wpmKey = MenuManager.Instance._words_per_minute_key;
            _accuracyKey = MenuManager.Instance._accuracy_key;
        }
        else
        {
            _wpmKey = "WPM";
            _accuracyKey = "Accuracy";
        }
    }

    [Header("UI local (solo este jugador ve esto)")]
    [SerializeField] private TMPro.TMP_Text inputText;
    [SerializeField] private TMPro.TMP_Text wordText;
    [SerializeField] private Button leaveGame;

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

    /// <summary>Idioma elegido por este jugador. El servidor lo usa para asignar palabras.</summary>
    [SyncVar]
    public string selectedLanguage = "english";

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
        if (HUDManager.Instance != null)
            HUDManager.Instance.ActivateSlot(slotIndex, playerName);
    }

    public override void OnStartLocalPlayer()
    {
        if (inputText != null) inputText.text = "";
        CmdSetName("Jugador " + netId);

        // Enviar al servidor el idioma que el jugador eligio en el menu
        string lang = PlayerPrefs.GetString("SelectedLanguage", "english");
        CmdSetLanguage(lang);

        _typedWords = 0;
        _correctWords = 0;
        _sessionStartTime = Time.time;

        if (leaveGame != null)
        {
            leaveGame.onClick.AddListener(OnLeaveGamePressed);
            UpdateLeaveButtonVisibility();
        }
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
        if (leaveGame != null)
            UpdateLeaveButtonVisibility();

        if (GameManager.Instance == null) return;
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
                UpdateInputTextColor();
                _waitingForServer = true;
                CmdSubmitWord(submitted);
            }
            else
            {
                if (inputText != null)
                    inputText.text += c;
            }

            UpdateInputTextColor();
        }
    }

    private void UpdateInputTextColor()
    {
        if (inputText == null)
            return;

        string typed = inputText.text;
        if (string.IsNullOrEmpty(typed))
        {
            inputText.color = Color.white;
            return;
        }

        if (!string.IsNullOrEmpty(_currentWord) && _currentWord.StartsWith(typed, System.StringComparison.OrdinalIgnoreCase))
            inputText.color = Color.green;
        else
            inputText.color = Color.red;
    }

    private void UpdateLeaveButtonVisibility()
    {
        if (leaveGame == null) return;
        leaveGame.gameObject.SetActive(SceneManager.GetActiveScene().name == "InGame");
    }

    private void OnLeaveGamePressed()
    {
        if (!isLocalPlayer || leaveGame == null)
            return;

        leaveGame.interactable = false;
        _waitingForServer = true;
        CmdRequestLeave();
    }

    [Command]
    private void CmdRequestLeave()
    {
        int leftSlot = slotIndex;
        GameManager.Instance?.UnregisterPlayer(this);
        GameManager.Instance?.RefreshHUDLeader();
        RpcDeactivatePlayerSlot(leftSlot);
        TargetReturnToMenu(connectionToClient);
    }

    [ClientRpc]
    private void RpcDeactivatePlayerSlot(int slotToDeactivate)
    {
        HUDManager.Instance?.DeactivateSlot(slotToDeactivate);
    }

    [TargetRpc]
    private void TargetReturnToMenu(NetworkConnectionToClient target)
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();

        SceneManager.LoadScene("Menu");
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

    [Command]
    private void CmdSetLanguage(string languageID) => selectedLanguage = languageID;

    // ------------------------------------------------------------------
    // TargetRPCs
    // ------------------------------------------------------------------

    [TargetRpc]
    private void TargetWordResult(NetworkConnectionToClient target, bool correct)
    {
        _waitingForServer = false;
        UpdateTypingStats(correct);
        Debug.Log(correct ? "Correcto!" : "Incorrecto.");
    }

    private void UpdateTypingStats(bool correct)
    {
        _typedWords++;
        if (correct) _correctWords++;

        float elapsedMinutes = Mathf.Max(1f / 60f, (Time.time - _sessionStartTime) / 60f);
        int wpm = Mathf.RoundToInt(_correctWords / elapsedMinutes);
        float accuracy = _typedWords > 0 ? (_correctWords / (float)_typedWords) * 100f : 0f;

        PlayerPrefs.SetInt(_wpmKey, wpm);
        PlayerPrefs.SetFloat(_accuracyKey, accuracy);
        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------------
    // Server helpers
    // ------------------------------------------------------------------

    [Server]
    public void ServerAssignNewWord()
    {
        if (WordsManager.Instance == null) return;
        _currentWord = WordsManager.Instance.GetRandomWord(selectedLanguage);
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
        GameManager.Instance?.RefreshHUDLeader();
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