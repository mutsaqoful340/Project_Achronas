using UnityEngine;
using TMPro;
using UnityEngine.Events;
using NUnit.Framework.Internal;
using Unity.Collections;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEditor.EditorTools;

public class _Enemy_Boss : MonoBehaviour
{
    public enum BossType
    {
        DadakMerak,
        Leak,
        Hanoman
    }

    enum EnemyState
    {
        Idle,
        Alerted,
        CaughtPlayer
    }

    [Header("Enemy Type")]
    public BossType bossType;

    [Header("Enemy Properties")]
    public Light detectionLight;
    public Animator animator;

    [Header("Visual Detection Settings")]
    [Tooltip("Detection range (uses spotlight range if available)")]
    [SerializeField] private float detectionRange = 10f;
    [Tooltip("Detection cone angle (uses spotlight angle if available)")]
    [SerializeField] private float detectionAngle = 60f;
    [Tooltip("Layer mask for line of sight detection")]
    [SerializeField] private LayerMask detectionLayer;
    [Tooltip("Raycast offset from player position (chest height)")]
    [SerializeField] private float raycastOffset = 1f;
    [Tooltip("How often to check for player (in seconds)")]
    [SerializeField] private float detectionInterval = 0.15f;

    [Header("Awareness Settings")]
    [Tooltip("How long player must be in sight before enemy investigates")]
    [SerializeField] private float awarenessThreshold = 2f;
    [Tooltip("How fast awareness increases per second when player visible")]
    [SerializeField] private float awarenessIncreaseRate = 1f;
    [Tooltip("How fast awareness decreases per second when player not visible")]
    [SerializeField] private float awarenessDecreaseRate = 0.5f;
    [SerializeField] private float currentAwareness = 0f;

    [Header("Catch Player Settings")]
    [Tooltip("Slot for caught player (e.g., for parenting or animation)")]
    public Transform caughtPlayerSlot;
    [SerializeField] private Player_Components caughtPlayerComponent; // Reference to caught player's components for control

    [Header("Mannequinn Interaction")]
    public _Enemy_Mannequin[] enemyMannequin;

    [Header("Spotting Events")]
    public UnityEvent OnSpottingPlayer;

    // State management
    private EnemyState currentState = EnemyState.Idle;
    
    // Navigation
    private NavMeshAgent navAgent;

    // Detection variables
    private List<GameObject> detectedPlayers = new List<GameObject>();
    private GameObject cachedPlayer;
    private float nextDetectionTime;
    private bool isPlayerVisible = false;
    private Vector3 lastKnownPlayerPosition;
    
    // Chase variables
    private float defaultAngularSpeed;

    private void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        
        if (navAgent == null)
        {
            Debug.LogError($"{gameObject.name}: NavMeshAgent component is missing!");
            return;
        }
        
        // Store default angular speed for restoration
        defaultAngularSpeed = navAgent.angularSpeed;
        
        // Ensure updateRotation is enabled (critical for angularSpeed to work)
        if (!navAgent.updateRotation)
        {
            navAgent.updateRotation = true;
        }

        // Start in idle state
        TransitionToState(EnemyState.Idle);
        Debug.Log($"{gameObject.name}: Initialized. Waiting for player detection.");
        detectionLight.enabled = true; // Ensure detection light is on at start

