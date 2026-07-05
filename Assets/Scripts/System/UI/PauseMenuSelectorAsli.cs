using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuSelectorAsli : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pausePanel;
    public GameObject continuePanel;
    public GameObject settingsPanel;
    public GameObject settingsGameplayPanel;
    public GameObject settingsAudioPanel;
    public GameObject settingsVideoPanel;
    public GameObject kontrolPanel;
    public GameObject mapPanel;

    [Header("External")]
    public MenuSelector menuSelector;

    [Header("Map System")]
    public MapController mapController;

    private Stack<GameObject> panelHistory = new Stack<GameObject>();
    private InputActions inputActions;
    private GameObject _lastSelected;

    void OnEnable()
    {
        inputActions = new InputActions();
        inputActions.UI.Enable();
        inputActions.UI.Cancel.performed += OnCancelInput;
    }

    void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.UI.Cancel.performed -= OnCancelInput;
            inputActions.Dispose();
        }
    }

    void Update()
    {
        // debug selected
        if (EventSystem.current.currentSelectedGameObject != _lastSelected)
        {
            _lastSelected = EventSystem.current.currentSelectedGameObject;
            Debug.Log("Selected berubah jadi: " +
                (_lastSelected != null ? _lastSelected.name : "NULL"));
        }

        Gamepad gp = Gamepad.current;
        if (gp == null) return;

        bool isAnyPausePanelOpen =
            pausePanel.activeSelf ||
            continuePanel.activeSelf ||
            settingsPanel.activeSelf ||
            settingsGameplayPanel.activeSelf ||
            settingsAudioPanel.activeSelf ||
            settingsVideoPanel.activeSelf ||
            kontrolPanel.activeSelf ||
            mapPanel.activeSelf;

        // Select/View = buka pause biasa
        if (gp.selectButton.wasPressedThisFrame && !isAnyPausePanelOpen)
        {
            OpenPauseMenu();
        }

        // D-pad Right = buka pause langsung ke map
        if (gp.dpad.right.wasPressedThisFrame && !isAnyPausePanelOpen)
        {
            Debug.Log("Buka Pause langsung ke Map");

            OpenPauseMenu();
            OpenPanel_Map();
        }
    }

    private void OnCancelInput(InputAction.CallbackContext context)
    {
        if (!pausePanel.activeSelf &&
            !continuePanel.activeSelf &&
            !settingsPanel.activeSelf &&
            !settingsGameplayPanel.activeSelf &&
            !settingsAudioPanel.activeSelf &&
            !settingsVideoPanel.activeSelf &&
            !kontrolPanel.activeSelf &&
            !mapPanel.activeSelf)
            return;

        EventSystem.current.SetSelectedGameObject(null);
        GoBack();
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void PushCurrentState(GameObject current)
    {
        if (current != null)
            panelHistory.Push(current);
    }

    public void GoBack()
    {
        if (panelHistory.Count == 0)
        {
            DisableAll();
            return;
        }

        GameObject prev = panelHistory.Pop();

        EventSystem.current.SetSelectedGameObject(null);

        if (mapController != null && mapController.isOpen)
        {
            mapController.ToggleMap();
        }

        DisableAll();

        prev.SetActive(true);

        StartCoroutine(DelayedRefreshSelection(prev));
    }

    public void OpenPauseMenu()
    {
        panelHistory.Clear();
        DisableAll();
        pausePanel.SetActive(true);
        SelectFirstButton(pausePanel);
    }

    public void OpenPanel_Continue()
    {
        PushCurrentState(pausePanel);
        DisableAll();
        continuePanel.SetActive(true);
        SelectFirstButton(continuePanel);
    }

    public void OpenPanel_Settings()
    {
        PushCurrentState(pausePanel);
        DisableAll();
        settingsPanel.SetActive(true);
        SelectFirstButton(settingsPanel);
    }

    public void OpenPanel_SettingsGameplay()
    {
        PushCurrentState(settingsPanel);
        DisableAll();
        settingsGameplayPanel.SetActive(true);
        SelectFirstButton(settingsGameplayPanel);
    }

    public void OpenPanel_SettingsAudio()
    {
        PushCurrentState(settingsPanel);
        DisableAll();
        settingsAudioPanel.SetActive(true);
        SelectFirstButton(settingsAudioPanel);
    }

    public void OpenPanel_SettingsVideo()
    {
        PushCurrentState(settingsPanel);
        DisableAll();
        settingsVideoPanel.SetActive(true);
        SelectFirstButton(settingsVideoPanel);
    }

    public void OpenPanel_Kontrol()
    {
        PushCurrentState(settingsGameplayPanel);
        DisableAll();
        kontrolPanel.SetActive(true);
        SelectFirstButton(kontrolPanel);
    }

    public void OpenPanel_Map()
    {
        PushCurrentState(pausePanel);

        DisableAll();

        mapPanel.SetActive(true);

        if (mapController != null && !mapController.isOpen)
        {
            mapController.ToggleMap();
        }

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void DisableAll()
    {
        pausePanel.SetActive(false);
        continuePanel.SetActive(false);
        settingsPanel.SetActive(false);
        settingsGameplayPanel.SetActive(false);
        settingsAudioPanel.SetActive(false);
        settingsVideoPanel.SetActive(false);
        kontrolPanel.SetActive(false);
        mapPanel.SetActive(false);
    }

    private void SelectFirstButton(GameObject panel)
    {
        StartCoroutine(SelectFirstButtonRoutine(panel));
    }

    private System.Collections.IEnumerator SelectFirstButtonRoutine(GameObject panel)
    {
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            panel.GetComponent<RectTransform>()
        );

        Button btn = panel.GetComponentInChildren<Button>(true);

        if (btn != null)
        {
            btn.Select();
            EventSystem.current.SetSelectedGameObject(btn.gameObject);

            ForceButtonSelectedAnimation(btn);
        }
    }

    private System.Collections.IEnumerator DelayedRefreshSelection(GameObject panel)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return null;

        yield return StartCoroutine(RefreshSelection(panel));
    }

    private System.Collections.IEnumerator RefreshSelection(GameObject panel)
    {
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            panel.GetComponent<RectTransform>()
        );

        Button btn = panel.GetComponentInChildren<Button>(true);

        if (btn != null)
        {
            EventSystem.current.SetSelectedGameObject(null);

            yield return null;

            btn.Select();
            EventSystem.current.SetSelectedGameObject(btn.gameObject);

            ForceButtonSelectedAnimation(btn);
        }
    }

    private void ForceButtonSelectedAnimation(Button btn)
    {
        Animator anim = btn.GetComponent<Animator>();

        if (anim != null)
        {
            anim.ResetTrigger("Normal");
            anim.ResetTrigger("Pressed");
            anim.ResetTrigger("Highlighted");

            // pastikan nama state ini sesuai animator kamu
            anim.Play("Selected", 0, 0f);
            anim.Update(Time.unscaledDeltaTime);
        }
    }
}