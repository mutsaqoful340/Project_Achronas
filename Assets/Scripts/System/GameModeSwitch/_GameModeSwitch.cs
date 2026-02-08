using System;
using UnityEngine;
using UnityEngine.InputSystem;


public class _GameModeSwitch : MonoBehaviour
{
    public static _GameModeSwitch Instance { get; private set; }

    public PlayerInput[] PlayerInput;
    public enum GameMode
    {
        Player,
        UI
    }

    public GameMode currentMode = GameMode.UI;

    // Event that fires when game mode changes
    public event Action<GameMode> OnGameModeChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("<color=magenta>_GameModeSwitch Instance created</color>");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SwitchMode()
    {
        Debug.Log($"<color=magenta>SwitchMode called - Current mode: {currentMode}</color>");
        
        if (currentMode == GameMode.UI)
        {
            currentMode = GameMode.Player;
            
            // Switch PlayerInput action maps if any are assigned
            if (PlayerInput != null && PlayerInput.Length > 0)
            {
                foreach (var playerInput in PlayerInput)
                {
                    if (playerInput != null)
                        playerInput.SwitchCurrentActionMap("Player");
                }
            }
            
            Debug.Log($"<color=green>Invoking OnGameModeChanged event with GameMode.Player. Subscribers: {OnGameModeChanged?.GetInvocationList()?.Length ?? 0}</color>");
            OnGameModeChanged?.Invoke(GameMode.Player);
            Debug.Log("<color=green>Switched to Player mode</color>");
        }
        else
        {
            currentMode = GameMode.UI;
            
            // Switch PlayerInput action maps if any are assigned
            if (PlayerInput != null && PlayerInput.Length > 0)
            {
                foreach (var playerInput in PlayerInput)
                {
                    if (playerInput != null)
                        playerInput.SwitchCurrentActionMap("UI");
                }
            }
            
            Debug.Log($"<color=red>Invoking OnGameModeChanged event with GameMode.UI. Subscribers: {OnGameModeChanged?.GetInvocationList()?.Length ?? 0}</color>");
            OnGameModeChanged?.Invoke(GameMode.UI);
            Debug.Log("<color=red>Switched to UI mode</color>");
        }
    }
}
