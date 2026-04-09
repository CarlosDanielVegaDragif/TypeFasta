using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Existe en cada cliente. Tiene referencia a los 7 slots del HUD.
/// NetworkPlayerController llama a UpdateSlot() cuando cambian sus SyncVars.
/// El servidor asigna el slotIndex a cada jugador al conectarse.
/// </summary>
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [System.Serializable]
    public class PlayerSlot
    {
        public GameObject root;          // El GameObject raiz del slot (para mostrar/ocultar)
        public TMP_Text playerNameText;
        public TMP_Text scoreText;
        public Image background;         // Opcional: para colorear al jugador lider
    }

    [Header("Slots del HUD (0 a 6)")]
    public PlayerSlot[] slots = new PlayerSlot[7];

    [Header("Colores")]
    public Color defaultColor = Color.white;
    public Color leaderColor  = Color.yellow;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        // Desactivar todos los slots aquí, en Awake, antes de que Mirror spawneé jugadores
        foreach (var slot in slots)
            if (slot.root != null) slot.root.SetActive(false);
    }

    /// <summary>Activar un slot y asignarle nombre inicial.</summary>
    public void ActivateSlot(int index, string playerName)
    {
        if (!IsValidIndex(index)) return;
        Debug.Log($"[HUD] ActivateSlot {index} - root: {slots[index].root}, root null: {slots[index].root == null}");
        slots[index].root.SetActive(true);
        slots[index].playerNameText.text = playerName;
        slots[index].scoreText.text      = "0";
        slots[index].background.color    = defaultColor;
    }

    /// <summary>Ocultar un slot (jugador desconectado).</summary>
    public void DeactivateSlot(int index)
    {
        if (!IsValidIndex(index)) return;
        slots[index].root.SetActive(false);
    }

    /// <summary>Actualizar nombre y puntaje de un slot.</summary>
    public void UpdateSlot(int index, string playerName, int score)
    {
        if (!IsValidIndex(index)) return;
        slots[index].playerNameText.text = playerName;
        slots[index].scoreText.text      = score.ToString();
    }

    /// <summary>Resaltar el slot lider. Llamar cada vez que cambia un puntaje.</summary>
    public void RefreshLeader(int leaderSlotIndex)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].root.activeSelf) continue;
            slots[i].background.color = (i == leaderSlotIndex) ? leaderColor : defaultColor;
        }
    }

    private bool IsValidIndex(int index) => index >= 0 && index < slots.Length;
}