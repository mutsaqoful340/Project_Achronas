using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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

    [Header("External")]
    public MenuSelector menuSelector;

    private Stack<GameObject> panelHistory = new Stack<GameObject>();
    private InputActions inputActions;

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
        if (!pausePanel.activeSelf && !continuePanel.activeSelf && !settingsPanel.activeSelf
            && !settingsGameplayPanel.activeSelf && !settingsAudioPanel.activeSelf
            && !settingsVideoPanel.activeSelf && !kontrolPanel.activeSelf) return;
        GoBack();
    }

    void PushCurrentState(GameObject current)
    {
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
        DisableAll();
        prev.SetActive(true);
        SelectFirstButton(prev);
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

    public void DisableAll()
    {
        pausePanel.SetActive(false);
        continuePanel.SetActive(false);
        settingsPanel.SetActive(false);
        settingsGameplayPanel.SetActive(false);
        settingsAudioPanel.SetActive(false);
        settingsVideoPanel.SetActive(false);
        kontrolPanel.SetActive(false);
    }

    private void SelectFirstButton(GameObject panel)
    {
        Button btn = panel.GetComponentInChildren<Button>();
        if (btn != null)
            EventSystem.current.SetSelectedGameObject(btn.gameObject);
    }
}