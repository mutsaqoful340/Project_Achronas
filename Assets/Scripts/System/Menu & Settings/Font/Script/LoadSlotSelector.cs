using UnityEngine;
using TMPro;

public class LoadSlotSelector : MonoBehaviour
{
    public GameObject[] slotBgs;
    public TextMeshProUGUI[] slotLabels;
    public MenuSelector menuSelector;       // Drag MenuSelector
    public LoadingScreen loadingScreen;     // Drag LoadingScreen
    public Transform playerTransform;       // Drag Player

    int index = 0;
    int totalSlots = 6;
    string[] slotNames = { "slot1", "slot2", "slot3", "slot4", "slot5", "slot6" };

    void OnEnable()
    {
        index = 0;
        RefreshSlotLabels();
        UpdateSlots();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            int col = index / 2;
            if (col < 2) index += 2;
            UpdateSlots();
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            int col = index / 2;
            if (col > 0) index -= 2;
            UpdateSlots();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            int row = index % 2;
            if (row < 1) index++;
            UpdateSlots();
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            int row = index % 2;
            if (row > 0) index--;
            UpdateSlots();
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

    void LoadFromSlot()
    {
        string slot = slotNames[index];

        if (!SaveManager.Instance.SlotExists(slot))
        {
            Debug.Log($"[LoadSlot] Slot '{slot}' kosong.");
            return;
        }

        Debug.Log($"[LoadSlot] Posisi sebelum load: {playerTransform.position}");
        SaveManager.Instance.Load(slot, playerTransform);
        Debug.Log($"[LoadSlot] Posisi sesudah load: {playerTransform.position}");

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

    void UpdateSlots()
    {
        for (int i = 0; i < totalSlots; i++)
        {
            slotBgs[i].SetActive(i == index);
        }
    }
}
