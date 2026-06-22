using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SlotSelector : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    // REFERENCES
    // ═══════════════════════════════════════════════════════════
    [Header("UI - Slot")]
    public GameObject[] slotBgs;
    public TextMeshProUGUI[] slotLabels;
    public Image[] slotThumbnails;

    [Header("UI - Popup Delete")]
    public GameObject popupDelete;
    public TextMeshProUGUI popupSlotText;   // Text-SlotSave → tampilkan "SLOT 1", "SLOT 2", dst
    public Button popupBtnYa;
    public Button popupBtnTidak;

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
    private string[] slotLabelsUI = { "SLOT 1", "SLOT 2", "SLOT 3", "SLOT 4", "SLOT 5", "SLOT 6" };
    private int thumbWidth = 320;
    private int thumbHeight = 180;

    private InputActions inputActions;
    private bool popupOpen = false;
    private float inputCooldown = 0f;

    // ═══════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════
    void OnEnable()
    {
        selectedIndex = -1;
        popupOpen = false;

        if (popupDelete != null) popupDelete.SetActive(false);

        RefreshSlotLabels();
        RefreshThumbnails();
        StartCoroutine(SelectFirstNextFrame());

        inputActions = new InputActions();
        inputActions.UI.Enable();
        inputActions.UI.BtnA.performed += OnBtnA;
        inputActions.UI.BtnX.performed += OnBtnX;
        inputActions.UI.Cancel.performed += OnCancel;
    }

    void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.UI.BtnA.performed -= OnBtnA;
            inputActions.UI.BtnX.performed -= OnBtnX;
            inputActions.UI.Cancel.performed -= OnCancel;
            inputActions.UI.Disable();
            inputActions.Dispose();
            inputActions = null;
        }
    }

    void Update()
    {
        if (inputCooldown > 0f) inputCooldown -= Time.unscaledDeltaTime;
        if (popupOpen) return;
        if (EventSystem.current == null) return;

        GameObject focused = EventSystem.current.currentSelectedGameObject;
        if (focused == null) return;

        Debug.Log($"Focused: {focused.name} | selectedIndex: {selectedIndex}");

        for (int i = 0; i < slotBgs.Length; i++)
        {
            if (slotBgs[i] == null) continue;
            if (focused == slotBgs[i]
                || focused.transform == slotBgs[i].transform
                || focused.transform.IsChildOf(slotBgs[i].transform)
                || slotBgs[i].transform.IsChildOf(focused.transform))
            {
                selectedIndex = i;
                return;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // INPUT CALLBACKS
    // ═══════════════════════════════════════════════════════════
    private void OnBtnA(InputAction.CallbackContext ctx)
    {
        if (!gameObject.activeInHierarchy) return;
        if (popupOpen) return;
        if (inputCooldown > 0f) return;
        inputCooldown = 0.3f;
        ConfirmLoad();
    }

    private void OnBtnX(InputAction.CallbackContext ctx)
    {
        if (!gameObject.activeInHierarchy) return;
        if (popupOpen) return;
        if (menuSelector == null || !menuSelector.isInContinuePanel) return;
        TryOpenDeletePopup();
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        if (!gameObject.activeInHierarchy) return;
        if (popupOpen) ClosePopup();
        // Kalau popup tidak buka, biarkan MenuSelector.GoBack() handle
    }

    // ═══════════════════════════════════════════════════════════
    // POPUP
    // ═══════════════════════════════════════════════════════════
    void TryOpenDeletePopup()
    {
        if (selectedIndex == -1) return;

        string slot = slotNames[selectedIndex];
        if (SaveManager.Instance == null || !SaveManager.Instance.SlotExists(slot))
        {
            Debug.Log($"[SlotSelector] Slot '{slot}' kosong, tidak bisa dihapus.");
            return;
        }

        // Update teks slot di popup
        if (popupSlotText != null)
            popupSlotText.text = slotLabelsUI[selectedIndex];

        // Pasang listener tombol (clear dulu biar tidak double)
        popupBtnYa.onClick.RemoveAllListeners();
        popupBtnTidak.onClick.RemoveAllListeners();
        popupBtnYa.onClick.AddListener(OnPopupYa);
        popupBtnTidak.onClick.AddListener(OnPopupTidak);

        popupDelete.SetActive(true);
        popupOpen = true;
        inputCooldown = 0.3f;

        // Fokus ke tombol Tidak (aman, biar tidak salah hapus)
        EventSystem.current.SetSelectedGameObject(popupBtnTidak.gameObject);
    }

    public void OnPopupYa()
    {
        int indexToDelete = selectedIndex; // simpan dulu sebelum di-reset
        DoDelete(indexToDelete);
        ClosePopup();
    }

    public void OnPopupTidak()
    {
        ClosePopup();
    }

    void ClosePopup()
    {
        popupDelete.SetActive(false);
        popupOpen = false;

        StartCoroutine(RestoreFocusNextFrame());
    }

    IEnumerator RestoreFocusNextFrame()
    {
        yield return null;

        // Coba fokus ke slot yang tadi dipilih
        if (selectedIndex >= 0 && slotBgs != null && selectedIndex < slotBgs.Length)
        {
            Button btn = slotBgs[selectedIndex].GetComponent<Button>();
            if (btn != null && btn.interactable)
            {
                EventSystem.current.SetSelectedGameObject(btn.gameObject);
                yield break;
            }
        }

        // Fallback ke firstSelectedButton
        if (firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // PUBLIC
    // ═══════════════════════════════════════════════════════════
    public void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= totalSlots) return;
        selectedIndex = slotIndex;
    }

    public void ConfirmLoad()
    {
        if (selectedIndex == -1) return;

        string slot = slotNames[selectedIndex];
        if (SaveManager.Instance == null || !SaveManager.Instance.SlotExists(slot))
        {
            Debug.Log($"[SlotSelector] Slot '{slot}' kosong.");
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

        if (pauseMenu != null) pauseMenu.isInSavePanel = false;
        menuSelector.isInContinuePanel = false;
        Time.timeScale = 1f;

        SaveManager.Instance.Load(slot, player1Transform, player2Transform);

        menuSelector.DisableAll();
        menuSelector.playerMovement.enabled = true;
        menuSelector.panelHistory.Clear();
    }

    // ═══════════════════════════════════════════════════════════
    // DELETE
    // ═══════════════════════════════════════════════════════════
    void DoDelete(int slotIndex)
    {
        string slot = slotNames[slotIndex];

        SaveManager.Instance.DeleteSave(slot);

        string thumbPath = ThumbnailPath(slot);
        if (File.Exists(thumbPath))
            File.Delete(thumbPath);

        selectedIndex = -1; // reset supaya fokus fallback ke firstSelectedButton
        RefreshSlotLabels();
        RefreshThumbnails();

        Debug.Log($"[SlotSelector] Slot '{slot}' berhasil dihapus.");
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
            selectedIndex = 0;
        }
    }

    string ThumbnailPath(string slot) =>
        Path.Combine(Application.persistentDataPath, "saves", slot + "_thumb.png");
}
