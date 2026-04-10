using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI del selector de idioma en el menu principal.
/// Tiene dos botones de flecha (anterior/siguiente) y un texto con el idioma actual.
/// Guarda la seleccion en PlayerPrefs para que persista entre sesiones.
/// </summary>
public class LanguageSelector : MonoBehaviour
{
    [SerializeField] private WordsManager _wordsManager;

    [Header("UI")]
    [SerializeField] private TMP_Text languageNameText;
    [SerializeField] private Button   btnPrevious;
    [SerializeField] private Button   btnNext;

    private LanguageData[] _languages;
    private int _currentIndex = 0;

    public string SelectedLanguageID => _languages != null && _languages.Length > 0
        ? _languages[_currentIndex].languageID
        : "english";

    private void Start()
    {
        _languages = _wordsManager.GetAvailableLanguages();

        Debug.Log("Languages count: " + (_languages == null ? "NULL" : _languages.Length));

        if (_languages == null || _languages.Length == 0)
        {
            Debug.LogWarning("[LanguageSelector] No se encontraron idiomas en WordsManager.");
            return;
        }

        // Restaurar seleccion guardada
        string saved = PlayerPrefs.GetString("SelectedLanguage", _languages[0].languageID);
        _currentIndex = System.Array.FindIndex(_languages, l => l.languageID == saved);
        if (_currentIndex < 0) _currentIndex = 0;

        btnPrevious.onClick.AddListener(Previous);
        btnNext.onClick.AddListener(Next);

        Refresh();
    }

    private void Previous()
    {
        _currentIndex = (_currentIndex - 1 + _languages.Length) % _languages.Length;
        Refresh();
    }

    private void Next()
    {
        _currentIndex = (_currentIndex + 1) % _languages.Length;
        Refresh();
    }

    private void Refresh()
    {
        languageNameText.text = _languages[_currentIndex].displayName;
        PlayerPrefs.SetString("SelectedLanguage", _languages[_currentIndex].languageID);
    }
}