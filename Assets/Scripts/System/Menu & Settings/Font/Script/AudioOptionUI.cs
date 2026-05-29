using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class AudioOptionUI : MonoBehaviour
{
    [System.Serializable]
    public class AudioOption
    {
        public AudioType audioType;
        public TextMeshProUGUI labelText;
        public TextMeshProUGUI valueText;
        public GameObject highlight;

        [Header("SLIDER")]
        public Slider slider;

        [Header("BUTTONS")]
        public Button btnLeft;
        public Button btnRight;
        public TextMeshProUGUI btnLeftText;
        public TextMeshProUGUI btnRightText;

        [Header("RESET")]
        public bool isResetButton = false; // 🔥 centang untuk entry Reset

        [Range(0, 100)]
        public int defaultValue = 100;

        [HideInInspector] public int currentValue;

        [Header("COLOR")]
        public Color normalColor = Color.white;
        public Color selectedColor = Color.black;
    }

    [Header("OPTIONS")]
    public List<AudioOption> audioOptions = new List<AudioOption>();

    [Header("STEP")]
    public int step = 5;

    [Header("MENU LINK")]
    public MenuSelector menuSelector;

    static List<AudioOptionUI> allInstances = new List<AudioOptionUI>();
    static int currentIndex = 0;

    void OnEnable()
    {
        if (!allInstances.Contains(this))
            allInstances.Add(this);

        currentIndex = 0;

        for (int i = 0; i < audioOptions.Count; i++)
        {
            AudioOption opt = audioOptions[i];
            opt.currentValue = opt.defaultValue;

            if (opt.slider != null)
            {
                opt.slider.minValue = 0;
                opt.slider.maxValue = 100;
                opt.slider.wholeNumbers = true;
                opt.slider.interactable = false;
            }

            int capturedIndex = i;

            if (opt.btnLeft != null)
            {
                opt.btnLeft.onClick.RemoveAllListeners();
                opt.btnLeft.onClick.AddListener(() =>
                {
                    currentIndex = capturedIndex;
                    ChangeValue(-1);
                });
            }

            if (opt.btnRight != null)
            {
                opt.btnRight.onClick.RemoveAllListeners();
                opt.btnRight.onClick.AddListener(() =>
                {
                    currentIndex = capturedIndex;
                    ChangeValue(1);
                });
            }

            // 🔥 kalau ini reset button, assign onClick juga
            if (opt.isResetButton && opt.btnLeft != null)
            {
                opt.btnLeft.onClick.RemoveAllListeners();
                opt.btnLeft.onClick.AddListener(() => ResetAllToDefault());
            }
        }

        if (menuSelector != null)
            menuSelector.OpenPanel_Audio();

        UpdateSelection();
        UpdateAllUI();
    }

    void OnDisable()
    {
        allInstances.Remove(this);
        currentIndex = 0;

        if (menuSelector != null)
            menuSelector.ExitAudioPanel();
    }

    void Update()
    {
        if (audioOptions.Count == 0) return;
        if (!gameObject.activeInHierarchy) return;
        if (allInstances.Count == 0 || allInstances[0] != this) return;

        // 🔥 Escape tetap jalan walau isInAudioPanel true
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

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (audioOptions[currentIndex].isResetButton)
                ResetAllToDefault();
        }

        if (!audioOptions[currentIndex].isResetButton)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
                ChangeValue(1);

            if (Input.GetKeyDown(KeyCode.LeftArrow))
                ChangeValue(-1);
        }
    }

    void Move(int dir)
    {
        currentIndex = (currentIndex + dir + audioOptions.Count) % audioOptions.Count;
        UpdateSelection();
    }

    void ChangeValue(int dir)
    {
        AudioOption opt = audioOptions[currentIndex];
        if (opt.isResetButton) return; // 🔥 skip kalau reset button

        opt.currentValue = Mathf.Clamp(opt.currentValue + (dir * step), 0, 100);
        UpdateUI(currentIndex);
        ApplySetting(opt);
    }

    void UpdateSelection()
    {
        for (int i = 0; i < audioOptions.Count; i++)
        {
            bool selected = (i == currentIndex);

            if (audioOptions[i].highlight != null)
                audioOptions[i].highlight.SetActive(selected);

            Color targetColor = selected ? audioOptions[i].selectedColor : audioOptions[i].normalColor;
            targetColor.a = 1f;

            if (audioOptions[i].labelText != null)
                audioOptions[i].labelText.color = targetColor;

            if (audioOptions[i].valueText != null)
                audioOptions[i].valueText.color = targetColor;

            if (audioOptions[i].btnLeftText != null)
                audioOptions[i].btnLeftText.color = targetColor;

            if (audioOptions[i].btnRightText != null)
                audioOptions[i].btnRightText.color = targetColor;
        }
    }

    void UpdateUI(int i)
    {
        AudioOption opt = audioOptions[i];

        if (opt.isResetButton) return; // 🔥 skip slider/value untuk reset button

        if (opt.valueText != null)
            opt.valueText.text = opt.currentValue.ToString();

        if (opt.slider != null)
            opt.slider.value = opt.currentValue;
    }

    void UpdateAllUI()
    {
        for (int i = 0; i < audioOptions.Count; i++)
            UpdateUI(i);
    }

    void ApplySetting(AudioOption opt)
    {
        if (opt.isResetButton) return;

        switch (opt.audioType)
        {
            case AudioType.Master:
                Debug.Log("Master Volume: " + opt.currentValue);
                break;
            case AudioType.Music:
                Debug.Log("Music Volume: " + opt.currentValue);
                break;
            case AudioType.SFX:
                Debug.Log("SFX Volume: " + opt.currentValue);
                break;
            case AudioType.Dialogue:
                Debug.Log("Dialogue Volume: " + opt.currentValue);
                break;
        }
    }

    void ResetAllToDefault()
    {
        for (int i = 0; i < audioOptions.Count; i++)
        {
            if (audioOptions[i].isResetButton) continue;
            audioOptions[i].currentValue = audioOptions[i].defaultValue;
            UpdateUI(i);
            ApplySetting(audioOptions[i]);
        }

        Debug.Log("Reset to default");
    }
}

public enum AudioType
{
    None,
    Master,
    Music,
    SFX,
    Dialogue
}