using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;
using UnityEngine.EventSystems;

public class SlotSelector : MonoBehaviour
{
    public GameObject[] slotBgs;
    public TextMeshProUGUI[] slotLabels;
    public Image[] slotThumbnails;

    public Transform playerTransform;
    public LoadingScreen loadingScreen;
    public MenuSelector menuSelector;
    public PauseMenuSelector pauseMenu;

    [Header("First Selected Button")]
    public Button firstSelectedButton; // assign BtnSave1 di Inspector

    int totalSlots = 6;
    string[] slotNames = { "slot1", "slot2", "slot3", "slot4", "slot5", "slot6" };
    int thumbWidth = 320;
    int thumbHeight = 180;

    void OnEnable()
    {
        RefreshSlotLabels();
        RefreshThumbnails();
        StartCoroutine(SelectFirstNextFrame());
    }

    void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == null)
            Debug.LogWarning("Selection LOST!");
    }


    public void SelectSlot(int slotIndex)
    {
        Debug.Log("SelectSlot dipanggil: " + slotIndex);
        if (slotIndex < 0 || slotIndex >= totalSlots) return;
        DoLoad(slotIndex);

    }

    void DoLoad(int slotIndex)
    {
        string slot = slotNames[slotIndex];

        if (SaveManager.Instance == null)
        {
            Debug.LogError("[SlotSelector] SaveManager.Instance is NULL!");
            return;
        }

        if (!SaveManager.Instance.SlotExists(slot))
        {
            Debug.Log($"[SlotSelector] Slot '{slot}' kosong.");
            return;
        }

        if (pauseMenu != null) pauseMenu.isInSavePanel = false;
        menuSelector.isInContinuePanel = false;
        Time.timeScale = 1f;
        SaveManager.Instance.Load(slot, playerTransform);
        loadingScreen.StartLoading();
    }

    void RefreshSlotLabels()
    {
        if (slotLabels == null || slotLabels.Length == 0) return;

        for (int i = 0; i < totalSlots; i++)
        {
            string slot = slotNames[i];

            if (SaveManager.Instance != null && SaveManager.Instance.SlotExists(slot))
            {
                SaveData data = SaveManager.Instance.LoadRaw(slot);
                if (data != null)
                {
                    string jam = System.DateTime.Parse(data.savedAt).ToString("dd/MM/yyyy HH:mm");
                    int menit = data.playTimeSeconds / 60;
                    int detik = data.playTimeSeconds % 60;
                    slotLabels[i].text = $"{jam}\n{menit:00}:{detik:00}";
                }
            }
            else
            {
                slotLabels[i].text = "EMPTY";
            }
        }
    }

    void RefreshThumbnails()
    {
        if (slotThumbnails == null || slotThumbnails.Length == 0) return;

        for (int i = 0; i < totalSlots; i++)
        {
            string path = ThumbnailPath(slotNames[i]);

            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(thumbWidth, thumbHeight, TextureFormat.RGB24, false);
                tex.LoadImage(bytes);
                slotThumbnails[i].sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                slotThumbnails[i].color = Color.white;
            }
            else
            {
                slotThumbnails[i].sprite = null;
                slotThumbnails[i].color = new Color(0.1f, 0.1f, 0.1f, 1f);
            }
        }
    }

    IEnumerator SelectFirstNextFrame()
    {
        yield return null;
        Debug.Log("Coroutine jalan, firstSelectedButton: " + (firstSelectedButton != null ? firstSelectedButton.name : "NULL"));
        if (firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
            Debug.Log("Selected: " + EventSystem.current.currentSelectedGameObject?.name);
        }
    }

    string ThumbnailPath(string slot) =>
        Path.Combine(Application.persistentDataPath, "saves", slot + "_thumb.png");
}