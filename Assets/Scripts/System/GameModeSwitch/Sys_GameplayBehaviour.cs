using UnityEngine;

/// <summary>
/// Base class for scripts that should only be active during Gameplay mode
/// </summary>
public abstract class Sys_GameplayBehaviour : MonoBehaviour
{
    protected bool isActive = false;
    private bool hasSubscribed = false;

    protected virtual void Start()
    {
        // Subscribe in Start() to ensure _GameModeSwitch.Awake() has run first
        TrySubscribe();
    }

    protected virtual void OnEnable()
    {
        // Try to subscribe (will work if Instance already exists)
        TrySubscribe();
    }

    protected virtual void OnDisable()
    {
        if (Sys_GameModeSwitch.Instance != null && hasSubscribed)
        {
            Sys_GameModeSwitch.Instance.OnGameModeChanged -= OnGameModeChanged;
            hasSubscribed = false;
        }
    }

    private void TrySubscribe()
    {
        if (hasSubscribed) return; // Already subscribed
        
        if (Sys_GameModeSwitch.Instance != null)
        {
            Sys_GameModeSwitch.Instance.OnGameModeChanged += OnGameModeChanged;
            hasSubscribed = true;
            // Set initial state based on current mode
            OnGameModeChanged(Sys_GameModeSwitch.Instance.currentMode);
        }
    }

    private void OnGameModeChanged(Sys_GameModeSwitch.GameMode mode)
    {
        if (mode == Sys_GameModeSwitch.GameMode.Player)
        {
            isActive = true;
            OnGameplayEnabled();
        }
        else
        {
            isActive = false;
            OnGameplayDisabled();
        }
    }

    /// <summary>
    /// Called when Gameplay mode is activated
    /// </summary>
    protected virtual void OnGameplayEnabled()
    {
        // Override this in derived classes
    }

    /// <summary>
    /// Called when Gameplay mode is deactivated
    /// </summary>
    protected virtual void OnGameplayDisabled()
    {
        // Override this in derived classes
    }
}
