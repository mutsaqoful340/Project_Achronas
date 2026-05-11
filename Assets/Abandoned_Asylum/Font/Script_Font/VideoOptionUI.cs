using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class VideoOptionUI : MonoBehaviour
{
    public enum VideoType
    {
        None,
        DisplayMode,
        FrameRateLimit,
        VSync,
        Brightness,
        Apply,
        ResetToDefault
    }

    [System.Serializable]
    public class VideoOption
    {
        public string label;
        public VideoType videoType;

        [Header("UI")]
        public TextMeshProUGUI labelText;
        public TextMeshProUGUI valueText;
        public GameObject highlight;

        [Header("BUTTONS")]
        public Button btnLeft;
        public Button btnRight;
        public TextMeshProUGUI btnLeftText;
        public TextMeshProUGUI btnRightText;

        [Header("OPTIONS")]
        public string[] options; // isi pilihan

        [Header("COLOR")]
        public Color normalColor = Color.white;
        public Color selectedColor = Color.black;

        [HideInInspector] public int currentOptionIndex = 0;
    }

    [Header("OPTIONS")]
    public List<VideoOption> videoOptions = new List<VideoOption>();

    [Header("MENU LINK")]
    public MenuSelector menuSelector;

    static List<VideoOptionUI> allInstances = new List<VideoOptionUI>();
    static int currentIndex = 0;

    void OnEnable()
    {
        if (!allInstances.Contains(this))
            allInstances.Add(this);

        currentIndex = 0;

        for (int i = 0; i < videoOptions.Count; i++)
        {
            VideoOption opt = videoOptions[i];
            opt.currentOptionIndex = 0;

            int capturedIndex = i;

            if (opt.btnLeft != null)
            {
                opt.btnLeft.onClick.RemoveAllListeners();
                opt.btnLeft.onClick.AddListener(() =>
                {
                    currentIndex = capturedIndex;
                    HandleLeft();
                });
            }

            if (opt.btnRight != null)
            {
                opt.btnRight.onClick.RemoveAllListeners();
                opt.btnRight.onClick.AddListener(() =>
                {
                    currentIndex = capturedIndex;
                    HandleRight();
                });
            }
        }

        if (menuSelector != null)
            menuSelector.EnterVideoPanel();

        UpdateSelection();
        UpdateAllUI();
    }

    void OnDisable()
    {
        allInstances.Remove(this);
        currentIndex = 0;

        if (menuSelector != null)
            menuSelector.ExitVideoPanel();
    }

    void Update()
    {
        if (videoOptions.Count == 0) return;
        if (!gameObject.activeInHierarchy) return;
        if (allInstances.Count == 0 || allInstances[0] != this) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (menuSelector != null)
                menuSelector.GoBack();
            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
            Move(1);

        if (Input.GetKeyDown(KeyCode.UpArrow))
            Move(-1);

        if (Input.GetKeyDown(KeyCode.RightArrow))
            HandleRight();

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            HandleLeft();

        if (Input.GetKeyDown(KeyCode.Return))
            HandleReturn();
    }

    void Move(int dir)
    {
        currentIndex = (currentIndex + dir + videoOptions.Count) % videoOptions.Count;
        UpdateSelection();
    }

    void HandleLeft()
    {
        VideoOption opt = videoOptions[currentIndex];

        if (opt.videoType == VideoType.Apply || opt.videoType == VideoType.ResetToDefault) return;
        if (opt.options == null || opt.options.Length == 0) return;

        opt.currentOptionIndex = (opt.currentOptionIndex - 1 + opt.options.Length) % opt.options.Length;
        UpdateUI(currentIndex);
    }

    void HandleRight()
    {
        VideoOption opt = videoOptions[currentIndex];

        if (opt.videoType == VideoType.Apply || opt.videoType == VideoType.ResetToDefault) return;
        if (opt.options == null || opt.options.Length == 0) return;

        opt.currentOptionIndex = (opt.currentOptionIndex + 1) % opt.options.Length;
        UpdateUI(currentIndex);
    }

    void HandleReturn()
    {
        VideoOption opt = videoOptions[currentIndex];

        if (opt.videoType == VideoType.Apply)
            ApplyAllSettings();
        else if (opt.videoType == VideoType.ResetToDefault)
            ResetAllToDefault();
    }

    void UpdateSelection()
    {
        for (int i = 0; i < videoOptions.Count; i++)
        {
            bool selected = (i == currentIndex);

            if (videoOptions[i].highlight != null)
                videoOptions[i].highlight.SetActive(selected);

            Color targetColor = selected ? videoOptions[i].selectedColor : videoOptions[i].normalColor;
            targetColor.a = 1f;

            if (videoOptions[i].labelText != null)
                videoOptions[i].labelText.color = targetColor;

            if (videoOptions[i].valueText != null)
                videoOptions[i].valueText.color = targetColor;

            if (videoOptions[i].btnLeftText != null)
                videoOptions[i].btnLeftText.color = targetColor;

            if (videoOptions[i].btnRightText != null)
                videoOptions[i].btnRightText.color = targetColor;
        }
    }

    void UpdateUI(int i)
    {
        VideoOption opt = videoOptions[i];

        if (opt.videoType == VideoType.Apply || opt.videoType == VideoType.ResetToDefault) return;

        if (opt.valueText != null && opt.options != null && opt.options.Length > 0)
            opt.valueText.text = opt.options[opt.currentOptionIndex];
    }

    void UpdateAllUI()
    {
        for (int i = 0; i < videoOptions.Count; i++)
            UpdateUI(i);
    }

    void ApplyAllSettings()
    {
        foreach (VideoOption opt in videoOptions)
        {
            if (opt.options == null || opt.options.Length == 0) continue;

            string val = opt.options[opt.currentOptionIndex];

            switch (opt.videoType)
            {
                case VideoType.DisplayMode:
                    Debug.Log("Display Mode: " + val);
                    break;

                case VideoType.FrameRateLimit:
                    Debug.Log("Frame Rate: " + val);
                    if (int.TryParse(val, out int fps))
                        Application.targetFrameRate = fps;
                    break;

                case VideoType.VSync:
                    Debug.Log("VSync: " + val);
                    QualitySettings.vSyncCount = opt.currentOptionIndex;
                    break;

                case VideoType.Brightness:
                    Debug.Log("Brightness: " + val);
                    break;
            }
        }

        Debug.Log("Settings Applied!");
    }

    void ResetAllToDefault()
    {
        for (int i = 0; i < videoOptions.Count; i++)
        {
            VideoOption opt = videoOptions[i];
            if (opt.videoType == VideoType.Apply || opt.videoType == VideoType.ResetToDefault) continue;
            opt.currentOptionIndex = 0;
            UpdateUI(i);
        }

        Debug.Log("Video Reset to Default");
    }
}