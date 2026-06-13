using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;

public class SaveTabletSelector : MonoBehaviour
{
    public enum Mode { Save, Load }

    // ═══════════════════════════════════════════════════════════
    // REFERENCES
    // ═══════════════════════════════════════════════════════════
    [Header("UI")]
    public GameObject[] slotBgs;
    public TextMeshProUGUI[] slotLabels;
    public Image[] slotThumbnails;

    [Header("Tab Buttons")]
    public Image btnSaveBg;
    public Image btnLoadBg;
    public TextMeshProUGUI txtSave;
    public TextMeshProUGUI txtLoad;

    [Header("Players")]
    public Transform player1Transform;
    public Transform player2Transform;

    [Header("External References")]
    public PlayerSaveController playerSave;
    public LoadingScreen loadingScreen;
    public Camera captureCamera;
    public PauseMenuSelector pauseMenu;

    // ═══════════════════════════════════════════════════════════
    // PRIVATE
    // ═══════════════════════════════════════════════════════════
    public Mode currentMode = Mode.Save;

    private int index = 0;
    private int totalSlots = 6;
    private string[] slotNames = { "slot1", "slot2", "slot3", "slot4", "slot5", "slot6" };
    private int thumbWidth = 320;
    private int thumbHeight = 180;

    // ═══════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════
    void OnEnable()
    {
        currentMode = Mode.Save;
        index = 0;
        RefreshSlotLabels();
        RefreshThumbnails();
    }

    void Update()
    {
        // Switch tab
        if (Input.GetKeyDown(KeyCode.Q))
            currentMode = Mode.Save;

        if (Input.GetKeyDown(KeyCode.E))
            currentMode = Mode.Load;

        // Navigasi slot
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
            if (currentMode == Mode.Save)
                StartCoroutine(DoSaveWithScreenshot());
            else
                DoLoad();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            pauseMenu.CloseSavePanel();
    }

    // ═══════════════════════════════════════════════════════════
    // SAVE
    // ═══════════════════════════════════════════════════════════
    IEnumerator DoSaveWithScreenshot()
    {
        string slot = slotNames[index];

        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        yield return new WaitForEndOfFrame();

        CaptureAndSaveThumbnail(slot);
        cg.alpha = 1f;

        playerSave.SaveToSlot(slot);
        RefreshSlotLabels();
        RefreshThumbnails();

        Debug.Log($"[SaveTablet] Tersimpan ke '{slot}'");
    }

    void CaptureAndSaveThumbnail(string slot)
    {
        if (captureCamera == null)
            captureCamera = Camera.main;

        RenderTexture rt = new RenderTexture(thumbWidth, thumbHeight, 24);
        captureCamera.targetTexture = rt;
        captureCamera.Render();

        RenderTexture.active = rt;
        Texture2D screenshot = new Texture2D(thumbWidth, thumbHeight, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, thumbWidth, thumbHeight), 0, 0);
        screenshot.Apply();

        captureCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        byte[] bytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(ThumbnailPath(slot), bytes);
        Destroy(screenshot);
    }

    // ═══════════════════════════════════════════════════════════
    // LOAD
    // ═══════════════════════════════════════════════════════════
    void DoLoad()
    {
        string slot = slotNames[index];

        if (!SaveManager.Instance.SlotExists(slot))
        {
            Debug.Log($"[SaveTablet] Slot '{slot}' kosong.");
            return;
        }

        pauseMenu.isPaused = false;
        pauseMenu.isInSavePanel = false;
        Time.timeScale = 1f;

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

    // ═══════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════
    string ThumbnailPath(string slot) =>
        Path.Combine(Application.persistentDataPath, "saves", slot + "_thumb.png");
}