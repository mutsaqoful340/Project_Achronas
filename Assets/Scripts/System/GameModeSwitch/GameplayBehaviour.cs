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
        
        Debug.Log($"<color=cyan>GameplayBehaviour trying to subscribe for {gameObject.name}</color>");
        
        if (_GameModeSwitch.Instance != null)
        {
            _GameModeSwitch.Instance.OnGameModeChanged += OnGameModeChanged;
            hasSubscribed = true;
            Debug.Log($"<color=cyan>Subscribed to OnGameModeChanged. Initial mode: {_GameModeSwitch.Instance.currentMode}</color>");
            // Set initial state based on current mode
            OnGameModeChanged(_GameModeSwitch.Instance.currentMode);
        }
        else
        {
            Debug.LogWarning($"<color=orange>_GameModeSwitch.Instance is NULL for {gameObject.name} - will retry in Start()</color>");
        }
    }

    private void OnGameModeChanged(_GameModeSwitch.GameMode mode)
    {
        Debug.Log($"<color=cyan>GameplayBehaviour.OnGameModeChanged called: mode={mode}</color>");
        
        if (mode == _GameModeSwitch.GameMode.Player)
        {
            isActive = true;
            Debug.Log($"<color=green>Setting isActive=TRUE for {gameObject.name}</color>");
            OnGameplayEnabled();
        }
        else
        {
            isActive = false;
            Debug.Log($"<color=red>Setting isActive=FALSE for {gameObject.name}</color>");
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
