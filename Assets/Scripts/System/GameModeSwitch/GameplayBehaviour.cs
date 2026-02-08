using UnityEngine;

/// <summary>
/// Base class for scripts that should only be active during Gameplay mode
/// </summary>
public abstract class GameplayBehaviour : MonoBehaviour
{
    protected bool isActive = false;

    protected virtual void OnEnable()
    {
        if (_GameModeSwitch.Instance != null)
        {
            _GameModeSwitch.Instance.OnGameModeChanged += OnGameModeChanged;
            // Set initial state based on current mode
            OnGameModeChanged(_GameModeSwitch.Instance.currentMode);
        }
    }

    protected virtual void OnDisable()
    {
        if (_GameModeSwitch.Instance != null)
        {
            _GameModeSwitch.Instance.OnGameModeChanged -= OnGameModeChanged;
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
