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
    [SerializeField] private Player_Components nearbyDepressedPlayer;
    #endregion

    #region Public Properties
    public bool IsCarrying => isCurrentlyCarrying;
    public Player_Components CarriedPlayer => currentCarriedPlayer;
    #endregion

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
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
        animator.SetBool("IsCarry", true);
        targetPlayer.animator.SetBool("IsCarry", true);
        
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
        animator.SetBool("IsCarry", false);
        currentCarriedPlayer.animator.SetBool("IsCarry", false);

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

    public void Balance()
    {
        
    }

    /// <summary>
    /// Detect nearby depressed players via trigger collider
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"OnTriggerStay triggered by: {other.gameObject.name}");
            var player = other.GetComponent<Player_Components>();
            if (player != null && player.currentActionState == ActionState.Depressed)
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
}