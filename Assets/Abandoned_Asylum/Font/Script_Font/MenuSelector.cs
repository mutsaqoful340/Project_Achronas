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
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject gameplayPanel;
    public GameObject extrasPanel;
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
    bool inSinglePlayer = false;

    public bool isInSetting = false;
    public bool isInControlPanel = false;
    public bool isInAudioPanel = false;
    public bool isInVideoPanel = false;
    public bool isInContinuePanel = false;

    // ===== HISTORY =====
    private struct PanelState
    {
        public GameObject panel;
        public bool wasInSettings;
        public bool wasInExtras;
        public bool wasInControlPanel;
        public bool wasInSinglePlayer;
    }

    private Stack<PanelState> panelHistory = new Stack<PanelState>();

    private InputActions inputActions;

    void Start()
    {
        playerMovement.enabled = false;
        DisableAll();
        ShowMainMenu();
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
        GoBack();
    }
    void PushCurrentState(GameObject currentPanel)
    {
        panelHistory.Push(new PanelState
        {
            panel = currentPanel,
            wasInSettings = inSettings,
            wasInExtras = inExtras,
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
        isInControlPanel = prev.wasInControlPanel;
        inSinglePlayer = prev.wasInSinglePlayer;
        isInContinuePanel = false;
        SelectFirstButton(prev.panel);
    }

    public void ShowMainMenu()
    {
        DisableAll();
        mainPanel.SetActive(true);
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
        DisableAll();
        continuePanel.SetActive(true);
        SelectFirstButton(continuePanel);
        isInContinuePanel = true;
        playerMovement.enabled = true;
        inSinglePlayer = false;
        panelHistory.Clear();
    }

    public void OpenPanel_Control()
    {
        PushCurrentState(gameplayPanel);
        DisableAll();
        controlPanel.SetActive(true);
        SelectFirstButton(controlPanel);
        isInControlPanel = true;
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
        DisableAll();
        playerMovement.enabled = true;
        inSinglePlayer = false;
        panelHistory.Clear();
    }

    public void SelectLoadGame()
    {
        Debug.Log("Load game");
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

    public void DisableAll()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        extrasPanel.SetActive(false);
        gameplayPanel.SetActive(false);
        controlPanel.SetActive(false);
        audioPanel.SetActive(false);
        videoPanel.SetActive(false);
        playPanel.SetActive(false);
        continuePanel.SetActive(false);
    }
}