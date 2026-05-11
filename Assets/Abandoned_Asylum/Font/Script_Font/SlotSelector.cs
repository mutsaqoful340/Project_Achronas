using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class SlotSelector : MonoBehaviour
{
    public GameObject[] slotBgs;
    public TextMeshProUGUI[] slotLabels;
    public Image[] slotThumbnails;

    public Transform playerTransform;
    public LoadingScreen loadingScreen;
    public MenuSelector menuSelector;
    public PauseMenuSelector pauseMenu;

    int index = 0;
    int totalSlots = 6;
    string[] slotNames = { "slot1", "slot2", "slot3", "slot4", "slot5", "slot6" };
    int thumbWidth = 320;
    int thumbHeight = 180;

    void OnEnable()
    {
        index = 0;
        RefreshSlotLabels();
        RefreshThumbnails();
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
            DoLoad();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseMenu != null) pauseMenu.isInSavePanel = false;
            menuSelector.isInContinuePanel = false;
            menuSelector.GoBack();
            gameObject.SetActive(false);
        }
    }

    void DoLoad()
    {
        string slot = slotNames[index];

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

    void UpdateSlots()
    {
        for (int i = 0; i < totalSlots; i++)
            slotBgs[i].SetActive(i == index);
    }

    string ThumbnailPath(string slot) =>
        Path.Combine(Application.persistentDataPath, "saves", slot + "_thumb.png");
}