        // Validate mannequins ONLY for DadakMerak (the only type that needs them)
        if (bossType == BossType.DadakMerak)
        {
            if (enemyMannequin == null || enemyMannequin.Length == 0)
            {
                Debug.LogWarning($"{gameObject.name} (DadakMerak): No mannequins assigned! Cannot control mannequins.");
            }
        }
    }

    private void Update()
    {
        // OPTIMIZATION: Check for player at intervals (more frequently during chase)
        float currentInterval = currentState == EnemyState.Alerted ? 0.05f : detectionInterval;
        
        if (Time.time >= nextDetectionTime)
        {
            nextDetectionTime = Time.time + currentInterval;
            isPlayerVisible = IsPlayerInLOS();
        }

        // Update awareness based on player visibility
        UpdateAwareness();

        // Execute current state behavior
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle();
                break;
            case EnemyState.Alerted:
                HandleAlerted();
                break;
            case EnemyState.CaughtPlayer:
                HandleCatchingPlayer();
                break;
        }
    }

    #region State Handlers
    private void HandleIdle()
    {
        // Stationary guard - stays at post, watches for player
        // TODO: Can add idle animations, looking around, etc.
        // Will transition to Chase if player detected
        // Returns to Idle after losing player (unlike Patrol enemies)
    }

    private void HandleAlerted()
    {
        switch (bossType)
        {
            case BossType.Hanoman:
            case BossType.Leak:
                break;
            case BossType.DadakMerak:
                break;
        }
    }

    private void HandleCatchingPlayer()
    {
        // Continuous behavior while catching player (e.g., play animation, lock player)
        // One-time event (OnPlayerCaught) is called in TransitionToState when entering this state
    }
    #endregion

    #region State Transition
    private void TransitionToState(EnemyState newState)
    {
        // Guard clause: prevent duplicate transitions
        if (currentState == newState) return;

        // // Exit current state (if cleanup needed)
        // switch (currentState)
        // {
        //     // Add exit logic here if needed
        // }

        currentState = newState;

        // Enter new state - ONE-TIME EVENTS GO HERE
        switch (newState)
        {
            case EnemyState.Idle:
                // Idle entry behavior
                break;

            case EnemyState.Alerted:
                OnSpottingPlayer?.Invoke(); // Invoked once when player is spotted
                OnPlayerSpotted(); // Call spotted method
                break;

            case EnemyState.CaughtPlayer:
                OnPlayerCaught(); // Called once when player is caught
                break;
        }
    }
    #endregion

    #region Detection Methods
    private bool IsPlayerInLOS()
    {
        // Clear previous detections
        detectedPlayers.Clear();
        
        // Find ALL players in scene
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        if (allPlayers.Length == 0)
            return false;

        // Use spotlight properties if available, otherwise use manual settings
        float range = detectionLight != null ? detectionLight.range : detectionRange;
        float angle = detectionLight != null ? detectionLight.spotAngle : detectionAngle;

        Vector3 enemyPosition = detectionLight != null ? detectionLight.transform.position : transform.position;
        Vector3 enemyForward = detectionLight != null ? detectionLight.transform.forward : transform.forward;
        
        // Check each player
        foreach (GameObject player in allPlayers)
        {
            if (player == null)
                continue;
                
            Vector3 playerCenter = player.transform.position + Vector3.up * raycastOffset;
            Vector3 toPlayer = playerCenter - enemyPosition;
            
            // 1. Distance check (optimized with sqrMagnitude)
            float sqrDistance = toPlayer.sqrMagnitude;
            float sqrRange = range * range;
            if (sqrDistance > sqrRange)
            {
                Debug.DrawRay(enemyPosition, toPlayer.normalized * range, Color.red);
                continue;
            }

            // 2. Angle check
            Vector3 directionToPlayer = toPlayer.normalized;
            float angleToPlayer = Vector3.Angle(enemyForward, directionToPlayer);
            if (angleToPlayer > angle / 2f)
            {
                Debug.DrawRay(enemyPosition, directionToPlayer * Mathf.Sqrt(sqrDistance), Color.red);
                continue;
            }

            // 3. Raycast check (most expensive, done last)
            float actualDistance = Mathf.Sqrt(sqrDistance);
            if (Physics.Raycast(enemyPosition, directionToPlayer, out RaycastHit hit, actualDistance, detectionLayer))
            {
                // CRITICAL: Check if we hit THIS specific player, not just any player
                bool hitThisPlayer = hit.collider.gameObject == player || hit.collider.transform.IsChildOf(player.transform);
                
                if (hitThisPlayer)
                {
                    Debug.DrawRay(enemyPosition, directionToPlayer * hit.distance, Color.green);
                    detectedPlayers.Add(player); // Add to detected players list
                    lastKnownPlayerPosition = playerCenter;
                    cachedPlayer = player; // Cache for single-player targeting
                }
                else
                {
                    // Hit something else (wall or different player) - this player is BLOCKED
                    Debug.DrawRay(enemyPosition, directionToPlayer * hit.distance, Color.yellow);
                }
            }
            else
            {
                Debug.DrawRay(enemyPosition, directionToPlayer * actualDistance, Color.red);
            }
        }
        // Return true if ANY player was detected
        return detectedPlayers.Count > 0;
    }

    private void UpdateAwareness()
    {
        if (isPlayerVisible)
        {
            // Player visible - increase awareness
            currentAwareness += awarenessIncreaseRate * Time.deltaTime;
            currentAwareness = Mathf.Clamp(currentAwareness, 0f, awarenessThreshold);

            // Invoke event when threshold reached
            if (currentAwareness >= awarenessThreshold && currentState != EnemyState.Alerted)
            {
                TransitionToState(EnemyState.Alerted);
            }
        }
        else
        {
            // Player not visible - decrease awareness
            currentAwareness -= awarenessDecreaseRate * Time.deltaTime;
            currentAwareness = Mathf.Max(0f, currentAwareness);
        }
    }
    #endregion

    #region Catching Methods
    private void CatchPlayer()
    {
        if (cachedPlayer != null)
        {
            // Attempt to get Player_Components from the caught player
            caughtPlayerComponent = cachedPlayer.GetComponent<Player_Components>();
            
            if (caughtPlayerComponent != null)
            {
                // Parent player to the caught player slot
                if (caughtPlayerSlot != null)
                {
                    cachedPlayer.transform.SetParent(caughtPlayerSlot);
                    cachedPlayer.transform.localPosition = Vector3.zero;
                    cachedPlayer.transform.localRotation = Quaternion.identity;
                    Debug.Log($"{gameObject.name}: Player parented to caught slot.");
                }
                else
                {
                    Debug.LogWarning($"{gameObject.name}: caughtPlayerSlot is not assigned!");
                }
                
                // Freeze player movement
                FreezePlayer();
            }
            else
            {
                Debug.LogError($"{gameObject.name}: Caught player {cachedPlayer.name} does not have Player_Components!");
            }
        }
        else
        {
            Debug.LogError($"{gameObject.name}: No cached player to catch!");
        }
    }

    private void FreezePlayer()
    {
        if (cachedPlayer == null)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot freeze player - cachedPlayer is null!");
            return;
        }

        // Method 1: Disable CharacterController
        CharacterController controller = cachedPlayer.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            Debug.Log($"{gameObject.name}: Disabled CharacterController on {cachedPlayer.name}");
        }
        
        // Method 2: Disable Rigidbody physics
        Rigidbody rb = cachedPlayer.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            Debug.Log($"{gameObject.name}: Froze Rigidbody on {cachedPlayer.name}");
        }
        
        // Method 3: Disable input (if using Unity Input System)
        UnityEngine.InputSystem.PlayerInput playerInput = cachedPlayer.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
        {
            playerInput.DeactivateInput();
            Debug.Log($"{gameObject.name}: Deactivated input on {cachedPlayer.name}");
        }
        
        // Method 4: Call Player_Components freeze method (if it exists)
        if (caughtPlayerComponent != null)
        {
            // TODO: Call specific freeze methods on Player_Components
            // Example: caughtPlayerComponent.SetMovementLocked(true);
            // Example: caughtPlayerComponent.DisableControl();
        }
    }
    #endregion

    #region Public Methods
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"{gameObject.name}: Player entered trigger zone.");
            TransitionToState(EnemyState.CaughtPlayer);
        }
    }

    public void OnPlayerCaught()
    {
        switch (bossType)
        {
            case BossType.DadakMerak:
                break;
                
            case BossType.Hanoman:
                Debug.Log($"{gameObject.name} (Hanoman): Player caught! You are ded!");
                CatchPlayer();
                break;
                
            case BossType.Leak:
                Debug.Log($"{gameObject.name} (Leak): Player caught! You are ded!");
                CatchPlayer();
                break;
        }
    }

    public void OnPlayerSpotted()
    {
        switch (bossType)
        {
            case BossType.DadakMerak:
                Debug.Log($"{gameObject.name} (Dadak Merak): Player spotted! ");
                break;
            case BossType.Hanoman:
            case BossType.Leak:
                Debug.Log($"{gameObject.name} ({bossType}): Player spotted! Do nothing.");
                break;
        }
    }
    #endregion
}