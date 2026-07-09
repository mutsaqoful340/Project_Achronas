using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Playables;

public class _Enemy_Boss : MonoBehaviour
{
    /// <summary>
    /// Patrol waypoint with configurable wait duration
    /// </summary>
    [System.Serializable]
    public struct PatrolWaypoint
    {
        public Transform position;
        [Tooltip("Time to wait at this waypoint (0 = proceed immediately)")]
        public float waitDuration;
        [Tooltip("Move speed when transitioning to this waypoint (<= 0 uses default speed)")]
        public float moveSpeed;
        [Tooltip("If true, invoke OnArrivedAtPatrolWaypoint event when reaching this waypoint")]
        public bool invokeArrivalEvent;
    }

    public enum BossType {DadakMerak, Leak, Hanoman}

    enum EnemyState {Idle, Alerted, SearchMode, CaughtPlayer}

    [Header("References")]
    public Animator animator;

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
    
    // Safety: Track which player actually entered the trigger
    private GameObject triggerPlayer = null;

    [Header("Mannequinn Interaction")]
    public _Enemy_Mannequin[] enemyMannequin;

    [Header("DadakMerak Chase Settings")]
    [Tooltip("Speed at which DadakMerak moves when chasing the player")]
    [SerializeField] private float chaseSpeed = 10f;
    [Tooltip("Rotation speed when chasing player (degrees per second)")]
    [SerializeField] private float chaseRotationSpeed = 360f;
    
    [Header("DadakMerak Dynamic Waypoint Settings")]
    [Tooltip("Distance player must move to spawn a new waypoint")]
    [SerializeField] private float waypointSpawnDistance = 2f;
    [Tooltip("Radius around waypoint to consider it 'reached'")]
    [SerializeField] private float waypointDynamicReachDistance = 0.5f;
    
    [Header("DadakMerak Search State Settings")]
    [Tooltip("Duration to follow first spotted player's waypoints after they exit LOS (seconds)")]
    [SerializeField] private float searchStateDuration = 10f;

    [Header("DadakMerak Death Sequence")]
    [Tooltip("Timeline to play when player is caught (for death animation/cutscene)")]
    [SerializeField] private PlayableDirector deathTimeline;
    
    [Header("DadakMerak Patrol Timeline")]
    [Tooltip("Timeline for idle/patrol behavior (looping animation, used when not using waypoint patrol)")]
    [SerializeField] private PlayableDirector patrolTimeline;

    [Header("Spotting Pause")]
    [Tooltip("Pause the spotting system if true (used for cutscenes or special events)")]
    [SerializeField] private bool pauseSpotting = false;

    [Header("Patrol Settings")]
    [Tooltip("Is this enemy using patrol waypoints? (If true, will follow patrol waypoints)")]
    [SerializeField] private bool isPatrolWaypoint;
    [Tooltip("If true, enemy will loop through patrol waypoints indefinitely; if false, will stop at last waypoint")]
    [SerializeField] private bool loopPatrolWaypoints;
    [Tooltip("Array of patrol waypoints with individual wait durations")]
    [SerializeField] private PatrolWaypoint[] patrolWaypoints;
    [Tooltip("How close to get to waypoint before moving to next")]
    [SerializeField] private float waypointReachDistance = 0.5f;
    [Tooltip("How fast to rotate towards next waypoint")]
    [SerializeField] private float waypointRotationSpeed = 5f;

    [Header("Spotting Events")]
    public UnityEvent OnSpottingPlayer;

    [Header("Patrol Events")]
    [Tooltip("Invoked when the boss reaches the last patrol waypoint")]
    public UnityEvent OnReachedLastPatrolWaypoint;
    [Tooltip("Invoked on each waypoint arrival immediately before waiting (if wait duration > 0)")]
    public UnityEvent OnArrivedAtPatrolWaypoint;
    [Tooltip("Invoked after the wait duration at a patrol waypoint has finished")]
    public UnityEvent OnAfterDurationFinished;

    // State management
    private EnemyState currentState = EnemyState.Idle;
    private bool playerCaught = false; // Hard lock: prevents all detection/movement after player is caught
    
