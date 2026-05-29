using UnityEngine;
using System.Collections.Generic;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }

    private Dictionary<string, string> localizedTexts = new Dictionary<string, string>();
    private string currentLanguage = "en";

    public delegate void OnLanguageChanged();
    public static event OnLanguageChanged LanguageChanged;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load saved language, default to "en"
        string saved = PlayerPrefs.GetString("Language", "en");
        LoadLanguage(saved);
    }

    public void LoadLanguage(string languageCode)
    {
        currentLanguage = languageCode;

        // Load JSON from Resources/Languages/
        TextAsset jsonFile = Resources.Load<TextAsset>($"Languages/{languageCode}");

        if (jsonFile == null)
        {
            Debug.LogError($"[LanguageManager] File not found: Resources/Languages/{languageCode}.json");
            return;
        }

        localizedTexts = ParseJSON(jsonFile.text);

        // Save to PlayerPrefs
        PlayerPrefs.SetString("Language", languageCode);
        PlayerPrefs.Save();

        // Notify all LocalizedText components
        LanguageChanged?.Invoke();

        Debug.Log($"[LanguageManager] Language loaded: {languageCode}");
    }

    public string GetText(string key)
    {
        if (localizedTexts.TryGetValue(key, out string value))
            return value;

        Debug.LogWarning($"[LanguageManager] Key not found: {key}");
        return $"[{key}]";
    }

    public string GetCurrentLanguage()
    {
        return currentLanguage;
    }

    // Simple JSON parser (key-value flat structure)
    private Dictionary<string, string> ParseJSON(string json)
    {
        var result = new Dictionary<string, string>();

        // Remove whitespace, braces
        json = json.Trim().TrimStart('{').TrimEnd('}');

        string[] lines = json.Split('\n');

        foreach (string line in lines)
        {
            string trimmed = line.Trim().TrimEnd(',');
            if (!trimmed.Contains(":")) continue;

            int colonIndex = trimmed.IndexOf(':');
            string key = trimmed.Substring(0, colonIndex).Trim().Trim('"');
            string value = trimmed.Substring(colonIndex + 1).Trim().Trim('"');

            if (!string.IsNullOrEmpty(key))
                result[key] = value;
        }

        return result;
    }
}