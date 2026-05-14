using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class VideoOptionUI : MonoBehaviour
{
    public enum VideoType
    {
        DisplayMode,
        FrameRateLimit,
        VSync,
        Brightness
    }

    [Header("UI")]
    public TextMeshProUGUI valueText;

    [Header("OPTIONS")]
    public string[] options;
    public string defaultValue;

    [Header("VIDEO TYPE")]
    public VideoType videoType;

    int index = 0;
    private InputActions inputActions;
    private Button button;
    private bool horizontalPressProcessed = false;

    void OnEnable()
    {
        button = GetComponent<Button>();

        // Setup InputActions for horizontal navigation (left/right)
        if (inputActions == null)
        {
            inputActions = new InputActions();
            inputActions.UI.Enable();
        }

        UpdateUI();
    }

    void OnDisable()
    {
        // Cleanup InputActions
        if (inputActions != null)
        {
            inputActions.Dispose();
            inputActions = null;
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

        // Reset flag when stick returns to neutral
        if (Mathf.Abs(horizontalInput) < 0.3f)
        {
            horizontalPressProcessed = false;
            return;
        }

        // Only selected button processes input
        if (EventSystem.current.currentSelectedGameObject != gameObject)
            return;

        // Already processed this press
        if (horizontalPressProcessed)
            return;

        // Process the press
        if (horizontalInput > 0.5f)
        {
            Previous();
            horizontalPressProcessed = true;
        }
        else if (horizontalInput < -0.5f)
        {
            Next();
            horizontalPressProcessed = true;
        }
    }

    public void Next()
    {
        if (options == null || options.Length == 0) return;
        index = (index + 1) % options.Length;
        UpdateUI();
    }

    public void Previous()
    {
        if (options == null || options.Length == 0) return;
        index = (index - 1 + options.Length) % options.Length;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (valueText != null && options != null && options.Length > 0)
            valueText.text = options[index];
    }

    public void ApplySetting()
    {
        if (options == null || options.Length == 0) return;

        string value = options[index];

        switch (videoType)
        {
            case VideoType.DisplayMode:
                Debug.Log("Display Mode: " + value);
                break;

            case VideoType.FrameRateLimit:
                Debug.Log("Frame Rate: " + value);
                if (int.TryParse(value, out int fps))
                    Application.targetFrameRate = fps;
                break;

            case VideoType.VSync:
                Debug.Log("VSync: " + value);
                QualitySettings.vSyncCount = index;
                break;

            case VideoType.Brightness:
                Debug.Log("Brightness: " + value);  
                break;
        }
    }

    public void ResetToDefault()
    {
        if (options == null || options.Length == 0) return;

        int defaultIndex = System.Array.IndexOf(options, defaultValue);
        if (defaultIndex >= 0)
        {
            index = defaultIndex;
            UpdateUI();
            ApplySetting();
        }
    }

    public string GetValue()
    {
        if (options == null || options.Length == 0) return "";
        return options[index];
    }
}