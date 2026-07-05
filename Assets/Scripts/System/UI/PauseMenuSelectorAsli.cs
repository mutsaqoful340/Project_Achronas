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
    public GameObject mapPanel;          // ← BARU: MapPanel (minimap)
    [Header("External")]
    public MenuSelector menuSelector;
    private Stack<GameObject> panelHistory = new Stack<GameObject>();
    private InputActions inputActions;

    // ← BARU: buat debug selection
    private GameObject _lastSelected;

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

    // ← BARU: debug log tiap kali selection berubah
    void Update()
    {
        if (EventSystem.current.currentSelectedGameObject != _lastSelected)
        {
            _lastSelected = EventSystem.current.currentSelectedGameObject;
            Debug.Log("Selected berubah jadi: " + (_lastSelected != null ? _lastSelected.name : "NULL"));
        }
    }

    private void OnCancelInput(InputAction.CallbackContext context)
    {
        if (!pausePanel.activeSelf && !continuePanel.activeSelf && !settingsPanel.activeSelf
            && !settingsGameplayPanel.activeSelf && !settingsAudioPanel.activeSelf
            && !settingsVideoPanel.activeSelf && !kontrolPanel.activeSelf && !mapPanel.activeSelf) return;
        GoBack();
    }
    public void BackToMainMenu()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
    // ← BARU: buka MapPanel, Panel_PauseMenu otomatis off lewat DisableAll()
    public void OpenPanel_Map()
    {
        PushCurrentState(pausePanel);
        DisableAll();
        mapPanel.SetActive(true);
        SelectFirstButton(mapPanel);
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
        mapPanel.SetActive(false);   // ← BARU
    }
    private void SelectFirstButton(GameObject panel)
    {
        StartCoroutine(SelectFirstButtonRoutine(panel));
    }

    private System.Collections.IEnumerator SelectFirstButtonRoutine(GameObject panel)
    {
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        yield return null; // 1 frame tambahan setelah render selesai
        Button btn = panel.GetComponentInChildren<Button>(true);
        if (btn != null)
            EventSystem.current.SetSelectedGameObject(btn.gameObject);
        else
            Debug.LogWarning("Gak ketemu Button di panel: " + panel.name);
    }
}