using UnityEngine;
using TMPro;

public class PauseMenuSelector : MonoBehaviour
{
    // ===== TEXT =====
    public TextMeshProUGUI[] pauseTexts;

    // ===== BG =====
    public GameObject[] pauseBg;

    // ===== PANEL =====
    public GameObject pausePanel;
    public SaveTabletSelector saveTabletSelector;
    public MenuSelector menuSelector;
    public TabletAnimator tabletAnimator;

    // ===== STATE =====
    string[] pauseMenu = { "RESUME", "SAVE", "SETTINGS", "QUIT TO MENU" };
    int index = 0;
    public bool isPaused = false;
    public bool isInSavePanel = false;

    private void Start()
    {
        pausePanel.SetActive(false);
        saveTabletSelector.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isInSavePanel) return;
            if (isPaused) Resume();
            else Pause();
            return;
        }

        if (!isPaused) return;
        if (isInSavePanel) return;

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            index = (index + 1) % pauseMenu.Length;
            UpdateMenu();
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            index--;
            if (index < 0) index = pauseMenu.Length - 1;
            UpdateMenu();
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            HandleSelect();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        tabletAnimator.ShowTablet();
        index = 0;
        UpdateMenu();
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        tabletAnimator.HideTablet();
        saveTabletSelector.gameObject.SetActive(false);
        isInSavePanel = false;
    }

    void HandleSelect()
    {
        string selected = pauseMenu[index];

        if (selected == "RESUME")
        {
            Resume();
        }
        else if (selected == "SAVE")
        {
            pausePanel.SetActive(false);
            saveTabletSelector.gameObject.SetActive(true);
            isInSavePanel = true;
        }
        else if (selected == "SETTINGS")
        {
            Debug.Log("Settings belum dibuat");
        }
        else if (selected == "QUIT TO MENU")
        {
            Time.timeScale = 1f;
            isPaused = false;
            saveTabletSelector.gameObject.SetActive(false);
            tabletAnimator.HideTablet();
            menuSelector.ShowMainMenu();
        }
    }

    public void CloseSavePanel()
    {
        saveTabletSelector.gameObject.SetActive(false);
        pausePanel.SetActive(true);
        isInSavePanel = false;
        index = 1;
        UpdateMenu();
    }

    void UpdateMenu()
    {
        for (int i = 0; i < pauseTexts.Length; i++)
        {
            if (i < pauseMenu.Length)
            {
                bool selected = (i == index);
                pauseBg[i].SetActive(selected);
                pauseTexts[i].color = selected ? Color.black : Color.red;
                pauseTexts[i].transform.localScale = selected ? Vector3.one * 1.05f : Vector3.one;
            }
        }
    }
}