    // Navigation
    private NavMeshAgent navAgent;
    private Vector3 initialPosition; // Store starting position for reset
    private Quaternion initialRotation; // Store starting rotation for reset

    [Header("DO NOT MANUALLY ASSIGN!!!")]
    // Detection variables
    [SerializeField] private List<GameObject> detectedPlayers = new List<GameObject>(); // Temporarily holds all currently detected players this frame
    [SerializeField] private List<GameObject> spottedPlayers = new List<GameObject>(); // Priority queue: [0] = first spotted, [1] = second spotted, etc.
    [SerializeField] private List<Vector3> dynamicWaypoints = new List<Vector3>();
    private GameObject cachedPlayer; // Legacy field, kept for compatibility
    private float nextDetectionTime;
    private bool isPlayerVisible = false;
    private Vector3 lastKnownPlayerPosition;
    
    // Search state variables
    private float searchStateTimer = 0f;
    private GameObject searchTargetPlayer = null; // The player being followed during search state
    
    // Chase variables
    private float defaultAngularSpeed;
    private float defaultMoveSpeed;
    private int currentWaypointIndex = 0;
    private Vector3 lastSpawnedWaypointPosition = Vector3.zero;
    private bool isAggravated = false;  // Lock onto player after OnAggravated() - always maintains LOS
    
    // Patrol variables
    private int currentPatrolWaypointIndex = 0;
    private float waypointWaitTimer = 0f;
    private bool isWaitingAtWaypoint = false;
    private bool hasInvokedLastWaypointEvent = false;
    private bool patrolInitialized = false;  // Track if patrol has been initialized to avoid resetting index unnecessarily

    // Startup config snapshots for reliable respawn reset.
    private bool initialPauseSpotting;
    private bool initialIsPatrolWaypoint;
    private bool initialLoopPatrolWaypoints;

    private void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        
        if (navAgent == null)
        {
            Debug.LogError($"{gameObject.name}: NavMeshAgent component is missing!");
            return;
        }
        
        // Store initial position and rotation for reset
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        // Store default angular speed for restoration
        defaultAngularSpeed = navAgent.angularSpeed;
        defaultMoveSpeed = navAgent.speed;

        // Snapshot startup toggles so reset restores designer-configured defaults.
        initialPauseSpotting = pauseSpotting;
        initialIsPatrolWaypoint = isPatrolWaypoint;
        initialLoopPatrolWaypoints = loopPatrolWaypoints;
        
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
        // Hard lock: once a player is caught, stop everything
        if (playerCaught)
            return;

        // Skip detection and awareness updates while paused or while catching player
        if (!pauseSpotting && currentState != EnemyState.CaughtPlayer)
        {
            // OPTIMIZATION: Check for player at intervals (more frequently during chase)
            float currentInterval = currentState == EnemyState.Alerted || currentState == EnemyState.SearchMode ? 0.05f : detectionInterval;
            
            if (Time.time >= nextDetectionTime)
            {
                nextDetectionTime = Time.time + currentInterval;
                isPlayerVisible = IsPlayerInLOS();
            }

            // If aggravated, always maintain LOS on spotted player
            if (isAggravated && spottedPlayers.Count > 0)
            {
                isPlayerVisible = true;
            }

            // Update awareness based on player visibility
            UpdateAwareness();
        }

        // Update search state timer if in search mode
        if (currentState == EnemyState.SearchMode)
        {
            searchStateTimer -= Time.deltaTime;
            if (searchStateTimer <= 0f)
            {
                // Search state expired, transition back to Alerted or Idle
                if (spottedPlayers.Count > 0)
                {
                    TransitionToState(EnemyState.Alerted);
                }
                else
                {
                    TransitionToState(EnemyState.Idle);
                }
            }
        }

