using UnityEngine;
using TMPro;

public class SaveSlotSelector : MonoBehaviour
{
    public GameObject[] slotBgs;
    public TextMeshProUGUI[] slotLabels;
    public PauseMenuSelector pauseMenu;     // Drag PauseMenuSelector
    public PlayerSaveController playerSave; // Drag PlayerSaveController

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
            SaveToSlot();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            pauseMenu.CloseSavePanel(); // balik ke pause menu
        }
    }

    void SaveToSlot()
    {
        string slot = slotNames[index];
        Debug.Log($"playerSave: {playerSave}, slot: {slot}");  // ← tambahkan ini
        playerSave.SaveToSlot(slot);
        RefreshSlotLabels();
        Debug.Log($"[SaveSlot] Tersimpan ke '{slot}'");
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

    void Awake()
    {
        Debug.Log($"[SaveSlotSelector] Awake di: {gameObject.name}");
    }
}
