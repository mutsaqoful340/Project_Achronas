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

public class _Enemy_Mannequin : MonoBehaviour
{
    public enum EnemyType
    {
        Jaranan,
        SingaBarong,
        Normal
    }

    enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Flee,
        Investigate
    }

    [Header("Enemy Type")]
    public EnemyType enemyType;

    [Header("Enemy Properties")]
    public Light detectionLight;

    [Header("Patrol Settings")]
    [Tooltip("Array of waypoint transforms for patrol route")]
    public Transform[] patrolWaypoints;
    [Tooltip("How close to get to waypoint before moving to next")]
    [SerializeField] private float waypointReachDistance = 0.5f;
    [Tooltip("Time to wait at each waypoint before moving")]
    [SerializeField] private float waypointWaitTime = 2f;
    [Tooltip("How fast to rotate towards next waypoint")]
    [SerializeField] private float waypointRotationSpeed = 5f;

    [Header("Detection Settings")]
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

    [Header("Investigation Settings")]
    [Tooltip("Distance to last known player position to consider 'arrived'")]
    [SerializeField] private float investigateTimer = 0f;
    [Tooltip("How long to investigate last known player position")]
    [SerializeField] private float investigationDuration = 5f; // How long to search before giving up
    
    [Header("Chase Settings")]
    [Tooltip("How fast enemy rotates when chasing player (degrees per second)")]
    [SerializeField] private float chaseRotationSpeed = 360f;
    
    [Header("Light Reaction")]
    public UnityEvent onLitByPlayerLight;

    // State management
    private EnemyState currentState = EnemyState.Patrol;
    
    // Patrol variables
    private NavMeshAgent navAgent;
    private int currentWaypointIndex = 0;
    private float waypointWaitTimer = 0f;
    private bool isWaitingAtWaypoint = false;

    // Detection variables
    private List<GameObject> detectedPlayers = new List<GameObject>();
    private GameObject cachedPlayer;
    private float nextDetectionTime;
    private bool isPlayerVisible = false;
    private Vector3 lastKnownPlayerPosition;
    private bool hasSetInvestigationDestination = false;
    
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

        // Determine initial state based on waypoints
        if (patrolWaypoints.Length > 0)
        {
            // Enemy has patrol route
            currentState = EnemyState.Patrol;
            MoveToCurrentWaypoint();
        }
        else
        {
            // Enemy is stationary guard - stays at starting position
            currentState = EnemyState.Idle;
            Debug.Log($"{gameObject.name}: Stationary guard mode (no waypoints). Will chase if player detected.");
        }

        // Check for Enemy Type to set detection light
        if (enemyType == EnemyType.Jaranan)
        {
            detectionLight.enabled = true;
        }
        else if (enemyType == EnemyType.SingaBarong)
        {
            detectionLight.enabled = true;
        }
        else if (enemyType == EnemyType.Normal)
        {
            detectionLight.enabled = true;
        }
    }

    private void Update()
    {
        // OPTIMIZATION: Check for player at intervals (more frequently during chase)
        float currentInterval = currentState == EnemyState.Chase ? 0.05f : detectionInterval;
        
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
            case EnemyState.Patrol:
                HandlePatrol();
                break;
            case EnemyState.Chase:
                HandleChase();
                break;
            case EnemyState.Flee:
                HandleFlee();
                break;
            case EnemyState.Investigate:
                HandleInvestigate();
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

    private void HandlePatrol()
    {
        if (patrolWaypoints.Length == 0 || navAgent == null)
            return;

        // Check if waiting at waypoint
        if (isWaitingAtWaypoint)
        {
            // Rotate towards next waypoint while waiting
            RotateTowardsNextWaypoint();
            
            waypointWaitTimer -= Time.deltaTime;
            if (waypointWaitTimer <= 0f)
            {
                isWaitingAtWaypoint = false;
                MoveToNextWaypoint();
            }
            return;
        }

        // Check if reached current waypoint
        if (!navAgent.pathPending && navAgent.remainingDistance <= waypointReachDistance)
        {
            // Start waiting at waypoint
            isWaitingAtWaypoint = true;
            waypointWaitTimer = waypointWaitTime;
        }
    }

    private void HandleChase()
    {
        // Set chase rotation speed
        if (navAgent.angularSpeed != chaseRotationSpeed)
        {
            navAgent.angularSpeed = chaseRotationSpeed;
        }
        
        // Chase the player while they're visible
        if (isPlayerVisible && cachedPlayer != null)
        {
            // Continuously update destination to player's current position
            navAgent.SetDestination(cachedPlayer.transform.position);
            lastKnownPlayerPosition = cachedPlayer.transform.position;
        }
        else
        {
            // Lost sight of player - transition to Investigate
            Debug.Log($"{gameObject.name}: Lost sight of player. Investigating last known position...");
            navAgent.angularSpeed = defaultAngularSpeed; // Restore default rotation speed
            hasSetInvestigationDestination = false; // Reset flag for new investigation
            currentState = EnemyState.Investigate;
        }
    }

    private void HandleFlee()
    {
        // TODO: Implement flee behavior
    }

    private void HandleInvestigate()
    {
        // Move to last known player position
        if (!hasSetInvestigationDestination)
        {
            navAgent.SetDestination(lastKnownPlayerPosition);
            investigateTimer = investigationDuration;
            hasSetInvestigationDestination = true;
        }

        // Wait at last known position, looking around
        if (!navAgent.pathPending && navAgent.remainingDistance <= waypointReachDistance)
        {
            investigateTimer -= Time.deltaTime;
            
            // TODO: Add looking around behavior (rotate, check surroundings)
            
            // If player found again, transition to Chase
            if (isPlayerVisible)
            {
                Debug.Log($"{gameObject.name}: Player spotted during investigation! Chasing...");
                currentState = EnemyState.Chase;
                return;
            }
            
            // Give up after timer expires
            if (investigateTimer <= 0f)
            {
                Debug.Log($"{gameObject.name}: Investigation complete. Returning to patrol.");
                currentAwareness = 0f;
                navAgent.angularSpeed = defaultAngularSpeed; // Restore default rotation speed
                
                // Return to appropriate state
                if (patrolWaypoints.Length > 0)
                {
                    // Find and move to nearest waypoint for smart return
                    currentWaypointIndex = FindNearestWaypoint();
                    Debug.Log($"{gameObject.name}: Returning to nearest waypoint {currentWaypointIndex}.");
                    currentState = EnemyState.Patrol;
                    MoveToCurrentWaypoint();
                }
                else
                {
                    currentState = EnemyState.Idle;
                }
            }
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

            // Transition to Chase or Investigate when threshold reached (only for Jaranan and SingaBarong)
            if (currentAwareness >= awarenessThreshold && currentState != EnemyState.Chase && currentState != EnemyState.Investigate)
            {
                if (enemyType == EnemyType.Jaranan || enemyType == EnemyType.SingaBarong)
                {
                    // If player is still visible, chase them directly
                    if (isPlayerVisible && cachedPlayer != null)
                    {
                        Debug.Log($"{gameObject.name}: Player detected and visible! Chasing...");
                        currentState = EnemyState.Chase;
                    }
                    else
                    {
                        // Player was detected but lost sight, investigate last known position
                        Debug.Log($"{gameObject.name}: Player detected but lost sight. Investigating...");
                        hasSetInvestigationDestination = false; // Reset flag for new investigation
                        currentState = EnemyState.Investigate;
                    }
                }
                else
                {
                    // Normal type doesn't investigate, just keeps awareness high
                    Debug.Log($"{gameObject.name}: Player spotted but not investigating (Normal type).");
                }
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

    #region Patrol Methods
    private void MoveToCurrentWaypoint()
    {
        if (patrolWaypoints.Length == 0 || currentWaypointIndex >= patrolWaypoints.Length)
            return;

        Transform targetWaypoint = patrolWaypoints[currentWaypointIndex];
        if (targetWaypoint != null)
        {
            navAgent.SetDestination(targetWaypoint.position);
        }
    }

    private void MoveToNextWaypoint()
    {
        currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
        MoveToCurrentWaypoint();
    }

    private void RotateTowardsNextWaypoint()
    {
        if (patrolWaypoints.Length == 0)
            return;

        // Get next waypoint index
        int nextIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
        if (patrolWaypoints[nextIndex] == null)
            return;

        // Calculate direction to next waypoint
        Vector3 directionToNext = (patrolWaypoints[nextIndex].position - transform.position).normalized;
        
        // Ignore vertical rotation (keep enemy upright)
        directionToNext.y = 0;
        
        if (directionToNext.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToNext);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, waypointRotationSpeed * Time.deltaTime);
        }
    }

    private int FindNearestWaypoint()
    {
        if (patrolWaypoints.Length == 0)
            return 0;

        int nearestIndex = 0;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < patrolWaypoints.Length; i++)
        {
            if (patrolWaypoints[i] == null)
                continue;

            float distance = Vector3.Distance(transform.position, patrolWaypoints[i].position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }
        return nearestIndex;
    }
    #endregion

    #region Public Methods
    public void OnLitByPlayerLight()
    {
        // Behavior based on enemy type
        switch (enemyType)
        {
            case EnemyType.Jaranan:
                Debug.Log($"{gameObject.name} (Jaranan): Spotted by light! Fleeing...");
                // TODO: Implement Jaranan-specific behavior (flee from light)
                break;
                
            case EnemyType.SingaBarong:
                Debug.Log($"{gameObject.name} (Singa Barong): Spotted by light! Becoming aggressive...");
                // TODO: Implement Singa Barong-specific behavior (chase player)
                break;
                
            case EnemyType.Normal:
                Debug.Log($"{gameObject.name} (Normal): Spotted by light!");
                // TODO: Implement Normal-specific behavior
                break;
        }
        
        // Invoke Unity Event for Inspector-assigned reactions
        onLitByPlayerLight?.Invoke();
    }

    public void OnDadakMerakCommand()
    {
        switch (enemyType)
        {
            case EnemyType.Jaranan:
            case EnemyType.SingaBarong:
                Debug.Log($"{gameObject.name} ({enemyType}): Received Dadak Merak command! Fleeing...");
                break;
            case EnemyType.Normal:
                Debug.Log($"{gameObject.name} (Normal): Received Dadak Merak command! No special reaction.");
                break;
        }                
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmos()
    {
        if (patrolWaypoints == null || patrolWaypoints.Length == 0)
            return;

        // Draw waypoint spheres
        Gizmos.color = Color.yellow;
        foreach (Transform waypoint in patrolWaypoints)
        {
            if (waypoint != null)
            {
                Gizmos.DrawWireSphere(waypoint.position, 0.3f);
            }
        }

        // Draw lines between waypoints
        Gizmos.color = Color.cyan;
        for (int i = 0; i < patrolWaypoints.Length; i++)
        {
            if (patrolWaypoints[i] == null)
                continue;

            int nextIndex = (i + 1) % patrolWaypoints.Length;
            if (patrolWaypoints[nextIndex] != null)
            {
                Gizmos.DrawLine(patrolWaypoints[i].position, patrolWaypoints[nextIndex].position);
            }
        }
    }
    #endregion
}