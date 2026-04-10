using UnityEngine;

/// <summary>
/// ScriptableObject que representa un idioma con su lista de palabras.
/// Para agregar un idioma: click derecho en Project -> Create -> TypeFasta -> Language
/// </summary>
[CreateAssetMenu(fileName = "NewLanguage", menuName = "TypeFasta/Language")]
public class LanguageData : ScriptableObject
{
    [Tooltip("Identificador interno, en minusculas. Ej: english, spanish, french")]
    public string languageID;

    [Tooltip("Nombre que se muestra en la UI. Ej: English, Español, Français")]
    public string displayName;

    public string[] words;

    public string GetRandomWord()
    {
        if (words == null || words.Length == 0)
        {
            Debug.LogWarning($"[LanguageData] El idioma '{languageID}' no tiene palabras.");
            return "error";
        }
        return words[Random.Range(0, words.Length)];
    }
}