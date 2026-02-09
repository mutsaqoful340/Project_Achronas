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
            return; // Already in this mode
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
        OnGameModeChanged?.Invoke(mode);
        Debug.Log($"<color={(mode == GameMode.Player ? "green" : "yellow")}>Switched to {mode} mode</color>");
    }
}
