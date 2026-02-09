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
            SetMode(GameMode.Player);
        }
        else
        {
            SetMode(GameMode.UI);
        }
    }
    
    /// <summary>
    /// Set the game mode to a specific mode (doesn't toggle)
    /// </summary>
    public void SetMode(GameMode mode)
    {
        if (currentMode == mode)
        {
            Debug.Log($"<color=yellow>Already in {mode} mode, skipping</color>");
            return;
        }
        
        currentMode = mode;
        
        // Switch PlayerInput action maps if any are assigned
        if (PlayerInput != null && PlayerInput.Length > 0)
        {
            string actionMap = (mode == GameMode.Player) ? "Player" : "UI";
            foreach (var playerInput in PlayerInput)
            {
                if (playerInput != null)
                    playerInput.SwitchCurrentActionMap(actionMap);
            }
        }
        
        // Invoke event
        Debug.Log($"<color={(mode == GameMode.Player ? "green" : "red")}>Invoking OnGameModeChanged event with GameMode.{mode}. Subscribers: {OnGameModeChanged?.GetInvocationList()?.Length ?? 0}</color>");
        OnGameModeChanged?.Invoke(mode);
        Debug.Log($"<color={(mode == GameMode.Player ? "green" : "red")}>Switched to {mode} mode</color>");
    }
}
