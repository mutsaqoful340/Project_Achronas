using System;
using UnityEngine;
using UnityEngine.InputSystem;


public class _Sys_GameModeSwitch : MonoBehaviour
{
    public static _Sys_GameModeSwitch Instance { get; private set; }

    public PlayerInput[] PlayerInput;
    public enum GameMode
    {
        Player,
        UI
    }

    public GameMode currentMode = GameMode.UI;
    
    // Per-player mode tracking
    private GameMode[] playerModes;

    // Event that fires when game mode changes (global - affects all players)
    public event Action<GameMode> OnGameModeChanged;
    
    // Event that fires when a specific player's mode changes (playerIndex, newMode)
    public event Action<int, GameMode> OnPlayerModeChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize per-player modes
            if (PlayerInput != null)
            {
                playerModes = new GameMode[PlayerInput.Length];
                for (int i = 0; i < playerModes.Length; i++)
                {
                    playerModes[i] = currentMode;
                }
            }
            
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
            for (int i = 0; i < PlayerInput.Length; i++)
            {
                if (PlayerInput[i] != null)
                {
                    PlayerInput[i].SwitchCurrentActionMap(actionMap);
                    if (playerModes != null && i < playerModes.Length)
                    {
                        playerModes[i] = mode;
                    }
                }
            }
        }
        
        // CRITICAL: Disable/Enable player input modules based on game mode
        // This prevents player actions from firing while in UI mode
        Player_Components[] allPlayers = FindObjectsByType<Player_Components>();
        foreach (var player in allPlayers)
        {
            if (player != null && player.moduleInputPlay != null)
            {
                if (mode == GameMode.UI)
                {
                    player.moduleInputPlay.DisablePlayerActions();
                }
                else
                {
                    player.moduleInputPlay.EnablePlayerActions();
                }
            }
        }
        
        // Invoke event
        OnGameModeChanged?.Invoke(mode);
        Debug.Log($"<color={(mode == GameMode.Player ? "green" : "yellow")}>Switched ALL players to {mode} mode</color>");
    }
    
    /// <summary>
    /// Set the game mode for a specific player by index
    /// </summary>
    public void SetPlayerMode(int playerIndex, GameMode mode)
    {
        if (PlayerInput == null || playerIndex < 0 || playerIndex >= PlayerInput.Length)
        {
            Debug.LogWarning($"Invalid player index: {playerIndex}");
            return;
        }
        
        if (playerModes != null && playerIndex < playerModes.Length && playerModes[playerIndex] == mode)
        {
            return; // Already in this mode
        }
        
        // Switch the specific player's action map
        string actionMap = (mode == GameMode.Player) ? "Player" : "UI";
        if (PlayerInput[playerIndex] != null)
        {
            PlayerInput[playerIndex].SwitchCurrentActionMap(actionMap);
            if (playerModes != null && playerIndex < playerModes.Length)
            {
                playerModes[playerIndex] = mode;
            }
        }
        
        // Disable/Enable this specific player's input module
        Player_Components[] allPlayers = FindObjectsByType<Player_Components>();
        if (playerIndex < allPlayers.Length && allPlayers[playerIndex] != null && allPlayers[playerIndex].moduleInputPlay != null)
        {
            if (mode == GameMode.UI)
            {
                allPlayers[playerIndex].moduleInputPlay.DisablePlayerActions();
            }
            else
            {
                allPlayers[playerIndex].moduleInputPlay.EnablePlayerActions();
            }
        }
        
        // Invoke per-player event
        OnPlayerModeChanged?.Invoke(playerIndex, mode);
        Debug.Log($"<color={(mode == GameMode.Player ? "green" : "yellow")}>Switched Player {playerIndex} to {mode} mode</color>");
    }
    
    /// <summary>
    /// Get the current mode for a specific player
    /// </summary>
    public GameMode GetPlayerMode(int playerIndex)
    {
        if (playerModes != null && playerIndex >= 0 && playerIndex < playerModes.Length)
        {
            return playerModes[playerIndex];
        }
        return currentMode; // Fallback to global mode
    }
}
