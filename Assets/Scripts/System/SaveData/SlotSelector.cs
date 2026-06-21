using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;
using UnityEngine.EventSystems;

public class SlotSelector : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    // REFERENCES
    // ═══════════════════════════════════════════════════════════
    [Header("UI")]
    public GameObject[] slotBgs;
    public TextMeshProUGUI[] slotLabels;
    public Image[] slotThumbnails;

    [Header("Players")]
    public Transform player1Transform;
    public Transform player2Transform;

    [Header("External References")]
    public LoadingScreen loadingScreen;
    public MenuSelector menuSelector;
    public PauseMenuSelector pauseMenu;

    [Header("First Selected Button")]
    public Button firstSelectedButton;

    // ═══════════════════════════════════════════════════════════
    // PRIVATE
    // ═══════════════════════════════════════════════════════════
    private int totalSlots = 6;
    private int selectedIndex = -1;
    private string[] slotNames = { "slot1", "slot2", "slot3", "slot4", "slot5", "slot6" };
    private int thumbWidth = 320;
    private int thumbHeight = 180;

    // ═══════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════
    void OnEnable()
    {
        selectedIndex = -1;
        RefreshSlotLabels();
        RefreshThumbnails();
        StartCoroutine(SelectFirstNextFrame());
    }

    void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == null)
            Debug.LogWarning("Selection LOST!");
    }

    // ═══════════════════════════════════════════════════════════
    // PUBLIC
    // ═══════════════════════════════════════════════════════════

    /// <summary>Dipanggil dari OnClick tiap BtnSave — cuma highlight, tidak load.</summary>
    public void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= totalSlots) return;
        selectedIndex = slotIndex;
        Debug.Log($"[SlotSelector] Slot dipilih: {slotNames[slotIndex]}");
    }

    /// <summary>Dipanggil dari tombol Load di panel Continue.</summary>
    public void ConfirmLoad()
    {
        if (selectedIndex == -1)
        {
            Debug.LogWarning("[SlotSelector] Belum ada slot yang dipilih!");
            return;
        }
        DoLoad(selectedIndex);
    }

    // ═══════════════════════════════════════════════════════════
    // LOAD
    // ═══════════════════════════════════════════════════════════
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

        SaveManager.Instance.Load(slot, player1Transform, player2Transform);

        // Hide semua panel & aktifkan player
        menuSelector.DisableAll();
        menuSelector.playerMovement.enabled = true;
        menuSelector.panelHistory.Clear();
    }

    // ═══════════════════════════════════════════════════════════
    // UI REFRESH
    // ═══════════════════════════════════════════════════════════
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
                    int menit = data.playTimeSeconds / 60;
                    int detik = data.playTimeSeconds % 60;
                    string roomName = string.IsNullOrEmpty(data.lastRoomID) ? "Unknown" : data.lastRoomID;
                    slotLabels[i].text = $"{roomName}\n{menit:00}:{detik:00}";
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

    // ═══════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════
    IEnumerator SelectFirstNextFrame()
    {
        yield return null;
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