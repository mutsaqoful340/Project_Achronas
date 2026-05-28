using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// This script will handle the player's carry system, which includes balancing and other related mechanics.
/// It will interact with the player's components and animator to manage the carry state and its effects on the player.
/// ONLY APPLIES TO NAYA
/// </summary>

public class GP_PlayerCarrySystem : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public GP_PlayerSanity playerSanity;

    [Header("Carry Settings")]
    public Transform carryPoint;
    public float carryDistance = 2f;
    
    #region Carry State
    [SerializeField] private Player_Components currentCarriedPlayer;
    private bool isCurrentlyCarrying = false;
    [SerializeField] private Player_Components nearbyPlayer; // For debugging, shows any player in proximity regardless of state
    [SerializeField] private Player_Components nearbyDepressedPlayer;
    #endregion

    #region Public Properties
    public bool IsCarrying => isCurrentlyCarrying;
    public Player_Components CarriedPlayer => currentCarriedPlayer;
    public float BalanceMeter => balanceMeter;
    #endregion

    private CharacterController controller;
    private Player_Components playerComponents;

    #region Balance System
    [Header("Balance Settings")]
    [SerializeField] 
    [Tooltip("How much turning affects the balance meter. Higher = more sway from rotation")]
    private float turnSwayStrength = 2f;
    
    [SerializeField] 
    [Tooltip("How fast the balance meter returns to center when not disturbed. Higher = faster recovery")]
    private float dampingFactor = 3f;
    
    [SerializeField] 
    [Tooltip("How much player input (left/right stick) reduces sway. Higher = more effective stabilization")]
    private float stabilizationStrength = 2f;
    
    [SerializeField] 
    [Tooltip("Magnitude of involuntary sideways movement. Higher = player drifts more when swaying")]
    private float swayAmount = 0.5f;
    
    [SerializeField] 
    [Tooltip("Speed at which sway movement occurs. Higher = faster sideways drift")]
    private float swaySpeed = 2f;
    
    [SerializeField] 
    [Tooltip("Maximum sway velocity per frame to prevent excessive movement. Higher = allows bigger swings")]
    private float swayClampMax = 0.2f;
    
    [SerializeField] 
    [Tooltip("Smoothness of sway blending into movement (0-1). Higher = snappier response")]
    private float swayBlendFactor = 0.1f;
    
    private float balanceMeter = 0f;        // -1 (left) to 1 (right)
    private float lastYRotation = 0f;
    public Vector3 SwayVelocity { get; private set; } = Vector3.zero;
    public float SwayClampMax => swayClampMax;
    public float SwayBlendFactor => swayBlendFactor;
    #endregion

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        
        controller = GetComponent<CharacterController>();
        playerComponents = GetComponent<Player_Components>();
    }

    /// <summary>
    /// Check if Naya can carry the target player
    /// </summary>
    public bool CanCarry(Player_Components targetPlayer)
    {
        if (targetPlayer == null)
            return false;

        // Check if target is in Depressed state
        if (targetPlayer.currentActionState != ActionState.Depressed)
        {
            Debug.Log($"Target is not depressed. Current state: {targetPlayer.currentActionState}");
            return false;
        }

        // Check distance
        float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (distance > carryDistance)
        {
            Debug.Log($"Target too far. Distance: {distance:F2}m, Carry distance: {carryDistance}m");
            return false;
        }

        // Check if already carrying someone
        if (isCurrentlyCarrying)
        {
            Debug.Log("Already carrying someone");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Start carrying the target player
    /// </summary>
    public void StartCarrying(Player_Components targetPlayer)
    {
        if (!CanCarry(targetPlayer))
        {
            Debug.LogWarning("Cannot carry - validation failed");
            return;
        }

        currentCarriedPlayer = targetPlayer;
        isCurrentlyCarrying = true;

        // Parent the carried player to the carry point
        if (carryPoint != null)
        {
            targetPlayer.transform.parent = carryPoint;
            targetPlayer.transform.localPosition = Vector3.zero;
            targetPlayer.transform.localRotation = Quaternion.identity;
        }

        var targetcc = targetPlayer.GetComponent<CharacterController>();
        if (targetcc != null)
        {
            targetcc.enabled = false; // Disable character controller to prevent physics issues
        }

        // Update animator
        animator.SetTrigger("DoCarry");
        targetPlayer.animator.SetTrigger("DoCarry");
        
        // Notify sanity system
        if (playerSanity != null)
        {
            playerSanity.OnCarryStarted();
        }

        Debug.Log($"Naya is now carrying {targetPlayer.gameObject.name}");
    }

    /// <summary>
    /// Stop carrying the current player
    /// </summary>
    public void StopCarrying()
    {
        if (!isCurrentlyCarrying || currentCarriedPlayer == null)
            return;

        // Unparent the carried player
        currentCarriedPlayer.transform.parent = null;

        // Update animator
        animator.SetTrigger("DoUncarry");
        currentCarriedPlayer.animator.SetTrigger("DoUncarry");

        // Notify sanity system FIRST
        if (playerSanity != null)
        {
            playerSanity.OnCarryEnded();
        }

        // Re-enable character controller AFTER sanity updates
        var currentCarriedPlayerCC = currentCarriedPlayer.GetComponent<CharacterController>();
        if (currentCarriedPlayerCC != null)
        {
            currentCarriedPlayerCC.enabled = true;
            Debug.Log($"CharacterController re-enabled for {currentCarriedPlayer.gameObject.name}");
        }

        Debug.Log($"Naya stopped carrying {currentCarriedPlayer.gameObject.name}");

        currentCarriedPlayer = null;
        isCurrentlyCarrying = false;
    }

    private void Update()
    {
        if (isCurrentlyCarrying)
        {
            UpdateBalance();
        }
    }

    /// <summary>
    /// Updates balance meter based on turn sway, stabilization input, and natural damping
    /// Also calculates involuntary sideways movement
    /// </summary>
    private void UpdateBalance()
    {
        // Calculate rotation change (turn sway)
        float currentYRotation = transform.eulerAngles.y;
        float rotationDelta = Mathf.DeltaAngle(lastYRotation, currentYRotation);
        lastYRotation = currentYRotation;

        // Turn creates opposite sway (turn left → sway right)
        float turnSway = rotationDelta * turnSwayStrength;

        // Get lateral stabilization input from player
        float lateralInput = 0f;
        if (playerComponents != null && playerComponents.moduleInputPlay != null && playerComponents.assignedDevice != null)
        {
            Vector3 moveInput = playerComponents.moduleInputPlay.GetMoveInput(playerComponents.assignedDevice);
            lateralInput = moveInput.x;
        }

        // Stabilization opposes sway
        float stabilization = lateralInput * stabilizationStrength;

        // Natural damping back to center
        float damping = -balanceMeter * dampingFactor;

        // Apply combined sway
        balanceMeter += (turnSway + damping - stabilization) * Time.deltaTime;
        balanceMeter = Mathf.Clamp(balanceMeter, -1f, 1f);

        // Calculate involuntary sideways movement
        float lateralDisplacement = balanceMeter * swayAmount;
        SwayVelocity = transform.right * lateralDisplacement * swaySpeed;
    }

    /// <summary>
    /// Detect nearby depressed players via trigger collider
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"OnTriggerStay triggered by: {other.gameObject.name}");
            var player = other.GetComponentInParent<Player_Components>();
            if (player != null && (player.currentActionState == ActionState.Depressed))
            {
                nearbyDepressedPlayer = player;
            }
        }
    }

    /// <summary>
    /// Clear reference when depressed player leaves proximity
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Player_Components>() == nearbyDepressedPlayer)
        {
            nearbyDepressedPlayer = null;
        }
    }

    /// <summary>
    /// Attempt to carry nearby depressed player
    /// </summary>
    public void AttemptCarry()
    {
        if (nearbyDepressedPlayer != null && CanCarry(nearbyDepressedPlayer))
        {
            StartCarrying(nearbyDepressedPlayer);
        }
        else if (nearbyDepressedPlayer == null)
        {
            Debug.Log("No depressed player nearby");
        }
    }

    public bool HasNearbyDepressedPlayer()
    {
        return nearbyDepressedPlayer != null;
    }
}