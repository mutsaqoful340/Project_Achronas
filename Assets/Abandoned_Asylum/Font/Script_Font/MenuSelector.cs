using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class MenuSelector : MonoBehaviour, ISelectHandler, ICancelHandler
{
    // ===== ANIMATOR HIGHLIGHTS =====
    public Animator[] mainHighlights;
    public Animator[] settingsHighlights;
    public Animator[] extrasHighlights;
    public Animator[] singlePlayerHighlights;

    // ===== PANEL =====
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject gameplayPanel;
    public GameObject extrasPanel;
    public GameObject audioPanel;
    public GameObject videoPanel;
    public GameObject controlPanel;
    public GameObject singlePlayerPanel;
    public GameObject continuePanel;

    public MonoBehaviour playerMovement;
    public LoadingScreen loadingScreen;
    public PauseMenuSelector pauseMenu;
    public SlotSelector slotSelector;

    // ACTIVE UI
    Animator[] highlights;

    // MENU
    string[] mainMenu = { "SINGLE PLAYER", "CO-OP", "SETTINGS", "EXTRAS", "QUIT" };
    string[] extrasMenu = { "CREDITS", "GALLERY", "BACK" };
    string[] settingsMenu = { "GAMEPLAY", "VIDEO", "AUDIO", "BACK" };
    string[] singlePlayerMenu = { "CONTINUE", "NEW GAME", "LOAD GAME", "BACK" };

    string[] currentMenu;

    int index = 0;

    bool inSettings = false;
    bool inExtras = false;
    bool inSinglePlayer = false;

    public bool isUsingSetting = false;
    public bool isInControlPanel = false;
    public bool isInAudioPanel = false;
    public bool isInVideoPanel = false;
    public bool isInContinuePanel = false;

    // ===== HISTORY =====
    private struct PanelState
    {
        public GameObject panel;
        public Animator[] highlights;
        public string[] menu;
        public int index;
        public bool wasInSettings;
        public bool wasInExtras;
        public bool wasInControlPanel;
        public bool wasInSinglePlayer;
    }

    private Stack<PanelState> panelHistory = new Stack<PanelState>();

    void Start()
    {
        playerMovement.enabled = false;
        DisableAll();
        ShowMainMenu();
    }

    void Update()
    {
        if (currentMenu == null) return;

        if (isUsingSetting) return;
        if (isInAudioPanel) return;
        if (isInVideoPanel) return;
        if (isInContinuePanel) return;

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            index = (index + 1) % currentMenu.Length;
            UpdateMenu();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            index--;
            if (index < 0) index = currentMenu.Length - 1;
            UpdateMenu();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!SettingOptionUI.IsHandlingReturn())
                HandleSelect();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseMenu != null && pauseMenu.isPaused) return;
            GoBack();
        }
    }
    public void OnSelect(BaseEventData eventData)
    {
        Debug.Log("Menu selected");
    }

    public void OnCancel(BaseEventData eventData)
    {
        // Automatically called when Cancel input (Escape) is pressed
        GoBack();
    }
    void PushCurrentState(GameObject currentPanel)
    {
        panelHistory.Push(new PanelState
        {
            panel = currentPanel,
            highlights = highlights,
            menu = currentMenu,
            index = index,
            wasInSettings = inSettings,
            wasInExtras = inExtras,
            wasInSinglePlayer = inSinglePlayer,
            wasInControlPanel = isInControlPanel
        });
    }

    public void GoBack()
    {
        if (panelHistory.Count == 0) return;

        // HAPUS ClearHighlights(highlights) — tidak perlu

        PanelState prev = panelHistory.Pop();

        DisableAll();

        prev.panel.SetActive(true);
        highlights = prev.highlights;
        currentMenu = prev.menu;
        index = prev.index;
        inSettings = prev.wasInSettings;
        inExtras = prev.wasInExtras;
        isInControlPanel = prev.wasInControlPanel;
        inSinglePlayer = prev.wasInSinglePlayer;
        isInContinuePanel = false;

        UpdateMenu(); // ini sudah cukup — set true yang perlu, false yang lain
    }

    public void ShowMainMenu()
    {
        DisableAll();
        mainPanel.SetActive(true);
        highlights = mainHighlights;
        currentMenu = mainMenu;
        index = 0;
        UpdateMenu();
    }

    void HandleSelect()
    {
        string selected = currentMenu[index];

        if (!inSettings && !inExtras && !inSinglePlayer)
        {
            if (selected == "SINGLE PLAYER")
            {
                PushCurrentState(mainPanel);
                SwitchTo(singlePlayerPanel, singlePlayerHighlights, singlePlayerMenu);
                inSinglePlayer = true;
            }
            else if (selected == "SETTINGS")
            {
                PushCurrentState(mainPanel);
                SwitchTo(settingsPanel, settingsHighlights, settingsMenu);
                inSettings = true;
            }
            else if (selected == "EXTRAS")
            {
                PushCurrentState(mainPanel);
                SwitchTo(extrasPanel, extrasHighlights, extrasMenu);
                inExtras = true;
            }
            else if (selected == "QUIT")
            {
                Application.Quit();
            }
        }

        else if (inSettings)
        {
            if (selected == "GAMEPLAY")
            {
                PushCurrentState(settingsPanel);
                OpenGameplaySettings();
            }
            else if (selected == "VIDEO")
            {
                PushCurrentState(settingsPanel);
                OpenVideoSettings();
            }
            else if (selected == "AUDIO")
            {
                PushCurrentState(settingsPanel);
                OpenAudioSettings();
            }
            else if (selected == "BACK")
            {
                GoBack();
                return;
            }
        }

        else if (inSinglePlayer)
        {
            if (selected == "CONTINUE")
            {
                DisableAll();
                continuePanel.SetActive(true);
                isInContinuePanel = true;
                playerMovement.enabled = true;
                inSinglePlayer = false;
                panelHistory.Clear();
            }
            else if (selected == "NEW GAME")
            {
                DisableAll();
                playerMovement.enabled = true;
                currentMenu = null;
                inSinglePlayer = false;
                panelHistory.Clear();
            }
            else if (selected == "LOAD GAME")
            {
                Debug.Log("Load game");
            }
            else if (selected == "BACK")
            {
                GoBack();
                return;
            }
        }

        else if (inExtras)
        {
            if (selected == "BACK")
            {
                GoBack();
                return;
            }
        }

        UpdateMenu();
    }

    void OpenGameplaySettings()
    {
        DisableAll();
        gameplayPanel.SetActive(true);
        isInControlPanel = false;
    }

    void OpenAudioSettings()
    {
        DisableAll();
        audioPanel.SetActive(true);
    }

    void OpenVideoSettings()
    {
        DisableAll();
        videoPanel.SetActive(true);
    }

    public void StartGame()
    {
        DisableAll();
        playerMovement.enabled = true;
        currentMenu = null;
    }

    public void OpenControlPanel()
    {
        PushCurrentState(gameplayPanel);
        DisableAll();
        controlPanel.SetActive(true);
        isInControlPanel = true;
    }

    public void EnterAudioPanel()
    {
        isInAudioPanel = true;
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

    void SwitchTo(GameObject panel, Animator[] anim, string[] menu)
    {
        // HAPUS ClearHighlights(highlights) — tidak perlu

        DisableAll();
        panel.SetActive(true);
        highlights = anim;
        currentMenu = menu;
        index = 0;
        UpdateMenu(); // tambah ini
    }

    void RebindHighlights(Animator[] anims)
    {
        if (anims == null) return;
        foreach (var anim in anims)
        {
            if (anim == null) continue;
            anim.Rebind();       // reset animator ke state awal
            anim.Update(0f);     // paksa evaluate frame pertama
        }
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
        singlePlayerPanel.SetActive(false);
        continuePanel.SetActive(false);
    }

    void ClearHighlights(Animator[] toClear)
    {
        if (toClear == null) return;
        foreach (var anim in toClear)
        {
            if (anim == null) continue;
            anim.SetTrigger("Normal");
        }
    }

    void UpdateMenu()
    {
        for (int i = 0; i < highlights.Length; i++)
        {
            if (highlights[i] == null) continue;

            if (i == index)
                highlights[i].SetTrigger("Selected");
            else
                highlights[i].SetTrigger("Normal");
        }
    }
}