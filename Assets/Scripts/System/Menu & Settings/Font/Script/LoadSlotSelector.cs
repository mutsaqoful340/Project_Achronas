using UnityEngine;
using TMPro;

public class LoadSlotSelector : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    // REFERENCES
    // ═══════════════════════════════════════════════════════════
    [Header("UI")]
    public GameObject[] slotBgs;
    public TextMeshProUGUI[] slotLabels;

    [Header("Players")]
    public Transform player1Transform;
    public Transform player2Transform;

    [Header("External References")]
    public MenuSelector menuSelector;
    public LoadingScreen loadingScreen;

    // ═══════════════════════════════════════════════════════════
    // PRIVATE
    // ═══════════════════════════════════════════════════════════
    private int index = 0;
    private int totalSlots = 6;
    private string[] slotNames = { "slot1", "slot2", "slot3", "slot4", "slot5", "slot6" };

    // ═══════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════
    void OnEnable()
    {
        index = 0;
        RefreshSlotLabels();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            int col = index / 2;
            if (col < 2) index += 2;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            int col = index / 2;
            if (col > 0) index -= 2;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            int row = index % 2;
            if (row < 1) index++;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            int row = index % 2;
            if (row > 0) index--;
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            LoadFromSlot();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            menuSelector.isInContinuePanel = false;
            menuSelector.GoBack();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // LOAD
    // ═══════════════════════════════════════════════════════════
    void LoadFromSlot()
    {
        string slot = slotNames[index];

        if (!SaveManager.Instance.SlotExists(slot))
        {
            Debug.Log($"[LoadSlot] Slot '{slot}' kosong.");
            return;
        }

        SaveManager.Instance.Load(slot, player1Transform, player2Transform);
        loadingScreen.StartLoading();
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
}