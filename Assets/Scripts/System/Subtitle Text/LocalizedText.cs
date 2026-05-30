using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    [Header("KEY")]
    public string key;

    [SerializeField] private TextMeshProUGUI textComponent;

    void Awake()
    {
        if (textComponent == null)
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
        textComponent.text = value;
    }

    public void ForceUpdate()
    {
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();

        if (textComponent == null) return;
        if (string.IsNullOrEmpty(key)) return;
        if (LanguageManager.Instance == null) return;

        string value = LanguageManager.Instance.GetText(key);
        textComponent.text = value;
    }
}