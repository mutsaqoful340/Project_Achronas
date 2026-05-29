using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    [Header("KEY")]
    public string key;

    private TextMeshProUGUI textComponent;

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        // Subscribe to language change event
        LanguageManager.LanguageChanged += UpdateText;

        // Update immediately when enabled
        UpdateText();
    }

    void OnDisable()
    {
        // Unsubscribe to avoid memory leak
        LanguageManager.LanguageChanged -= UpdateText;
    }

    void UpdateText()
    {
        if (textComponent == null) return;
        if (string.IsNullOrEmpty(key)) return;
        if (LanguageManager.Instance == null) return;

        string value = LanguageManager.Instance.GetText(key);

        Debug.Log($"[LocalizedText] {key} => {value}");

        textComponent.text = value;
    }
}