using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Playables;

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

    [Header("DadakMerak Chase Settings")]
    [Tooltip("Speed at which DadakMerak moves when chasing the player")]
    [SerializeField] private float chaseSpeed = 5f;
    [Tooltip("Rotation speed when chasing player (degrees per second)")]
    [SerializeField] private float chaseRotationSpeed = 360f;
    
    [Header("DadakMerak Dynamic Waypoint Settings")]
    [Tooltip("Distance player must move to spawn a new waypoint")]
    [SerializeField] private float waypointSpawnDistance = 2f;
    [Tooltip("Radius around waypoint to consider it 'reached'")]
    [SerializeField] private float waypointReachDistance = 0.5f;

    [Header("DadakMerak Death Sequence")]
    [Tooltip("Timeline to play when player is caught (for death animation/cutscene)")]
    [SerializeField] private PlayableDirector deathTimeline;

    [Header("DadakMerak Patrol")]
    [Tooltip("Timeline for idle/patrol behavior (looping animation)")]
    [SerializeField] private PlayableDirector patrolTimeline;

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
    private List<Vector3> dynamicWaypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private Vector3 lastSpawnedWaypointPosition = Vector3.zero;

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
        // Play patrol timeline on entry (set once)
        if (patrolTimeline != null && patrolTimeline.state != PlayState.Playing)
        {
            patrolTimeline.Play();
        }
    }

    private void HandleAlerted()
    {
        switch (bossType)
        {
            case BossType.Hanoman:
            case BossType.Leak:
                // These bosses don't actively chase - they wait or investigate
                break;
            case BossType.DadakMerak:
                ChaseBehavior();
                break;
        }
    }

    private void ChaseBehavior()
    {
        if (navAgent == null || !navAgent.isOnNavMesh)
            return;

        if (cachedPlayer == null)
            return;

        // Set chase movement speed
        if (navAgent.speed != chaseSpeed)
        {
            navAgent.speed = chaseSpeed;
        }

        // Set chase rotation speed
        if (navAgent.angularSpeed != chaseRotationSpeed)
        {
            navAgent.angularSpeed = chaseRotationSpeed;
        }

        // **PATHFINDING LOGIC:**
        // If direct line of sight to player → chase directly (faster, no waypoints needed)
        // If blocked by obstacle → use dynamic waypoints to navigate around it
        
        if (isPlayerVisible)
        {
            // Clear path to player - chase directly
            navAgent.SetDestination(cachedPlayer.transform.position);
            
            // Clear waypoints if they exist (obstacle must have been removed)
            if (dynamicWaypoints.Count > 0)
            {
                dynamicWaypoints.Clear();
                currentWaypointIndex = 0;
                lastSpawnedWaypointPosition = Vector3.zero;
            }
        }
        else
        {
            // Blocked by obstacle - use waypoint trail
            // Spawn dynamic waypoints as player moves (creates breadcrumb trail)
            if (cachedPlayer != null)
            {
                Vector3 playerPos = cachedPlayer.transform.position;
                float distanceFromLastWaypoint = Vector3.Distance(playerPos, lastSpawnedWaypointPosition);

                // Spawn new waypoint if player moved far enough
                if (distanceFromLastWaypoint >= waypointSpawnDistance)
                {
                    SpawnDynamicWaypoint(playerPos);
                }
            }

            // Navigate through dynamic waypoints
            if (dynamicWaypoints.Count > 0)
            {
                // Remove waypoints we've already passed
                if (currentWaypointIndex < dynamicWaypoints.Count)
                {
                    Vector3 currentWaypoint = dynamicWaypoints[currentWaypointIndex];
                    float distToWaypoint = Vector3.Distance(transform.position, currentWaypoint);

                    // Check if reached current waypoint
                    if (distToWaypoint <= waypointReachDistance)
                    {
                        currentWaypointIndex++;
                    }
                    else
                    {
                        // Set destination to current waypoint
                        navAgent.SetDestination(currentWaypoint);
                    }
                }
                else if (cachedPlayer != null)
                {
                    // All waypoints cleared, try direct chase
                    navAgent.SetDestination(cachedPlayer.transform.position);
                }
            }
            else if (lastKnownPlayerPosition != Vector3.zero)
            {
                // Chase last known position if player not visible and no waypoints yet
                navAgent.SetDestination(lastKnownPlayerPosition);
            }
        }
    }

    private void SpawnDynamicWaypoint(Vector3 playerPosition)
    {
        dynamicWaypoints.Add(playerPosition);
        lastSpawnedWaypointPosition = playerPosition;
        Debug.Log($"{gameObject.name}: Dynamic waypoint spawned at {playerPosition}. Total waypoints: {dynamicWaypoints.Count}");
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

        // Exit current state (cleanup)
        switch (currentState)
        {
            case EnemyState.Idle:
                // Stop patrol timeline when leaving idle
                if (patrolTimeline != null && patrolTimeline.state == PlayState.Playing)
                {
                    patrolTimeline.Stop();
                }
                break;

            case EnemyState.Alerted:
                // Clean up dynamic waypoints when exiting alert state
                dynamicWaypoints.Clear();
                currentWaypointIndex = 0;
                lastSpawnedWaypointPosition = Vector3.zero;
                Debug.Log($"{gameObject.name}: Cleared dynamic waypoints.");
                break;
        }

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



        // Primary: Switch to UI mode to disable gameplay input while preserving UI access
        if (Sys_GameModeSwitch.Instance != null)
        {
            Sys_GameModeSwitch.Instance.SetMode(Sys_GameModeSwitch.GameMode.UI);
            Debug.Log($"{gameObject.name}: Switched to UI mode - player input locked, UI controls available");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: GameModeSwitch instance not found!");
        }

        // Backup: Disable CharacterController
        CharacterController controller = cachedPlayer.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            Debug.Log($"{gameObject.name}: Disabled CharacterController on {cachedPlayer.name}");
        }
        
        // Backup: Disable Rigidbody physics
        Rigidbody rb = cachedPlayer.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            Debug.Log($"{gameObject.name}: Froze Rigidbody on {cachedPlayer.name}");
        }
        
        // Call Player_Components freeze method (if it exists)
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
                Debug.Log($"{gameObject.name} (Dadak Merak): Player caught! Starting death sequence Timeline...");
                if (deathTimeline != null)
                {
                    deathTimeline.Play();
                    Debug.Log($"{gameObject.name}: Death Timeline started. Timeline will control parenting and death signal.");
                }
                else
                {
                    Debug.LogWarning($"{gameObject.name}: Death Timeline not assigned!");
                }
                break;
                
            case BossType.Hanoman:
            case BossType.Leak:
                Debug.Log($"{gameObject.name} (Hanoman): Player caught! You are ded!");
                CatchPlayer();
                break;
        }
    }

    /// <summary>
    /// Called by Timeline when it's time to parent the player to the grab slot.
    /// Place this as a signal or call it from an animation event in your Timeline.
    /// </summary>
    public void OnTimelineParentPlayer()
    {
        Debug.Log($"{gameObject.name}: Timeline signal - parenting player to grab slot");
        CatchPlayer();
    }

    /// <summary>
    /// Called by Timeline Signal when death animation reaches the point where player should be marked dead.
    /// Hook this method to a Signal Track in your Timeline editor.
    /// </summary>
    public void OnDeathSignalFired()
    {
        Debug.Log($"{gameObject.name}: Death signal fired from Timeline!");
        if (caughtPlayerComponent != null)
        {
            caughtPlayerComponent.HandleDead();
            Debug.Log($"{gameObject.name}: Player marked as dead via Timeline signal");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Caught player component not found!");
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

    #region Gizmo Visualization
    private void OnDrawGizmos()
    {
        // Visualize dynamic waypoints
        if (dynamicWaypoints != null && dynamicWaypoints.Count > 0)
        {
            Gizmos.color = Color.cyan;
            
            for (int i = 0; i < dynamicWaypoints.Count; i++)
            {
                Vector3 waypoint = dynamicWaypoints[i];
                
                // Draw waypoint sphere
                Gizmos.DrawWireSphere(waypoint, 0.3f);
                
                // Draw line to next waypoint
                if (i < dynamicWaypoints.Count - 1)
                {
                    Gizmos.DrawLine(waypoint, dynamicWaypoints[i + 1]);
                }
                
                // Highlight current waypoint being pursued
                if (i == currentWaypointIndex)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(waypoint, 0.5f);
                    Gizmos.color = Color.cyan;
                }
            }
        }

        // Visualize last known player position
        if (lastKnownPlayerPosition != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.2f);
        }
    }
    #endregion
}