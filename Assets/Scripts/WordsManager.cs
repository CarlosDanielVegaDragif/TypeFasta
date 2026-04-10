using UnityEngine;
using System.Linq;
using Mirror;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// WordsManager actualizado para usar LanguageData ScriptableObjects.
/// Arrastra todos tus assets de idioma en el Inspector.
/// Solo el servidor llama GetRandomWord() — nunca el cliente directamente.
/// </summary>
public class WordsManager : NetworkBehaviour
{
    public static WordsManager Instance { get; private set; }

    [Header("Idiomas disponibles")]
    [Tooltip("Arrastra aca todos los LanguageData assets que quieras incluir")]
    public LanguageData[] languages;

    /// <summary>
    /// Devuelve una palabra aleatoria del idioma indicado por languageID.
    /// Llamar solo desde el servidor.
    /// </summary>
    public string GetRandomWord(string languageID)
    {
        var lang = languages.FirstOrDefault(l => l.languageID == languageID);

        if (lang == null)
        {
            Debug.LogWarning($"[WordsManager] Idioma '{languageID}' no encontrado. Usando el primero disponible.");
            lang = languages.FirstOrDefault();
        }

        return lang != null ? lang.GetRandomWord() : "error";
    }

    /// <summary>Devuelve todos los idiomas disponibles (para poblar el dropdown en la UI).</summary>
    public LanguageData[] GetAvailableLanguages() => languages;
}

#if UNITY_EDITOR
[CustomEditor(typeof(WordsManager))]
public class WordsManagerEditor : Editor
{
    private string _testLanguageID = "english";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        WordsManager manager = (WordsManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        _testLanguageID = EditorGUILayout.TextField("Language ID", _testLanguageID);

        if (GUILayout.Button("Get Random Word"))
        {
            string word = manager.GetRandomWord(_testLanguageID);
            Debug.Log($"[WordsManager] {_testLanguageID}: {word}");
        }
    }
}
#endif