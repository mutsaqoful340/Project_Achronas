using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class AudioOptionUI : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public enum AudioType
    {
        None,
        Master,
        Music,
        SFX,
        Dialogue
    }

    [Header("AUDIO TYPE")]
    public AudioType audioType;

    [Header("UI")]
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI valueText;
    public TextMeshProUGUI btnLeftText;
    public TextMeshProUGUI btnRightText;
    public Slider slider;
    public GameObject highlight;

    [Header("COLOR")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.black;

    [Header("STEP")]
    public int step = 5;
    public int defaultValue = 100;

    [Header("RESET")]
    public bool isResetButton = false;

    [Header("MENU LINK")]
    public MenuSelector menuSelector;

    int currentValue = 100;
    private bool isInitializing = false;

    private InputActions inputActions;
    private Button button;
    private bool horizontalPressProcessed = false;

    void OnEnable()
    {
        isInitializing = true;

        button = GetComponent<Button>();

        if (isResetButton && button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(ResetAllInParent);
        }

        if (inputActions == null)
        {
            inputActions = new InputActions();
            inputActions.UI.Enable();
        }

        if (!isResetButton)
        {
            float saved = GetSavedVolume(audioType);
            currentValue = Mathf.RoundToInt(saved * 100f);
        }

        if (slider != null)
        {
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.wholeNumbers = true;
            slider.interactable = false;
        }

        if (highlight != null)
            highlight.SetActive(false);

        UpdateTextColors(normalColor);
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
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (highlight != null)
            highlight.SetActive(true);

        UpdateTextColors(selectedColor);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (highlight != null)
            highlight.SetActive(false);

        UpdateTextColors(normalColor);
    }

    void UpdateTextColors(Color color)
    {
        Color c = color;
        c.a = 1f;

        if (labelText != null) labelText.color = c;
        if (valueText != null) valueText.color = c;
        if (btnLeftText != null) btnLeftText.color = c;
        if (btnRightText != null) btnRightText.color = c;
    }

    void Update()
    {
        if (inputActions?.UI.enabled == true)
            HandleHorizontalInput();
    }

    private void HandleHorizontalInput()
    {
        if (isResetButton) return;

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

    public void Next()
    {
        if (isResetButton) return;
        currentValue = Mathf.Clamp(currentValue + step, 0, 100);
        UpdateUI();
        ApplySetting();
    }

    public void Previous()
    {
        if (isResetButton) return;
        currentValue = Mathf.Clamp(currentValue - step, 0, 100);
        UpdateUI();
        ApplySetting();
    }

    public void ResetToDefault()
    {
        currentValue = defaultValue;
        UpdateUI();
        ApplySetting();
    }

    void UpdateUI()
    {
        if (isResetButton) return;

        if (valueText != null)
            valueText.text = currentValue.ToString();

        if (slider != null)
            slider.value = currentValue;
    }

    void ResetAllInParent()
    {
        AudioOptionUI[] allOptions = transform.parent.GetComponentsInChildren<AudioOptionUI>();
        foreach (AudioOptionUI opt in allOptions)
        {
            if (!opt.isResetButton)
                opt.ResetToDefault();
        }
    }

    void ApplySetting()
    {
        if (isInitializing) return;
        if (isResetButton) return;
        if (AudioManager.Instance == null) return;

        float value = currentValue / 100f;

        switch (audioType)
        {
            case AudioType.Master:
                AudioManager.Instance.SetMasterVolume(value);
                break;
            case AudioType.Music:
                AudioManager.Instance.SetMusicVolume(value);
                break;
            case AudioType.SFX:
                AudioManager.Instance.SetSFXVolume(value);
                break;
            case AudioType.Dialogue:
                AudioManager.Instance.SetDialogueVolume(value);
                break;
        }
    }

    float GetSavedVolume(AudioType type)
    {
        if (AudioManager.Instance == null) return 1f;

        switch (type)
        {
            case AudioType.Master: return AudioManager.Instance.GetMasterVolume();
            case AudioType.Music: return AudioManager.Instance.GetMusicVolume();
            case AudioType.SFX: return AudioManager.Instance.GetSFXVolume();
            case AudioType.Dialogue: return AudioManager.Instance.GetDialogueVolume();
            default: return 1f;
        }
    }
}