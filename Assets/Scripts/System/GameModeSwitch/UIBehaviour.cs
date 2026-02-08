using UnityEngine;

/// <summary>
/// Base class for scripts that should only be active during UI mode
/// </summary>
public abstract class UIBehaviour : MonoBehaviour
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
        if (mode == _GameModeSwitch.GameMode.UI)
        {
            isActive = true;
            OnUIEnabled();
        }
        else
        {
            isActive = false;
            OnUIDisabled();
        }
    }

    /// <summary>
    /// Called when UI mode is activated
    /// </summary>
    protected virtual void OnUIEnabled()
    {
        // Override this in derived classes
    }

    /// <summary>
    /// Called when UI mode is deactivated
    /// </summary>
    protected virtual void OnUIDisabled()
    {
        // Override this in derived classes
    }
}
