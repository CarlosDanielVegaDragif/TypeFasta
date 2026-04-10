using UnityEngine;
using Mirror;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI global (asignar en la escena InGame)")]
    public TMP_Text timerText;
    public TMP_Text gameStateText;

    [Header("Configuracion")]
    public float gameDuration = 90f;
    public int minPlayers = 2;

    public enum GameState { WaitingForPlayers, InGame, GameOver }

    [SyncVar(hook = nameof(OnTimerChanged))]
    private float _timeRemaining;

    [SyncVar(hook = nameof(OnGameStateChanged))]
    private GameState _gameState = GameState.WaitingForPlayers;

    public GameState CurrentState => _gameState;

    private readonly List<PlayerController> _players = new();
    private readonly PlayerController[] _slots = new PlayerController[7];

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public override void OnStartServer()
    {
        _gameState = GameState.WaitingForPlayers;
        _timeRemaining = gameDuration;
    }

    public override void OnStartClient()
    {
        // Reconectar referencias UI que estan en la escena (no en el prefab)
        if (timerText == null)
            timerText = GameObject.FindWithTag("TimerText")?.GetComponent<TMP_Text>();
        if (gameStateText == null)
            gameStateText = GameObject.FindWithTag("GameStateText")?.GetComponent<TMP_Text>();
    }

    // ------------------------------------------------------------------
    // Registro de jugadores
    // ------------------------------------------------------------------

    [Server]
    public void RegisterPlayer(PlayerController player)
    {
        if (_players.Contains(player)) return;
        _players.Add(player);

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
            {
                _slots[i] = player;
                player.slotIndex = i;
                Debug.Log($"[Server] Jugador {player.netId} -> slot {i}");
                break;
            }
        }
    }

    [Server]
    public void UnregisterPlayer(PlayerController player)
    {
        _players.Remove(player);
        if (player.slotIndex >= 0 && player.slotIndex < _slots.Length)
            _slots[player.slotIndex] = null;
    }

    // ------------------------------------------------------------------
    // Control de partida
    // ------------------------------------------------------------------

    public void StartGame()
    {
        // Guardia: solo el servidor ejecuta la logica
        if (!isServer) return;

        if (_gameState != GameState.WaitingForPlayers)
        {
            Debug.LogWarning("[Server] La partida ya esta en curso o termino.");
            return;
        }
        if (_players.Count < minPlayers)
        {
            Debug.LogWarning($"[Server] Faltan jugadores. Conectados: {_players.Count}, minimo: {minPlayers}");
            return;
        }

        _gameState = GameState.InGame;
        _timeRemaining = gameDuration;

        foreach (var p in _players)
            p.ServerAssignNewWord();

        StartCoroutine(TimerCoroutine());
        Debug.Log("[Server] Partida iniciada.");
    }

    [Server]
    private IEnumerator TimerCoroutine()
    {
        while (_timeRemaining > 0f)
        {
            yield return new WaitForSeconds(1f);
            _timeRemaining -= 1f;
        }
        _timeRemaining = 0f;
        EndGame();
    }

    [Server]
    private void EndGame()
    {
        _gameState = GameState.GameOver;

        PlayerController winner = null;
        int topScore = int.MinValue;
        foreach (var p in _players)
            if (p.Score > topScore) { topScore = p.Score; winner = p; }

        RpcAnnounceWinner(winner != null ? winner.playerName : "None", topScore);
    }

    // ------------------------------------------------------------------
    // Lider del HUD
    // ------------------------------------------------------------------

    public void NotifyScoreChanged() => RefreshHUDLeader();

    [Server]
    public void RefreshHUDLeader()
    {
        int leaderSlot = -1;
        int topScore = int.MinValue;

        foreach (var p in _players)
        {
            if (p.slotIndex >= 0 && p.Score > topScore)
            {
                topScore = p.Score;
                leaderSlot = p.slotIndex;
            }
        }

        RpcSetLeader(leaderSlot);
    }

    [ClientRpc]
    private void RpcSetLeader(int leaderSlotIndex)
    {
        HUDManager.Instance?.RefreshLeader(leaderSlotIndex);
    }

    // ------------------------------------------------------------------
    // RPCs
    // ------------------------------------------------------------------

    [ClientRpc]
    private void RpcAnnounceWinner(string winnerName, int score)
    {
        if (gameStateText != null)
            gameStateText.text = $"Won {winnerName} with {score} points!";
        Debug.Log($"[Client] Winner: {winnerName} with {score} pts");
    }

    // ------------------------------------------------------------------
    // SyncVar hooks
    // ------------------------------------------------------------------

    private void OnTimerChanged(float oldVal, float newVal)
    {
        if (timerText == null)
            timerText = GameObject.FindWithTag("TimerText")?.GetComponent<TMP_Text>();
        if (timerText != null)
            timerText.text = Mathf.CeilToInt(newVal).ToString();
    }

    private void OnGameStateChanged(GameState oldState, GameState newState)
    {
        if (gameStateText == null)
            gameStateText = GameObject.FindWithTag("GameStateText")?.GetComponent<TMP_Text>();
        if (gameStateText != null)
            gameStateText.text = newState switch
            {
                GameState.WaitingForPlayers => "Waiting for players...",
                GameState.InGame            => "In game!",
                GameState.GameOver          => "Game Over!",
                _                           => ""
            };
    }
}