        // Execute current state behavior
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle();
                break;
            case EnemyState.Alerted:
                HandleAlerted();
                break;
            case EnemyState.SearchMode:
                HandleSearchMode();
                break;
            case EnemyState.CaughtPlayer:
                HandleCatchingPlayer();
                break;
        }
    }

    #region State Handlers
    private void HandleIdle()
    {
        // If using waypoint patrol, handle patrol behavior
        if (isPatrolWaypoint && patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            HandlePatrol();
            return;
        }
        
        // Otherwise, use timeline-based patrol/idle animation
        if (patrolTimeline != null && patrolTimeline.state != PlayState.Playing)
        {
            patrolTimeline.Play();
        }
    }

    private void HandlePatrol()
    {
        // Disable patrol when aggravated - always chase instead
        if (isAggravated)
            return;

        if (patrolWaypoints.Length == 0 || navAgent == null)
            return;

        // Ensure patrol can actually move (speed may be zeroed by other states)
        if (navAgent.speed <= 0.01f)
        {
            PatrolWaypoint speedSource = patrolWaypoints[Mathf.Clamp(currentPatrolWaypointIndex, 0, patrolWaypoints.Length - 1)];
            float fallbackSpeed = speedSource.moveSpeed > 0.01f
                ? speedSource.moveSpeed
                : (defaultMoveSpeed > 0.01f ? defaultMoveSpeed : chaseSpeed);
            navAgent.speed = fallbackSpeed;
        }

        // Check if waiting at waypoint
        if (isWaitingAtWaypoint)
        {
            // Face the waypoint's forward direction (Z+) while waiting
            PatrolWaypoint currentWaypoint = patrolWaypoints[currentPatrolWaypointIndex];
            if (currentWaypoint.position != null)
            {
                Vector3 waypointForward = currentWaypoint.position.forward;
                if (waypointForward.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(waypointForward);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, waypointRotationSpeed * Time.deltaTime);
                }
            }
            
            waypointWaitTimer -= Time.deltaTime;
            if (waypointWaitTimer <= 0f)
            {
                isWaitingAtWaypoint = false;
                OnAfterDurationFinished?.Invoke();
                MoveToNextPatrolWaypoint();
            }
            return;
        }

        // If current destination is invalid/partial, skip to next patrol point
        if (!navAgent.pathPending &&
            (navAgent.pathStatus == NavMeshPathStatus.PathInvalid || navAgent.pathStatus == NavMeshPathStatus.PathPartial))
        {
            MoveToNextPatrolWaypoint();
            return;
        }

        // Check if reached current waypoint
        if (!navAgent.pathPending && navAgent.remainingDistance <= waypointReachDistance)
        {
            // Invoke arrival event if waypoint allows it
            PatrolWaypoint currentWaypoint = patrolWaypoints[currentPatrolWaypointIndex];
            if (currentWaypoint.invokeArrivalEvent)
            {
                OnArrivedAtPatrolWaypoint?.Invoke();
            }
            
            int lastIndex = patrolWaypoints.Length - 1;
            bool isAtLastWaypoint = currentPatrolWaypointIndex == lastIndex;
            if (isAtLastWaypoint && (loopPatrolWaypoints || !hasInvokedLastWaypointEvent))
            {
                OnReachedLastPatrolWaypoint?.Invoke();
                hasInvokedLastWaypointEvent = true;
            }

            // Check if this waypoint has a wait duration
            if (currentWaypoint.waitDuration > 0f)
            {
                // Start waiting at waypoint
                isWaitingAtWaypoint = true;
                waypointWaitTimer = currentWaypoint.waitDuration;
            }
            else
            {
                // No wait duration, proceed immediately
                MoveToNextPatrolWaypoint();
            }
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

    private void HandleSearchMode()
    {
        // During search state, boss follows the first spotted player's dynamic waypoints
        switch (bossType)
        {
            case BossType.DadakMerak:
                ChaseBehavior();
                break;
        }
    }

    private void ChaseBehavior()
    {
        // Guard: only chase if actively alerted or in search mode (not caught or idle)
        if (currentState != EnemyState.Alerted && currentState != EnemyState.SearchMode)
            return;
            
        if (navAgent == null || !navAgent.isOnNavMesh)
            return;

        // During search mode, use searchTargetPlayer; during alert, use primary spotted player
        GameObject targetPlayer = currentState == EnemyState.SearchMode ? searchTargetPlayer : (spottedPlayers.Count > 0 ? spottedPlayers[0] : null);
        
        if (targetPlayer == null)
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
        // Special case: During search mode, continue following waypoints of the last spotted player
        
        if (isPlayerVisible && targetPlayer != null && spottedPlayers.Contains(targetPlayer))
        {
            // Clear path to player - chase directly
            navAgent.SetDestination(targetPlayer.transform.position);
            
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
            if (targetPlayer != null)
            {
                Vector3 playerPos = targetPlayer.transform.position;
                float distanceFromLastWaypoint = Vector3.Distance(playerPos, lastSpawnedWaypointPosition);

                // Spawn new waypoint if player moved far enough (only in Alerted state, not search)
                if (currentState == EnemyState.Alerted && distanceFromLastWaypoint >= waypointSpawnDistance)
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
                else if (targetPlayer != null)
                {
                    // All waypoints cleared, try direct chase
                    navAgent.SetDestination(targetPlayer.transform.position);
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
        // Only spawn waypoints during active chase (Alerted state)
        if (currentState != EnemyState.Alerted)
            return;
            
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
                // Stop patrol waypoint navigation
                if (isPatrolWaypoint && navAgent != null && navAgent.isOnNavMesh)
                {
                    navAgent.ResetPath();
                }
                break;

            case EnemyState.Alerted:
                // Don't clear waypoints when entering search mode - they're needed for pursuit
                if (newState != EnemyState.SearchMode)
                {
                    dynamicWaypoints.Clear();
                    currentWaypointIndex = 0;
                    lastSpawnedWaypointPosition = Vector3.zero;
                    Debug.Log($"{gameObject.name}: Cleared dynamic waypoints.");
                }
                break;
                
            case EnemyState.SearchMode:
                // Clean up when exiting search mode
                dynamicWaypoints.Clear();
                currentWaypointIndex = 0;
                lastSpawnedWaypointPosition = Vector3.zero;
                searchStateTimer = 0f;
                searchTargetPlayer = null;
                Debug.Log($"{gameObject.name}: Exited search mode. Cleared waypoints and timer.");
                break;
        }

        currentState = newState;

        // Enter new state - ONE-TIME EVENTS GO HERE
        switch (newState)
        {
            case EnemyState.Idle:
                // Idle entry behavior
                spottedPlayers.Clear();
                searchTargetPlayer = null;
                
                // Initialize patrol if enabled (only reset index on first initialization, not when resuming from chase)
                if (isPatrolWaypoint && patrolWaypoints != null && patrolWaypoints.Length > 0)
                {
                    // Only reset index if this is the first time entering patrol
                    if (!patrolInitialized)
                    {
                        currentPatrolWaypointIndex = 0;
                        patrolInitialized = true;
                        Debug.Log($"{gameObject.name}: Starting patrol with {patrolWaypoints.Length} waypoints");
                    }
                    else
                    {
                        // Resuming from chase - continue from nearest waypoint instead of restarting
                        Debug.Log($"{gameObject.name}: Resuming patrol from waypoint {currentPatrolWaypointIndex} (was chasing player)");
                    }
                    isWaitingAtWaypoint = false;
                    waypointWaitTimer = 0f;
                    hasInvokedLastWaypointEvent = false;
                    MoveToCurrentPatrolWaypoint();
                }
                break;

            case EnemyState.Alerted:
                OnSpottingPlayer?.Invoke(); // Invoked once when player is spotted
                OnPlayerSpotted(); // Call spotted method
                searchStateTimer = 0f;
                searchTargetPlayer = null;
                
                // Spawn initial waypoint at the player's current position when chase starts
                if (spottedPlayers.Count > 0)
                {
                    Vector3 playerStartPos = spottedPlayers[0].transform.position;
                    dynamicWaypoints.Add(playerStartPos);
                    lastSpawnedWaypointPosition = playerStartPos;
                    Debug.Log($"{gameObject.name}: Initial waypoint spawned at chase start position: {playerStartPos}");
                }
                break;
                
            case EnemyState.SearchMode:
                // Start search state - save which player we're following
                if (spottedPlayers.Count > 0)
                {
                    searchTargetPlayer = spottedPlayers[0];
                    searchStateTimer = searchStateDuration;
                    Debug.Log($"{gameObject.name}: Entered search mode for {searchStateDuration}s, following {searchTargetPlayer.name}'s waypoints");
                }
                break;

            case EnemyState.CaughtPlayer:
                // Clear awareness of other players - focus only on the caught one
                currentAwareness = 0f;
                detectedPlayers.Clear();
                spottedPlayers.Clear();
                cachedPlayer = null; // Clear detection cache
                searchTargetPlayer = null;
                // NOTE: Do NOT clear triggerPlayer here - it's needed by OnPlayerCaught() -> CatchPlayer()
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
                    cachedPlayer = player; // Cache for legacy compatibility
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
        
        // Update spotted players queue: maintain players in LOS, remove those who exited
        UpdateSpottedPlayersQueue();
        
        // Return true if ANY player was detected
        return detectedPlayers.Count > 0;
    }

    private void UpdateSpottedPlayersQueue()
    {
        // Remove players from spotted list if they're no longer in detectedPlayers
        for (int i = spottedPlayers.Count - 1; i >= 0; i--)
        {
            GameObject spottedPlayer = spottedPlayers[i];
            if (!detectedPlayers.Contains(spottedPlayer))
            {
                // Player exited LOS - remove from queue
                spottedPlayers.RemoveAt(i);
                Debug.Log($"{gameObject.name}: {spottedPlayer.name} exited LOS. Removed from queue. Queue size: {spottedPlayers.Count}");
                
                // If we were in Alerted state and just lost the primary target, enter search mode
                if (currentState == EnemyState.Alerted && i == 0 && spottedPlayers.Count > 0)
                {
                    // Primary target exited, but secondary target still in LOS
                    // Enter search mode to follow primary target's waypoints
                    TransitionToState(EnemyState.SearchMode);
                    Debug.Log($"{gameObject.name}: Primary target lost LOS. Entering search mode.");
                }
                else if (currentState == EnemyState.Alerted && i == 0 && spottedPlayers.Count == 0)
                {
                    // All players lost LOS
                    TransitionToState(EnemyState.Idle);
                    Debug.Log($"{gameObject.name}: All targets lost LOS. Returning to Idle.");
                }
            }
        }
        
        // Add newly detected players to the queue
        foreach (GameObject detectedPlayer in detectedPlayers)
        {
            if (!spottedPlayers.Contains(detectedPlayer))
            {
                // New player spotted - add to queue
                spottedPlayers.Add(detectedPlayer);
                Debug.Log($"{gameObject.name}: {detectedPlayer.name} spotted and added to queue. Queue size: {spottedPlayers.Count}");
            }
        }
    }

    private void UpdateAwareness()
    {
        // Only update awareness in Idle and Alerted states (not during search mode)
        if (currentState == EnemyState.SearchMode)
            return;
            
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
        // Use the player that actually triggered (not cached detection)
        GameObject playerToCatch = triggerPlayer ?? cachedPlayer;
        
        if (playerToCatch != null)
        {
            // Clear chase waypoints - stop following traces
            dynamicWaypoints.Clear();
            currentWaypointIndex = 0;
            lastSpawnedWaypointPosition = Vector3.zero;
            
            // Stop NavMesh movement completely
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.ResetPath();
                navAgent.SetDestination(transform.position); // Set destination to current position (prevent any movement)
                navAgent.speed = 0f;
            }
            
            // Attempt to get Player_Components from the caught player
            caughtPlayerComponent = playerToCatch.GetComponent<Player_Components>();
            
            if (caughtPlayerComponent != null)
            {
                // Parent player to the caught player slot
                if (caughtPlayerSlot != null)
                {
                    playerToCatch.transform.SetParent(caughtPlayerSlot);
                    playerToCatch.transform.localPosition = Vector3.zero;
                    playerToCatch.transform.localRotation = Quaternion.identity;
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
                Debug.LogError($"{gameObject.name}: Caught player {playerToCatch.name} does not have Player_Components!");
            }
        }
        else
        {
            Debug.LogError($"{gameObject.name}: No player to catch!");
        }
        
        // Clear trigger reference only after using it
        triggerPlayer = null;
    }

    private void FreezePlayer()
    {
        if (caughtPlayerComponent == null)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot freeze player - caughtPlayerComponent is null!");
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
        CharacterController controller = caughtPlayerComponent.gameObject.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            Debug.Log($"{gameObject.name}: Disabled CharacterController on {caughtPlayerComponent.gameObject.name}");
        }
        
        // Backup: Disable Rigidbody physics
        Rigidbody rb = caughtPlayerComponent.gameObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            Debug.Log($"{gameObject.name}: Froze Rigidbody on {caughtPlayerComponent.gameObject.name}");
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
            GameObject enteredPlayer = other.gameObject;
            
            // Determine which player should be the valid catch target based on current state
            GameObject validTarget = currentState == EnemyState.SearchMode ? searchTargetPlayer : (spottedPlayers.Count > 0 ? spottedPlayers[0] : null);
            
            // Only catch if the entered player is the one we're actually chasing
            if (enteredPlayer == validTarget)
            {
                Debug.Log($"{gameObject.name}: Target player {enteredPlayer.name} entered trigger zone. Catching!");
                triggerPlayer = enteredPlayer;
                TransitionToState(EnemyState.CaughtPlayer);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: Non-target player {enteredPlayer.name} entered trigger zone, but current target is {validTarget?.name ?? "none"}. Ignoring catch.");
            }
        }
    }

    public void OnAnimatorTrigger(string triggerName)
    {
        animator.SetTrigger(triggerName);
    }

    public void OnPatrolWaypoint()
    {
        SetPatrolWaypointMode(true);
    }

    public void OnSetPauseSpotting(bool paused)
    {
        pauseSpotting = paused;

        if (pauseSpotting)
        {
            // Freeze detection progression while paused.
            isPlayerVisible = false;
            nextDetectionTime = Time.time + detectionInterval;
        }
    }

    public void OnPauseSpotting()
    {
        OnSetPauseSpotting(true);
    }

    public void OnResumeSpotting()
    {
        OnSetPauseSpotting(false);
    }

    public void OnNoPatrolWaypoint()
    {
        SetPatrolWaypointMode(false);
    }

    public void OnSetLoopPatrolWaypoints(bool shouldLoop)
    {
        loopPatrolWaypoints = shouldLoop;
    }

    public void OnEnableLoopPatrolWaypoints()
    {
        OnSetLoopPatrolWaypoints(true);
    }

    public void OnDisableLoopPatrolWaypoints()
    {
        OnSetLoopPatrolWaypoints(false);
    }

    public void SetPatrolWaypointMode(bool enabled)
    {
        isPatrolWaypoint = enabled;

        if (!enabled)
        {
            isWaitingAtWaypoint = false;
            waypointWaitTimer = 0f;
            currentPatrolWaypointIndex = 0;
            hasInvokedLastWaypointEvent = false;

            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.ResetPath();
            }
        }
        else if (currentState == EnemyState.Idle && patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            currentPatrolWaypointIndex = 0;
            isWaitingAtWaypoint = false;
            waypointWaitTimer = 0f;
            hasInvokedLastWaypointEvent = false;
            MoveToCurrentPatrolWaypoint();
        }
    }

    public void OnPlayerCaught()
    {
        playerCaught = true; // Hard lock - stop all detection and movement immediately
        Debug.Log($"{gameObject.name}: OnPlayerCaught() called! BossType is: {bossType}");
        
        if (patrolTimeline != null && patrolTimeline.state == PlayState.Playing)
        {
            patrolTimeline.Stop();
        }
        
        // Stop NavMesh movement completely regardless of boss type
        dynamicWaypoints.Clear();
        currentWaypointIndex = 0;
        lastSpawnedWaypointPosition = Vector3.zero;
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.ResetPath();
            navAgent.SetDestination(transform.position);
            navAgent.speed = 0f;
        }
        
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
                Debug.Log($"{gameObject.name} ({bossType}): Player caught! You are ded!");
                CatchPlayer();
                break;
            
            default:
                Debug.LogWarning($"{gameObject.name}: Unknown BossType {bossType}! Applying Leak behavior as fallback.");
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

    public void OnAggravated()
    {
        // Find all players in scene
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");

        if (allPlayers.Length == 0)
        {
            Debug.LogWarning($"{gameObject.name}: OnAggravated() called but no player found in scene!");
            return;
        }

        GameObject target = allPlayers[0];

        // Disable waypoint patrol - boss will ONLY chase after aggravation
        isPatrolWaypoint = false;

        // Force immediate target lock for chase systems.
        cachedPlayer = target;
        lastKnownPlayerPosition = target.transform.position;
        isPlayerVisible = true;
        currentAwareness = awarenessThreshold;
        isAggravated = true;  // Lock onto this player - always maintain LOS

        if (!spottedPlayers.Contains(target))
        {
            spottedPlayers.Insert(0, target);
        }

        if (pauseSpotting)
        {
            pauseSpotting = false;
        }

        // Immediately boost chase speed
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.speed = chaseSpeed;
            navAgent.angularSpeed = chaseRotationSpeed;
        }

        // Enter Alerted immediately (DadakMerak starts chasing in HandleAlerted).
        if (currentState != EnemyState.CaughtPlayer)
        {
            TransitionToState(EnemyState.Alerted);
        }

        Debug.Log($"{gameObject.name} ({bossType}): Aggravated! Locked onto {target.name}. Chase speed set to {chaseSpeed}. LOS always maintained. Waypoint patrol disabled.");
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

        // Visualize patrol waypoints
        if (isPatrolWaypoint && patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolWaypoints.Length; i++)
            {
                if (patrolWaypoints[i].position != null)
                {
                    Vector3 waypointPos = patrolWaypoints[i].position.position;
                    
                    // Draw waypoint sphere
                    Gizmos.DrawWireSphere(waypointPos, 0.4f);
                    
                    // Draw forward direction (Z+)
                    Vector3 waypointForward = patrolWaypoints[i].position.forward;
                    Gizmos.DrawLine(waypointPos, waypointPos + waypointForward * 0.5f);
                    
                    // Draw line to next waypoint
                    if (i < patrolWaypoints.Length - 1 && patrolWaypoints[i + 1].position != null)
                    {
                        Gizmos.DrawLine(waypointPos, patrolWaypoints[i + 1].position.position);
                    }
                    else if (i == patrolWaypoints.Length - 1 && patrolWaypoints[0].position != null)
                    {
                        // Loop line back to start
                        Gizmos.DrawLine(waypointPos, patrolWaypoints[0].position.position);
                    }
                    
                    // Highlight current patrol waypoint
                    if (i == currentPatrolWaypointIndex)
                    {
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawWireSphere(waypointPos, 0.6f);
                        Gizmos.color = Color.green;
                    }
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

    #region Patrol Methods
    private void MoveToCurrentPatrolWaypoint()
    {
        if (patrolWaypoints.Length == 0 || currentPatrolWaypointIndex >= patrolWaypoints.Length)
            return;

        PatrolWaypoint targetWaypoint = patrolWaypoints[currentPatrolWaypointIndex];
        if (targetWaypoint.position != null)
        {
            float patrolMoveSpeed = targetWaypoint.moveSpeed > 0.01f
                ? targetWaypoint.moveSpeed
                : (defaultMoveSpeed > 0.01f ? defaultMoveSpeed : chaseSpeed);

            if (navAgent.speed != patrolMoveSpeed)
            {
                navAgent.speed = patrolMoveSpeed;
            }

            // Snap target to nearest NavMesh position to avoid jitter on unreachable points
            if (NavMesh.SamplePosition(targetWaypoint.position.position, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            {
                navAgent.SetDestination(hit.position);
            }
            else
            {
                // Waypoint is off NavMesh, skip to next
                MoveToNextPatrolWaypoint();
            }
        }
        else
        {
            // Null waypoint entry, skip to next
            MoveToNextPatrolWaypoint();
        }
    }

    private void MoveToNextPatrolWaypoint()
    {
        if (patrolWaypoints == null || patrolWaypoints.Length == 0)
            return;

        int lastIndex = patrolWaypoints.Length - 1;

        // If looping is disabled, stop at the last waypoint.
        if (!loopPatrolWaypoints && currentPatrolWaypointIndex >= lastIndex)
        {
            isWaitingAtWaypoint = false;
            waypointWaitTimer = 0f;

            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.ResetPath();
                navAgent.SetDestination(transform.position);
            }
            return;
        }

        // Move to next waypoint (loop or clamp based on setting).
        if (loopPatrolWaypoints)
        {
            currentPatrolWaypointIndex = (currentPatrolWaypointIndex + 1) % patrolWaypoints.Length;
        }
        else
        {
            currentPatrolWaypointIndex = Mathf.Min(currentPatrolWaypointIndex + 1, lastIndex);
        }

        MoveToCurrentPatrolWaypoint();
    }

    private void RotateTowardsNextPatrolWaypoint()
    {
        if (patrolWaypoints.Length == 0)
            return;

        // Get next waypoint index
        int nextIndex = (currentPatrolWaypointIndex + 1) % patrolWaypoints.Length;
        PatrolWaypoint nextWaypoint = patrolWaypoints[nextIndex];
        if (nextWaypoint.position == null)
            return;

        // Calculate direction to next waypoint
        Vector3 directionToNext = (nextWaypoint.position.position - transform.position).normalized;
        
        // Ignore vertical rotation (keep enemy upright)
        directionToNext.y = 0;
        
        if (directionToNext.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToNext);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, waypointRotationSpeed * Time.deltaTime);
        }
    }

    private int FindNearestPatrolWaypoint()
    {
        if (patrolWaypoints.Length == 0)
            return 0;

        int nearestIndex = 0;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < patrolWaypoints.Length; i++)
        {
            if (patrolWaypoints[i].position == null)
                continue;

            float distance = Vector3.Distance(transform.position, patrolWaypoints[i].position.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }
        return nearestIndex;
    }
    #endregion

    #region Reset System
    /// <summary>
    /// Reset boss to initial/idle state (called on respawn)
    /// </summary>
    public void ResetToInitialState()
    {
        // Clear all detection and awareness
        playerCaught = false; // Release hard lock so boss can detect again after reset
        pauseSpotting = initialPauseSpotting;
        isPatrolWaypoint = initialIsPatrolWaypoint;
        loopPatrolWaypoints = initialLoopPatrolWaypoints;
        currentAwareness = 0f;
        detectedPlayers.Clear();
        spottedPlayers.Clear();
        cachedPlayer = null;
        triggerPlayer = null;
        caughtPlayerComponent = null;
        isPlayerVisible = false;
        nextDetectionTime = 0f;
        lastKnownPlayerPosition = Vector3.zero;
        searchStateTimer = 0f;
        searchTargetPlayer = null;
        
        // Clear chase waypoints
        dynamicWaypoints.Clear();
        currentWaypointIndex = 0;
        lastSpawnedWaypointPosition = Vector3.zero;
        
        // Clear patrol state
        currentPatrolWaypointIndex = 0;
        isWaitingAtWaypoint = false;
        waypointWaitTimer = 0f;
        hasInvokedLastWaypointEvent = false;
        patrolInitialized = false;  // Reset so patrol reinitializes on respawn
        isAggravated = false;  // Reset aggravation lock

        // Stop current timelines
        if (deathTimeline != null && deathTimeline.state == PlayState.Playing)
        {
            deathTimeline.Stop();
        }
        if (patrolTimeline != null && patrolTimeline.state == PlayState.Playing)
        {
            patrolTimeline.Stop();
        }

        // Reset NavMesh agent and teleport to initial position
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            // Disable NavMeshAgent to allow teleportation
            navAgent.enabled = false;
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            navAgent.enabled = true;
            
            // Reset NavMeshAgent state
            navAgent.ResetPath();
            navAgent.speed = defaultMoveSpeed > 0.01f ? defaultMoveSpeed : navAgent.speed;
            navAgent.angularSpeed = defaultAngularSpeed;
            
            Debug.Log($"[RESET] {gameObject.name} teleported to initial position {initialPosition}");
        }
        else if (navAgent != null)
        {
            // NavMeshAgent exists but not on NavMesh - just set position directly
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            Debug.Log($"[RESET] {gameObject.name} reset position (NavMeshAgent off mesh)");
        }
        else
        {
            // No NavMeshAgent - just set position
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            Debug.Log($"[RESET] {gameObject.name} reset position (no NavMeshAgent)");
        }

        // Transition back to idle
        TransitionToState(EnemyState.Idle);

        Debug.Log($"[RESET] {gameObject.name} reset to initial state");
    }
    #endregion
}