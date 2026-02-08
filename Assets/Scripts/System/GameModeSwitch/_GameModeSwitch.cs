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
            currentMode = GameMode.Player;
            foreach (var playerInput in PlayerInput)
            {
                playerInput.SwitchCurrentActionMap("Player");
            }
            OnGameModeChanged?.Invoke(GameMode.Player);
            Debug.Log("Switched to Player Action Map");
        }
        else
        {
            currentMode = GameMode.UI;
            foreach (var playerInput in PlayerInput)
            {
                playerInput.SwitchCurrentActionMap("UI");
            }
            OnGameModeChanged?.Invoke(GameMode.UI);
            Debug.Log("Switched to UI Action Map");
        }
    }
}
