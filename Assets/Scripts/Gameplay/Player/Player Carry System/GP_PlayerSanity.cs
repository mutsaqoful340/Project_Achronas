using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// This script will handle the player's sanity level and its effects on the game.
/// PUT IN A MANAGER GAMEOBJECT, NOT IN THE PLAYER.
/// </summary>
public class GP_PlayerSanity : MonoBehaviour
{
    [Header("References")]
    public Player_Components Rinda;
    public Player_Components Naya;
    public Volume renderVol;
    public GameObject carryDetector;

    [Header("Sanity Settings")]
    public float sanityLevel = 100f;
    public float sanityDecreaseRate = 5f; // Sanity decrease per second when conditions are not met
    public float sanityIncreaseRate = 10f; // Sanity increase per second when conditions are met
    public float maxCarryDuration = 30f; // Maximum duration Naya can carry Rinda (seconds)
    
    [Header("State")]
    public bool IsSanityRecovered = false;
    public bool IsCarried = false;
    public bool IsDetectorActive = false;

    #region Private Variables
    private GP_PlayerCarrySystem carrySystem;
    private float carryDurationTimer = 0f;
    #endregion

    private void Start()
    {
        if (Naya != null)
            carrySystem = Naya.GetComponent<GP_PlayerCarrySystem>();
        if (carryDetector != null)
            carryDetector.SetActive(false);
    }

    private void Update()
    {
        // Sanity recovery while carried
        if (IsCarried)
            RecoverSanity();
    }

    /// <summary>
    /// Continuously deplete sanity until it reaches 0
    /// </summary>
    public void DepleteSanity()
    {
        if (sanityLevel > 0 && !IsCarried)
        {
            sanityLevel = Mathf.Max(0, sanityLevel - sanityDecreaseRate * Time.deltaTime);
            
            if (sanityLevel <= 0)
            {
                OnSanityDepleted();
            }
        }
    }

    /// <summary>
    /// Recover sanity (called when conditions are met)
    /// </summary>
    public void RecoverSanity()
    {
        sanityLevel = Mathf.Min(100f, sanityLevel + sanityIncreaseRate * Time.deltaTime);
        
        // Show drop option when fully recovered (only if being carried)
        if (IsCarried && sanityLevel >= 100f && !IsSanityRecovered)
        {
            IsSanityRecovered = true;
            ShowDropOption();
            carryDurationTimer = maxCarryDuration;
        }
        
        // Force drop after carry duration expires (only if being carried)
        if (IsCarried && IsSanityRecovered && carryDurationTimer > 0)
        {
            carryDurationTimer -= Time.deltaTime;
            if (carryDurationTimer <= 0)
            {
                ForceDrop();
            }
        }
    }

    /// <summary>
    /// Show UI indicator that Rinda is fully recovered and can be dropped
    /// </summary>
    private void ShowDropOption()
    {
        Debug.Log("Rinda is fully recovered! Press [Cancel] to put her down, or continue carrying (max " + maxCarryDuration + "s)");
    }

    /// <summary>
    /// Force drop Rinda if carry duration expires
    /// </summary>
    private void ForceDrop()
    {
        if (carrySystem != null && carrySystem.IsCarrying)
        {
            carrySystem.StopCarrying();
            IsCarried = false;
            Debug.Log("Naya exhausted - forced to drop Rinda");
        }
    }

    /// <summary>
    /// Called when Rinda's sanity reaches 0 (enters Depressed state)
    /// </summary>
    private void OnSanityDepleted()
    {
        Rinda.currentActionState = ActionState.Depressed;
        Rinda.HandleDepressed();
        IsSanityRecovered = false;
        OnDetector();
        Debug.Log("Sanity depleted! Rinda is now depressed.");
    }

    /// <summary>
    /// Called when Rinda's sanity fully recovers (reaches 100)
    /// </summary>
    private void OnSanityRecovered()
    {
        Rinda.currentActionState = ActionState.Idle;
        Rinda.HandleDepressed(); // Toggle off depressed
        IsSanityRecovered = false;
        IsCarried = false;
        Debug.Log("Rinda's sanity fully recovered!");
    }

    /// <summary>
    /// Called by GP_PlayerCarrySystem when carry starts
    /// </summary>
    public void OnCarryStarted()
    {
        IsCarried = true;
        
        // Disable detector when carry is successful
        if (carryDetector != null && IsDetectorActive)
        {
            OnDetector(); // Toggles detector off
        }
        
        Debug.Log("Rinda is now being carried - sanity recovery started");
    }

    /// <summary>
    /// Called by GP_PlayerCarrySystem when carry ends
    /// </summary>
    public void OnCarryEnded()
    {
        IsCarried = false;
        carryDurationTimer = 0f;
        
        if (IsSanityRecovered)
        {
            OnSanityRecovered();
        }
        
        Debug.Log("Carry ended");
    }

    public void OnDetector(){
        IsDetectorActive = !IsDetectorActive;

        if (carryDetector != null)
            carryDetector.SetActive(IsDetectorActive);
        
        Rigidbody rb = carryDetector.GetComponent<Rigidbody>();
        if (rb != null)           
        rb.linearVelocity = Vector3.zero; // Reset velocity to prevent drifting
        rb.isKinematic = true; // Make kinematic to prevent physics interference
    }
}
