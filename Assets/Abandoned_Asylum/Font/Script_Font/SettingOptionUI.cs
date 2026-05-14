using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SettingOptionUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI valueText;
    public GameObject highlight;

    [Header("OPTIONS")]
    public string[] options;

    [Header("SETTING TYPE")]
    public SettingType settingType;

    [Header("MENU LINK")]
    public MenuSelector menuSelector;

    [Header("COLOR")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.black;

    [Header("LABEL")]
    public TextMeshProUGUI labelText;

    [Header("LINKED PANEL")]
    public GameObject linkedPanel;

    static List<SettingOptionUI> allOptions = new List<SettingOptionUI>();
    static int currentIndex = 0;
    static bool isOpeningLinkedPanel = false; // 🔥 flag buat cegah reset

    int index = 0;

    // tambah static method baru
    // tambah di bawah static bool isOpeningLinkedPanel
    public static bool IsHandlingReturn()
    {
        if (allOptions.Count == 0) return false;
        if (currentIndex >= allOptions.Count) return false;
        return allOptions[currentIndex].linkedPanel != null;
    }

    void OnEnable()
    {
        if (!allOptions.Contains(this))
        {
            allOptions.Add(this);

            allOptions.Sort((a, b) =>
                a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex())
            );
        }

        UpdateSelection();
        UpdateUI();
    }

    void OnDisable()
    {
        // 🔥 kalau lagi buka linked panel, jangan reset apapun
        if (isOpeningLinkedPanel) return;

        int removedIndex = allOptions.IndexOf(this);

        if (removedIndex != -1)
            allOptions.RemoveAt(removedIndex);

        if (removedIndex < currentIndex)
            currentIndex--;

        if (allOptions.Count > 0)
        {
            currentIndex = Mathf.Clamp(currentIndex, 0, allOptions.Count - 1);
            UpdateSelection();
        }
        else
        {
            currentIndex = 0;
        }
    }

    void Start()
    {
        allOptions.Clear();
        allOptions.AddRange(FindObjectsOfType<SettingOptionUI>(true));

        allOptions.Sort((a, b) =>
            a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex())
        );

        currentIndex = 0;

        UpdateSelection();
        UpdateUI();

        Debug.Log("TOTAL SETTING: " + allOptions.Count);
    }

    void Update()
    {
        if (allOptions.Count == 0) return;
        if (!this.enabled) return;
        if (!gameObject.activeInHierarchy) return;
        if (this != GetInputOwner()) return;

        if (Input.GetKeyDown(KeyCode.DownArrow))
            Move(1);

        if (Input.GetKeyDown(KeyCode.UpArrow))
            Move(-1);

        if (Input.GetKeyDown(KeyCode.RightArrow))
            allOptions[currentIndex].Next();

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            allOptions[currentIndex].Previous();

        if (Input.GetKeyDown(KeyCode.Return))
        {
            SettingOptionUI selected = allOptions[currentIndex];
            if (selected.linkedPanel != null)
                OpenLinkedPanel(selected.linkedPanel);
        }
    }

    void OpenLinkedPanel(GameObject targetPanel)
    {
        // 🔥 aktifkan flag sebelum apapun dinonaktifkan
        isOpeningLinkedPanel = true;

        foreach (SettingOptionUI opt in allOptions)
        {
            if (opt.linkedPanel != null)
                opt.linkedPanel.SetActive(false);
        }

        targetPanel.SetActive(true);

        if (menuSelector != null)
            menuSelector.OpenPanel_Control();

        // 🔥 matikan flag setelah semua selesai
        isOpeningLinkedPanel = false;
    }

    SettingOptionUI GetInputOwner()
    {
        if (allOptions.Count == 0) return null;
        return allOptions[0];
    }

    void Move(int dir)
    {
        if (allOptions.Count == 0) return;

        currentIndex = (currentIndex + dir + allOptions.Count) % allOptions.Count;

        UpdateSelection();
    }

    void UpdateSelection()
    {
        for (int i = 0; i < allOptions.Count; i++)
        {
            bool selected = (i == currentIndex);

            if (allOptions[i].highlight != null)
                allOptions[i].highlight.SetActive(selected);

            if (allOptions[i].valueText != null)
            {
                allOptions[i].valueText.color = selected
                    ? allOptions[i].selectedColor
                    : allOptions[i].normalColor;
            }

            if (allOptions[i].labelText != null)
            {
                allOptions[i].labelText.color = selected
                    ? allOptions[i].selectedColor
                    : allOptions[i].normalColor;
            }
        }
    }

    public void Next()
    {
        if (options == null || options.Length == 0) return;

        UseSetting();
        index = (index + 1) % options.Length;
        UpdateUI();
        ReleaseSetting();
    }

    public void Previous()
    {
        if (options == null || options.Length == 0) return;

        UseSetting();
        index = (index - 1 + options.Length) % options.Length;
        UpdateUI();
        ReleaseSetting();
    }

    void UseSetting()
    {
        if (menuSelector != null)
            menuSelector.isInSetting = true;
    }

    void ReleaseSetting()
    {
        if (menuSelector != null)
            menuSelector.isInSetting = false;
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

        string value = options[index];

        switch (settingType)
        {
            case SettingType.Language:
                Debug.Log("Language: " + value);
                break;

            case SettingType.Subtitle:
                Debug.Log("Subtitle: " + value);
                break;

            case SettingType.Vibration:
                Debug.Log("Vibration: " + value);
                break;
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