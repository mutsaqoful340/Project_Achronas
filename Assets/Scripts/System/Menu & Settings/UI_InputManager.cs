using UnityEngine;

/// <summary>
/// Manages multi-controller UI input arbitration with cooldown system.
/// Allows only one controller to control UI at a time, with idle detection and cooldown between controller switches.
/// </summary>
public class UI_InputManager : MonoBehaviour
{
    public static UI_InputManager Instance { get; private set; }

    [Header("Timing")]
    [SerializeField] private float idleDuration = 1f;  // Time before idle timer starts
    [SerializeField] private float cooldownDuration = 2f;  // Time after idle before next controller can take over

    private int activeControllerIndex = -1;  // -1 = none, 0 = controller 0, 1 = controller 1
    private float lastInputTime = 0f;  // Time of last input from active controller
    private float idleStartTime = 0f;  // When active controller started being idle
    private bool isInCooldown = false;  // Whether we're in the cooldown phase
    private float cooldownStartTime = 0f;  // When cooldown started

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        lastInputTime = Time.time;
    }

    void Update()
    {
        // If no controller is active, skip
        if (activeControllerIndex == -1) return;

        float timeSinceLastInput = Time.time - lastInputTime;

        // Check if controller is idle
        if (timeSinceLastInput >= idleDuration && !isInCooldown)
        {
            // Start cooldown
            isInCooldown = true;
            cooldownStartTime = Time.time;
            Debug.Log($"[COOLDOWN START] Controller {activeControllerIndex} idle for {timeSinceLastInput:F2}s, ENTERING COOLDOWN for {cooldownDuration}s");
        }

        // Check if cooldown is over
        if (isInCooldown)
        {
            float timeSinceCooldownStart = Time.time - cooldownStartTime;
            if (timeSinceCooldownStart >= cooldownDuration)
            {
                // Cooldown over, release control
                isInCooldown = false;
                activeControllerIndex = -1;
                Debug.Log($"[COOLDOWN END] Cooldown lasted {timeSinceCooldownStart:F2}s, control RELEASED");
            }
            else
            {
                // Still in cooldown - log for debugging
                Debug.Log($"[IN COOLDOWN] Controller 1 attempting input, but still {(cooldownDuration - timeSinceCooldownStart):F2}s remaining");
            }
        }
    }

    /// <summary>
    /// Check if a controller can use UI right now.
    /// </summary>
    public bool CanControllerUseUI(int controllerIndex)
    {
        // If no one has control, this controller can take it
        if (activeControllerIndex == -1 && !isInCooldown)
        {
            Debug.Log($"[ALLOW] Controller {controllerIndex} CAN take control (no one active, no cooldown)");
            return true;
        }

        // If this controller already has control, allow it
        if (activeControllerIndex == controllerIndex)
        {
            Debug.Log($"[ALLOW] Controller {controllerIndex} already has control");
            return true;
        }

        // If another controller has control or we're in cooldown, reject
        Debug.Log($"[DENY] Controller {controllerIndex} BLOCKED - activeController={activeControllerIndex}, inCooldown={isInCooldown}");
        return false;
    }

    /// <summary>
    /// Notify that a controller provided input. Call this whenever a controller tries to use UI.
    /// </summary>
    public void NotifyControllerInput(int controllerIndex)
    {
        Debug.Log($"\n>>> Controller {controllerIndex} provided input");
        Debug.Log($"    Before: activeIdx={activeControllerIndex}, inCooldown={isInCooldown}");

        // If this controller doesn't have control, try to take it
        if (activeControllerIndex != controllerIndex)
        {
            if (!CanControllerUseUI(controllerIndex))
            {
                // Silently reject - cannot use UI right now
                Debug.Log($"    Result: INPUT REJECTED\n");
                return;
            }

            // Take control
            int previousController = activeControllerIndex;
            activeControllerIndex = controllerIndex;
            isInCooldown = false;
            Debug.Log($"    Result: TOOK CONTROL (was Controller {previousController})");
        }
        else
        {
            // If this controller already has control and we're in cooldown, cancel the cooldown
            if (isInCooldown)
            {
                isInCooldown = false;
                Debug.Log($"    Result: Cancelled cooldown, continuing control");
            }
            else
            {
                Debug.Log($"    Result: Already controlling");
            }
        }

        // Update last input time (reset idle timer)
        lastInputTime = Time.time;
        Debug.Log($"    After: activeIdx={activeControllerIndex}, inCooldown={isInCooldown}");
        Debug.Log($"    Idle timer reset\n");
    }

    /// <summary>
    /// Reset the idle/cooldown timers for the current controller.
    /// </summary>
    private void ResetIdleTimer()
    {
        lastInputTime = Time.time;
    }

    /// <summary>
    /// Force release control (e.g., when exiting UI entirely).
    /// </summary>
    public void ReleaseControl()
    {
        activeControllerIndex = -1;
        isInCooldown = false;
        Debug.Log($"UI Input: Control released");
    }

    /// <summary>
    /// Get the current controller with UI control, or -1 if none.
    /// </summary>
    public int GetActiveControllerIndex()
    {
        return activeControllerIndex;
    }

    /// <summary>
    /// Check if we're currently in cooldown phase.
    /// </summary>
    public bool IsInCooldown()
    {
        return isInCooldown;
    }
}
