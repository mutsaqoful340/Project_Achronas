using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MenuSelector : MonoBehaviour
{
    [Header("UI Panels")]
    // ===== PANEL =====
    public GameObject TitleScreen;
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject gameplayPanel;
    public GameObject extrasPanel;
    public GameObject creditsPanel;
    public GameObject galleryPanel;
    public GameObject audioPanel;
    public GameObject videoPanel;
    public GameObject controlPanel;
    public GameObject playPanel;
    public GameObject continuePanel;

    [Header("EXTERNAL REFERENCES")]
    public MonoBehaviour playerMovement;
    public LoadingScreen loadingScreen;
    public PauseMenuSelector pauseMenu;
    public SlotSelector slotSelector;

    bool inSettings = false;
    bool inExtras = false;
    bool inCredits = false;
    bool inGallery = false;
    bool inSinglePlayer = false;

    public bool isInSetting = false;
    public bool isInControlPanel = false;
    public bool isInAudioPanel = false;
    public bool isInVideoPanel = false;
    public bool isInContinuePanel = false;

    // ===== HISTORY =====
    public struct PanelState
    {
        public GameObject panel;
        public bool wasInSettings;
        public bool wasInExtras;
        public bool wasInCredits;
        public bool wasInGallery;
        public bool wasInControlPanel;
        public bool wasInSinglePlayer;
    }

    public Stack<PanelState> panelHistory = new Stack<PanelState>();

    private InputActions inputActions;

    void Start()
    {
        playerMovement.enabled = false;
        DisableAll();
        ShowTitleScreen();
    }

    void OnEnable()
    {
        inputActions = new InputActions();
        inputActions.UI.Enable();
        inputActions.UI.Cancel.performed += OnCancelInput;
    }

    void OnDisable()
    {
        inputActions.UI.Cancel.performed -= OnCancelInput;
        inputActions.Dispose();
    }

    private void OnCancelInput(InputAction.CallbackContext context)
    {

        if (slotSelector != null && slotSelector.IsPopupOpen()) return;
        // Block kalau semua panel nonaktif (sedang gameplay)
        if (!mainPanel.activeSelf && !settingsPanel.activeSelf && !extrasPanel.activeSelf
        && !gameplayPanel.activeSelf && !controlPanel.activeSelf && !audioPanel.activeSelf
        && !videoPanel.activeSelf && !playPanel.activeSelf && !continuePanel.activeSelf
        && !creditsPanel.activeSelf && !galleryPanel.activeSelf && !TitleScreen.activeSelf)
            return;

        GoBack();
    }
    void PushCurrentState(GameObject currentPanel)
    {
        panelHistory.Push(new PanelState
        {
            panel = currentPanel,
            wasInSettings = inSettings,
            wasInExtras = inExtras,
            wasInCredits = inCredits,
            wasInGallery = inGallery,
            wasInSinglePlayer = inSinglePlayer,
            wasInControlPanel = isInControlPanel
        });
    }

    public void GoBack()
    {
        if (panelHistory.Count == 0) return;

        PanelState prev = panelHistory.Pop();

        DisableAll();
        

        prev.panel.SetActive(true);
        inSettings = prev.wasInSettings;
        inExtras = prev.wasInExtras;
        inCredits = prev.wasInCredits;
        inGallery = prev.wasInGallery;
        isInControlPanel = prev.wasInControlPanel;
        inSinglePlayer = prev.wasInSinglePlayer;
        isInContinuePanel = false;

        EventSystem.current.SetSelectedGameObject(null);
        SelectFirstButton(prev.panel);
    }

    public void ShowTitleScreen()
    {
        DisableAll();
        TitleScreen.SetActive(true);
    }

    public void ShowMainMenu()
    {
        DisableAll();
        mainPanel.SetActive(true);
        SelectFirstButton(mainPanel);
    }

    // ===== PANEL OPENING METHODS =====
    #region Open Panel Methods
    public void OpenPanel_Play()
    {
        PushCurrentState(mainPanel);
        OpenPanelInternal(playPanel);
        SelectFirstButton(playPanel);
        inSinglePlayer = true;
    }

    public void OpenPanel_Settings()
    {
        PushCurrentState(mainPanel);
        OpenPanelInternal(settingsPanel);
        SelectFirstButton(settingsPanel);
        inSettings = true;
    }

    public void OpenPanel_Extras()
    {
        PushCurrentState(mainPanel);
        OpenPanelInternal(extrasPanel);
        SelectFirstButton(extrasPanel);
        inExtras = true;
    }

    public void OpenPanel_Gameplay()
    {
        PushCurrentState(settingsPanel);
        DisableAll();
        gameplayPanel.SetActive(true);
        SelectFirstButton(gameplayPanel);
        isInControlPanel = false;
    }

    public void OpenPanel_Control()
    {
        PushCurrentState(gameplayPanel);
        DisableAll();
        controlPanel.SetActive(true);
        SelectFirstButton(controlPanel);
        isInControlPanel = true;
    }

    public void OpenPanel_Video()
    {
        PushCurrentState(settingsPanel);
        DisableAll();
        videoPanel.SetActive(true);
        SelectFirstButton(videoPanel);
    }

    public void OpenPanel_Audio()
    {
        PushCurrentState(settingsPanel);
        DisableAll();
        audioPanel.SetActive(true);
        SelectFirstButton(audioPanel);
    }


    public void OpenPanel_Continue()
    {
        PushCurrentState(playPanel);
        DisableAll();
        continuePanel.SetActive(true);
        isInContinuePanel = true;
        inSinglePlayer = false;
        if (slotSelector != null) slotSelector.isFromPauseMenu = false;
    }
    #endregion

    // ===== BUTTON METHODS =====
    #region Button Methods
    public void SelectQuit()
    {
        Application.Quit();
    }

    public void SelectNewGame()
    {
        PlayerPrefs.DeleteAll();
        DisableAll();
        AudioManager.Instance.StopMainMenuBGM();
        playerMovement.enabled = true;
        inSinglePlayer = false;
        panelHistory.Clear();
    }


    public void SelectLoadGame()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[LOAD] SaveManager tidak ditemukan!");
            return;
        }

        // Cari slot terakhir yang ada datanya
        string[] slotNames = { "slot1", "slot2", "slot3", "slot4", "slot5", "slot6" };
        string lastSlot = null;

        for (int i = slotNames.Length - 1; i >= 0; i--)
        {
            if (SaveManager.Instance.SlotExists(slotNames[i]))
            {
                lastSlot = slotNames[i];
                break;
            }
        }

        if (lastSlot == null)
        {
            Debug.LogWarning("[LOAD] Tidak ada data save!");
            return;
        }

        DisableAll();
        playerMovement.enabled = true;
        inSinglePlayer = false;
        panelHistory.Clear();

        GameLoader gameLoader = FindAnyObjectByType<GameLoader>();
        if (gameLoader != null)
            gameLoader.LoadGame(lastSlot);
    }
    #endregion

    private void OpenPanelInternal(GameObject panel)
    {
        DisableAll();
        panel.SetActive(true);
    }

    private void SelectFirstButton(GameObject panel)
    {
        Button firstButton = panel.GetComponentInChildren<Button>();
        if (firstButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }
    }

    public void ExitAudioPanel()
    {
        isInAudioPanel = false;
    }

    public void EnterVideoPanel()
    {
        isInVideoPanel = true;
    }

    public void ExitVideoPanel()
    {
        isInVideoPanel = false;
    }

    public void OpenPanel_Credits()
    {
        PushCurrentState(mainPanel);
        OpenPanelInternal(creditsPanel);
        SelectFirstButton(creditsPanel);
        inCredits = true;
    }

    public void OpenPanel_Gallery()
    {
        PushCurrentState(mainPanel);
        OpenPanelInternal(galleryPanel);
        SelectFirstButton(galleryPanel);
        inGallery = true;
    }


    public void DisableAll()
    {
        TitleScreen.SetActive(false);
        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        extrasPanel.SetActive(false);
        gameplayPanel.SetActive(false);
        controlPanel.SetActive(false);
        audioPanel.SetActive(false);
        videoPanel.SetActive(false);
        playPanel.SetActive(false);
        continuePanel.SetActive(false);
        creditsPanel.SetActive(false);
        galleryPanel.SetActive(false);
    }
}