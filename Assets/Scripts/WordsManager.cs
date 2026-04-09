using UnityEngine;
using TMPro;
using Mirror;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// WordsManager adaptado para Mirror.
/// 
/// IMPORTANTE: En la versión online, WordsManager NO maneja wordText directamente
/// porque cada jugador tiene su propia palabra (via SyncVar en NetworkPlayerController).
/// wordText puede quedar como referencia opcional para el host o para una pantalla de debug.
/// 
/// Solo el SERVIDOR llama a GetRandomWord() — nunca el cliente directamente.
/// </summary>
public class WordsManager : NetworkBehaviour
{
    public static WordsManager Instance { get; private set; }

    [Header("Idioma")]
    public string currentLanguage = "english"; // "english" o "spanish"

    [Header("UI opcional (debug / pantalla del host)")]
    public TMP_Text wordText; // Puede dejarse null en la versión online

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Devuelve una palabra aleatoria del idioma indicado.
    /// Debe llamarse SOLO desde el servidor (desde NetworkPlayerController o NetworkGameManager).
    /// </summary>
    public string GetRandomWord(string language)
    {
        switch (language)
        {
            case "english":
                return english_words.GetRandomWord();
            case "spanish":
                return spanish_words.GetRandomWord();
            default:
                Debug.LogError("[WordsManager] Idioma no soportado: " + language);
                return "error";
        }
    }
}

// -----------------------------------------------------------------------
// Editor helper (sin cambios respecto al original)
// -----------------------------------------------------------------------
#if UNITY_EDITOR
[CustomEditor(typeof(WordsManager))]
public class WordsManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        WordsManager manager = (WordsManager)target;

        if (GUILayout.Button("Get Random English Word"))
        {
            string word = manager.GetRandomWord("english");
            Debug.Log("English word: " + word);
            if (manager.wordText != null) manager.wordText.text = word;
        }

        if (GUILayout.Button("Get Random Spanish Word"))
        {
            string word = manager.GetRandomWord("spanish");
            Debug.Log("Spanish word: " + word);
            if (manager.wordText != null) manager.wordText.text = word;
        }
    }
}
#endif