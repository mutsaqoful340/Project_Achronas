using UnityEngine;

/// <summary>
/// Base class for scripts that should only be active during UI mode
/// </summary>
public abstract class UIBehaviour : MonoBehaviour
{
    protected bool isActive = false;

    protected virtual void OnEnable()
    {
        if (_Sys_GameModeSwitch.Instance != null)
        {
            _Sys_GameModeSwitch.Instance.OnGameModeChanged += OnGameModeChanged;
            // Set initial state based on current mode
            OnGameModeChanged(_Sys_GameModeSwitch.Instance.currentMode);
        }
    }

    protected virtual void OnDisable()
    {
        if (_Sys_GameModeSwitch.Instance != null)
        {
            _Sys_GameModeSwitch.Instance.OnGameModeChanged -= OnGameModeChanged;
        }
    }

    private void OnGameModeChanged(_Sys_GameModeSwitch.GameMode mode)
    {
        if (mode == _Sys_GameModeSwitch.GameMode.UI)
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
