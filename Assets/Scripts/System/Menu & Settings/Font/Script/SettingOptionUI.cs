using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingOptionUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI valueText;

    [Header("OPTIONS")]
    public string[] options;

    [Header("SETTING TYPE")]
    public SettingType settingType;

    [Header("MENU LINK")]
    public MenuSelector menuSelector;

    [Header("LABEL")]
    public TextMeshProUGUI labelText;

    [Header("LINKED PANEL")]
    public GameObject linkedPanel;

    int index = 0;
    private bool isInitializing = false;

    private InputActions inputActions;
    private Button button;
    private bool horizontalPressProcessed = false;

    void OnEnable()
    {
        isInitializing = true;

        button = GetComponent<Button>();

        if (inputActions == null)
        {
            inputActions = new InputActions();
            inputActions.UI.Enable();
        }

        if (button != null && linkedPanel != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        // Sync index with saved language on enable
        if (settingType == SettingType.Language && LanguageManager.Instance != null)
        {
            string savedLang = LanguageManager.Instance.GetCurrentLanguage();
            for (int i = 0; i < options.Length; i++)
            {
                if (OptionsToLanguageCode(options[i]) == savedLang)
                {
                    index = i;
                    break;
                }
            }
        }

        UpdateUI();

        isInitializing = false;
    }

    void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Dispose();
            inputActions = null;
        }

        if (button != null && linkedPanel != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }

    void Update()
    {
        if (inputActions?.UI.enabled == true)
            HandleHorizontalInput();
    }

    private void HandleHorizontalInput()
    {
        float horizontalInput = inputActions.UI.Navigate.ReadValue<Vector2>().x;

        if (Mathf.Abs(horizontalInput) < 0.3f)
        {
            horizontalPressProcessed = false;
            return;
        }

        if (EventSystem.current.currentSelectedGameObject != gameObject)
            return;

        if (horizontalPressProcessed)
            return;

        if (horizontalInput > 0.5f)
        {
            Next();
            horizontalPressProcessed = true;
        }
        else if (horizontalInput < -0.5f)
        {
            Previous();
            horizontalPressProcessed = true;
        }
    }

    private void OnButtonClicked()
    {
        if (linkedPanel != null)
            OpenLinkedPanel(linkedPanel);
    }

    void OpenLinkedPanel(GameObject targetPanel)
    {
        SettingOptionUI[] allOptions = transform.parent.GetComponentsInChildren<SettingOptionUI>();
        foreach (SettingOptionUI opt in allOptions)
        {
            if (opt.linkedPanel != null)
                opt.linkedPanel.SetActive(false);
        }

        targetPanel.SetActive(true);

        if (menuSelector != null)
            menuSelector.OpenPanel_Control();
    }

    public void Next()
    {
        if (options == null || options.Length == 0) return;
        if (menuSelector != null) menuSelector.isInSetting = true;
        index = (index + 1) % options.Length;
        UpdateUI();
        if (menuSelector != null) menuSelector.isInSetting = false;
    }

    public void Previous()
    {
        if (options == null || options.Length == 0) return;
        if (menuSelector != null) menuSelector.isInSetting = true;
        index = (index - 1 + options.Length) % options.Length;
        UpdateUI();
        if (menuSelector != null) menuSelector.isInSetting = false;
    }

    void UpdateUI()
    {
        if (valueText != null && options != null && options.Length > 0)
            valueText.text = options[index];

        ApplySetting();
    }

    void ApplySetting()
    {
        if (options == null || options.Length == 0) return;

        // Skip applying settings during initialization
        if (isInitializing) return;

        string value = options[index];

        switch (settingType)
        {
            case SettingType.Language:
                if (LanguageManager.Instance != null)
                {
                    string langCode = OptionsToLanguageCode(value);
                    LanguageManager.Instance.LoadLanguage(langCode);
                }
                break;

            case SettingType.Subtitle:
                Debug.Log("Subtitle: " + value);
                break;

            case SettingType.Vibration:
                Debug.Log("Vibration: " + value);
                break;
        }
    }

    private string OptionsToLanguageCode(string optionValue)
    {
        switch (optionValue.ToLower())
        {
            case "english": return "en";
            case "indonesia":
            case "indonesian": return "id";
            default: return "en";
        }
    }

    public string GetValue()
    {
        if (options == null || options.Length == 0) return "";
        return options[index];
    }
}

public enum SettingType
{
    Language,
    Subtitle,
    Vibration,
    Control
}