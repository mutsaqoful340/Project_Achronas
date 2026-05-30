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
        LanguageManager.LanguageChanged += UpdateText;
        UpdateText();
    }

    void OnDisable()
    {
        LanguageManager.LanguageChanged -= UpdateText;
    }

    void UpdateText()
    {
        if (textComponent == null) return;
        if (string.IsNullOrEmpty(key)) return;
        if (LanguageManager.Instance == null) return;

        string value = LanguageManager.Instance.GetText(key);
        Debug.Log($"[LocalizedText] UpdateText: {key} => {value}");
        textComponent.text = value;
    }

    public void ForceUpdate()
    {
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();

        Debug.Log($"[LocalizedText] ForceUpdate called on: {gameObject.name} | tmp: {textComponent != null} | key: {key}");

        if (textComponent == null) return;
        if (string.IsNullOrEmpty(key)) return;
        if (LanguageManager.Instance == null) return;

        string value = LanguageManager.Instance.GetText(key);
        Debug.Log($"[LocalizedText] ForceUpdate: {key} => {value}");
        textComponent.text = value;
    }
}