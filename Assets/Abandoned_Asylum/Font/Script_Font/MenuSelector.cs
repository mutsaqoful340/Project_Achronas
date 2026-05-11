using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class MenuSelector : MonoBehaviour
{
    // ===== TEXT =====
    public TextMeshProUGUI[] mainTexts;
    public TextMeshProUGUI[] settingsTexts;
    public TextMeshProUGUI[] extrasTexts;
    public TextMeshProUGUI[] singlePlayerTexts;

    // ===== BG =====
    public GameObject[] mainBg;
    public GameObject[] settingsBg;
    public GameObject[] extrasBg;
    public GameObject[] singlePlayerBg;

    // ===== PANEL =====
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject gameplayPanel;
    public GameObject extrasPanel;
    public GameObject audioPanel;
    public GameObject videoPanel; // 🔥 baru
    public GameObject controlPanel;
    public GameObject singlePlayerPanel;
    public GameObject continuePanel;

    public MonoBehaviour playerMovement;


    public LoadingScreen loadingScreen;
    public PauseMenuSelector pauseMenu;
    public SlotSelector slotSelector; // ← tambahkan di sini


    // ACTIVE UI
    TextMeshProUGUI[] texts;
    GameObject[] backgrounds;

    // MENU
    string[] mainMenu = { "SINGLE PLAYER", "CO-OP", "EXTRAS", "SETTINGS", "QUIT" };
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
        public TextMeshProUGUI[] texts;
        public GameObject[] backgrounds;
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
            if (pauseMenu != null && pauseMenu.isPaused) return; // ← tambahkan ini
            GoBack();
        }
    }

    void PushCurrentState(GameObject currentPanel)
    {
        panelHistory.Push(new PanelState
        {
            panel = currentPanel,
            texts = texts,
            backgrounds = backgrounds,
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

        PanelState prev = panelHistory.Pop();

        DisableAll();

        prev.panel.SetActive(true);
        texts = prev.texts;
        backgrounds = prev.backgrounds;
        currentMenu = prev.menu;
        index = prev.index;
        inSettings = prev.wasInSettings;
        inExtras = prev.wasInExtras;
        isInControlPanel = prev.wasInControlPanel;
        inSinglePlayer = prev.wasInSinglePlayer;
        isInContinuePanel = false;

        UpdateMenu();
    }

    public void ShowMainMenu()
    {
        DisableAll();
        mainPanel.SetActive(true);
        texts = mainTexts;
        backgrounds = mainBg;
        currentMenu = mainMenu;
        index = 0;
        UpdateMenu();
    }

    void HandleSelect()
    {
        string selected = currentMenu[index];

        // ===== MAIN =====
        if (!inSettings && !inExtras && !inSinglePlayer)
        {
            if (selected == "SINGLE PLAYER")
            {
                PushCurrentState(mainPanel);
                SwitchTo(singlePlayerPanel, singlePlayerTexts, singlePlayerBg, singlePlayerMenu);
                inSinglePlayer = true;
            }
            else if (selected == "SETTINGS")
            {
                PushCurrentState(mainPanel);
                SwitchTo(settingsPanel, settingsTexts, settingsBg, settingsMenu);
                inSettings = true;
            }
            else if (selected == "EXTRAS")
            {
                PushCurrentState(mainPanel);
                SwitchTo(extrasPanel, extrasTexts, extrasBg, extrasMenu);
                inExtras = true;
            }
            else if (selected == "QUIT")
            {
                Application.Quit();
            }
        }

        // ===== SETTINGS =====
        else if (inSettings)
        {
            if (selected == "GAMEPLAY")
            {
                PushCurrentState(settingsPanel);
                OpenGameplaySettings();
            }
            else if (selected == "VIDEO") // 🔥 baru
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

        // ===== SINGLE PLAYER =====
        else if (inSinglePlayer)
        {
            if (selected == "CONTINUE")
            {
                DisableAll();
                continuePanel.SetActive(true);  // ← ganti continuePanel.SetActive(true)
                isInContinuePanel = true;
                playerMovement.enabled = true;
                inSinglePlayer = false;
                panelHistory.Clear();
                Debug.Log("Continue game");
            }
            else if (selected == "NEW GAME")
            {
                DisableAll();
                playerMovement.enabled = true;
                currentMenu = null;
                inSinglePlayer = false;  // ← tambahkan ini
                panelHistory.Clear();    // ← tambahkan ini, bersihkan history
                Debug.Log("New game");
            }
            else if (selected == "LOAD GAME")
            {
                // TODO: buka panel load save
                Debug.Log("Load game");
            }
            else if (selected == "BACK")
            {
                GoBack();
                return;
            }
        }

        // ===== EXTRAS =====
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

    void OpenVideoSettings() // 🔥 baru
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

    void SwitchTo(GameObject panel, TextMeshProUGUI[] txt, GameObject[] bg, string[] menu)
    {
        DisableAll();

        panel.SetActive(true);

        texts = txt;
        backgrounds = bg;
        currentMenu = menu;

        index = 0;
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

    void UpdateMenu()
    {
        for (int i = 0; i < texts.Length; i++)
        {
            if (i < currentMenu.Length)
            {
                texts[i].gameObject.SetActive(true);

                bool selected = (i == index);

                backgrounds[i].SetActive(selected);
                texts[i].color = selected ? Color.black : Color.red;
                texts[i].transform.localScale = selected ? Vector3.one * 1.05f : Vector3.one;
            }
        }
    }
}