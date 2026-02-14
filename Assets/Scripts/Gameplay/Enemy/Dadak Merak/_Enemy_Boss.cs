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
    // 
    public enum EnemyType
    {
        DadakMerak,
        Leak,
        Hanoman
    }

    enum EnemyState
    {
        Idle,
        Alerted
    }

    [Header("Enemy Type")]
    public EnemyType enemyType;

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

    [Header("Mannequinn Interaction")]
    public _Enemy_Mannequin[] enemyMannequin;
    
    [Header("Light Reaction")]
    public UnityEvent onLitByPlayerLight;

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
    
    // Enemy references

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
        currentState = EnemyState.Idle;
        Debug.Log($"{gameObject.name}: Initialized. Waiting for player detection.");
        detectionLight.enabled = true; // Ensure detection light is on at start
        
        // Cache Mannequin reference if available
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
        switch (enemyType)
        {
            case EnemyType.Hanoman:
            case EnemyType.Leak:
                break;
            case EnemyType.DadakMerak:
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
                currentState = EnemyState.Alerted;
                OnSpottingPlayer?.Invoke();
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

    #region Public Methods
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerLight"))
        {
            OnPlayerCaught();
        }
    }

    public void OnLitByPlayerLight()
    {
        // Behavior based on enemy type
        switch (enemyType)
        {
            case EnemyType.DadakMerak:
                Debug.Log($"{gameObject.name} (Dadak Merak): Spotted by light! Fleeing...");
                // TODO: Implement Dadak Merak-specific behavior (flee from light)
                break;
                
            case EnemyType.Hanoman:
                Debug.Log($"{gameObject.name} (Hanoman): Spotted by light! Becoming aggressive...");
                // TODO: Implement Hanoman-specific behavior (chase player)
                break;
                
            case EnemyType.Leak:
                Debug.Log($"{gameObject.name} (Leak): Spotted by light!");
                // TODO: Implement Leak-specific behavior
                break;
        }
        
        // Invoke Unity Event for Inspector-assigned reactions
        onLitByPlayerLight?.Invoke();
    }

    public void OnPlayerCaught()
    {
        switch (enemyType)
        {
            case EnemyType.DadakMerak:
                Debug.Log($"{gameObject.name} (Dadak Merak): Caught by player light!");
                // Call method on all Mannequins
                if (enemyMannequin != null && enemyMannequin.Length > 0)
                {
                    foreach (_Enemy_Mannequin mannequin in enemyMannequin)
                    {
                        if (mannequin != null)
                        {
                            mannequin.OnDadakMerakCommand();
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"{gameObject.name}: No _Enemy_Mannequin components assigned!");
                }
                break;
                
            case EnemyType.Hanoman:
                Debug.Log($"{gameObject.name} (Hanoman): Caught by player light!");
                break;
                
            case EnemyType.Leak:
                Debug.Log($"{gameObject.name} (Leak): Caught by player light!");
                break;
        }
    }
    #endregion
}