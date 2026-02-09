using UnityEngine;

/// <summary>
/// Base class for scripts that should only be active during Gameplay mode
/// </summary>
public abstract class GameplayBehaviour : MonoBehaviour
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
        if (_GameModeSwitch.Instance != null && hasSubscribed)
        {
            _GameModeSwitch.Instance.OnGameModeChanged -= OnGameModeChanged;
            hasSubscribed = false;
        }
    }

    private void TrySubscribe()
    {
        if (hasSubscribed) return; // Already subscribed
        
        if (_GameModeSwitch.Instance != null)
        {
            _GameModeSwitch.Instance.OnGameModeChanged += OnGameModeChanged;
            hasSubscribed = true;
            // Set initial state based on current mode
            OnGameModeChanged(_GameModeSwitch.Instance.currentMode);
        }
    }

    private void OnGameModeChanged(_GameModeSwitch.GameMode mode)
    {
        if (mode == _GameModeSwitch.GameMode.Player)
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
