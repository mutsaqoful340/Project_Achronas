using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI btnLeftText;
    public TextMeshProUGUI btnRightText;

    [Header("OPTIONS")]
    public string[] options;
    public string defaultValue;

    [Header("VIDEO TYPE")]
    public VideoType videoType;

    [Header("Brightness (URP)")]
    public Volume brightnessVolume;

    private ColorAdjustments colorAdjustments;

    int index = 0;
    private bool isInitializing = false;

    private InputActions inputActions;
    private bool horizontalPressProcessed = false;

    void OnEnable()
    {
        isInitializing = true;

        if (inputActions == null)
        {
            inputActions = new InputActions();
            inputActions.UI.Enable();
        }

        if (videoType == VideoType.Brightness &&
            brightnessVolume != null)
        {
            brightnessVolume.profile.TryGet(out colorAdjustments);
        }

        LoadSetting();
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

    public void Next()
    {
        if (options == null || options.Length == 0)
            return;

        index = (index + 1) % options.Length;
        UpdateUI();
    }

    public void Previous()
    {
        if (options == null || options.Length == 0)
            return;

        index = (index - 1 + options.Length) % options.Length;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (valueText != null && options.Length > 0)
            valueText.text = options[index];

        if (!isInitializing)
            ApplySetting();
    }

    void LoadSetting()
    {
        if (options == null || options.Length == 0)
            return;

        if (videoType == VideoType.Brightness)
        {
            float savedBrightness = PlayerPrefs.GetFloat("Brightness", 50);

            index = 0;
            float closest = Mathf.Abs(float.Parse(options[0]) - savedBrightness);

            for (int i = 1; i < options.Length; i++)
            {
                float d = Mathf.Abs(float.Parse(options[i]) - savedBrightness);

                if (d < closest)
                {
                    closest = d;
                    index = i;
                }
            }

            return;
        }

        string key = "Video_" + videoType;

        string saved = PlayerPrefs.GetString(key, defaultValue);

        int savedIndex = System.Array.IndexOf(options, saved);

        if (savedIndex >= 0)
            index = savedIndex;
    }

    public void ApplySetting()
    {
        if (options == null || options.Length == 0)
            return;

        string value = options[index];

        switch (videoType)
        {
            case VideoType.DisplayMode:

                string cleaned = value.Replace("×", "x").Replace(" ", "");

                string[] parts = cleaned.Split('x');

                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int width) &&
                    int.TryParse(parts[1], out int height))
                {
                    Screen.SetResolution(width, height, Screen.fullScreen);
                }

                PlayerPrefs.SetString("Video_DisplayMode", value);

                break;

            case VideoType.FrameRateLimit:

                if (value.ToLower() == "unlimited")
                    Application.targetFrameRate = -1;
                else if (int.TryParse(value, out int fps))
                    Application.targetFrameRate = fps;

                PlayerPrefs.SetString("Video_FrameRateLimit", value);

                break;

            case VideoType.VSync:

                QualitySettings.vSyncCount =
                    value.ToLower() == "on" ? 1 : 0;

                PlayerPrefs.SetString("Video_VSync", value);

                break;

            case VideoType.Brightness:

                if (float.TryParse(value, out float brightness))
                {
                    // 0 = gelap
                    // 50 = normal
                    // 100 = terang
                    float exposure = Mathf.Lerp(-2f, 2f, brightness / 100f);

                    if (colorAdjustments != null)
                    {
                        colorAdjustments.postExposure.value = exposure;
                    }

                    PlayerPrefs.SetFloat("Brightness", brightness);
                }

                break;
        }

        PlayerPrefs.Save();
    }

    public void ResetToDefault()
    {
        if (options == null || options.Length == 0)
            return;

        int defaultIndex = System.Array.IndexOf(options, defaultValue);

        if (defaultIndex >= 0)
        {
            index = defaultIndex;
            UpdateUI();
        }
    }

    public string GetValue()
    {
        if (options == null || options.Length == 0)
            return "";

        return options[index];
    }
}