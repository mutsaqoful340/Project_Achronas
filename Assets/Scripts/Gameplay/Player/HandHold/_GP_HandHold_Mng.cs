using System;
using Unity.VisualScripting;
using UnityEditor.EditorTools;
using UnityEngine.Animations.Rigging;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class _GP_HandHold_Mng : MonoBehaviour
{
    public enum RindaState
    {
        None,
        Holding,
        Reaching
    }

    public enum NayaState
    {
        None,
        Holding,
        Reaching
    }

    [Header("HandHold States [DEBUG]")]
    [Tooltip(@"State hand-hold untuk Rinda.
    DEBUG ONLY. DO NOT CHANGE MANUALLY!")]
    public RindaState currentRindaState = RindaState.None;
    [Tooltip(@"State hand-hold untuk Naya.
    DEBUG ONLY. DO NOT CHANGE MANUALLY!")]
    public NayaState currentNayaState = NayaState.None;

    [Header("Bool Flags [DEBUG]")]
    [Tooltip(@"Flag indikator apakah ada obstacle antara Rinda dan Naya.")]
    [SerializeField] private bool isObstacleBetweenPlayers = false;
    [Tooltip(@"Flag indikator apakah Rinda dan Naya berada dalam jarak deteksi hand-hold.")]
    [SerializeField] private bool isPlayersInRange = false;

    [Header("Player Object References")]
    [Tooltip("Referensi Rinda.")]
    public GameObject playerRinda;
    [Tooltip("Referensi komponen Player_Components Rinda.")]
    public Player_Components playerComponentsRinda;
    [Tooltip("Referensi Naya.")]
    public GameObject playerNaya;
    [Tooltip("Referensi komponen Player_Components Naya.")]
    public Player_Components playerComponentsNaya;

    [Header("HandHold Settings")]
    [Tooltip("Referensi Transform object yang akan menjadi target hand-hold pivot.")]
    public Transform handHoldPivotTarget;
    [Tooltip("Kecepatan mengikuti Target Pivot pada sumbu.")]
    public float handHoldFollowTargetSpeed = 10f;
    [Tooltip("Kecepatan mengikuti Target Pivot pada rotasi.")]
    public float handHoldFollowTargetRotationSpeed = 10f;
    [Tooltip("Referensi Transform object yang akan diikuti gameobject Rinda ketika hand-holding.")]
    public Transform handHoldPivotTransform;
    [Tooltip("Kecepatan mengikuti pivot pada sumbu X saat hand-holding.")]
    public float handHoldFollowSpeedX = 10f;
    [Tooltip("Kecepatan mengikuti pivot pada sumbu Z saat hand-holding.")]
    public float handHoldFollowSpeedZ = 10f;
    [Tooltip("Kecepatan mengikuti rotasi pivot saat hand-holding.")]
    public float handHoldRotationFollowSpeed = 10f;
    [Tooltip("Delay in seconds before Rinda jumps after Naya jumps.")]
    public float jumpDelaySeconds = 0f;

    [Header("IK Settings")]
    [Tooltip("TwoBoneIK constraint untuk tangan Rinda saat hand-holding.")]
    public TwoBoneIKConstraint rindaHandIKConstraint;
    [Tooltip("TwoBoneIK constraint untuk tangan Naya saat hand-holding.")]
    public TwoBoneIKConstraint nayaHandIKConstraint;
    [Tooltip("IK target Transform untuk tangan Rinda (ditempatkan di mana Rinda harus reach).")]
    public Transform rindaHandIKTarget;
    [Tooltip("IK target Transform untuk tangan Naya (ditempatkan di mana Naya harus reach).")]
    public Transform nayaHandIKTarget;
    [Tooltip("Offset posisi tangan Rinda relatif terhadap pivot (untuk IK).")]
    public Vector3 rindaHandIKOffset = new Vector3(-0.3f, 0.5f, 0f);
    [Tooltip("Offset rotasi tangan Rinda (Euler angles) relatif terhadap pivot.")]
    public Vector3 rindaHandIKRotationOffset = Vector3.zero;
    [Tooltip("Offset posisi tangan Naya relatif terhadap pivot (untuk IK).")]
    public Vector3 nayaHandIKOffset = new Vector3(0.3f, 0.5f, 0f);
    [Tooltip("Offset rotasi tangan Naya (Euler angles) relatif terhadap pivot.")]
    public Vector3 nayaHandIKRotationOffset = Vector3.zero;
    [Tooltip("Kecepatan blending weight IK saat hand-holding.")]
    public float handHoldIKBlendSpeed = 8f;
    [Tooltip("Weight IK saat baru reaching.")]
    [Range(0f, 1f)]
    public float handHoldReachIKWeight = 0.65f;
    [Tooltip("Weight IK penuh saat holding.")]
    [Range(0f, 1f)]
    public float handHoldFullIKWeight = 1f;

    [Header("Detection Settings")]
    [Tooltip("Jarak deteksi minimal untuk menentukan apakah pemain sedang berdekatan.")]
    public float handHoldDetectionRange = 2f;

    [Header("Head Turning References")]
    [Tooltip("Reference to Rinda's head turning script")]
    public _GP_HeadTurning rindaHeadTurning;
    [Tooltip("Reference to Naya's head turning script")]
    public _GP_HeadTurning nayaHeadTurning;

    #region Private Variables
    private float pendingRindaJumpTime = -999f;
    private float currentRindaHandIKWeight = 0f;
    private float currentNayaHandIKWeight = 0f;
    [HideInInspector] public bool isHandHoldActive = false;
    #endregion

    void Awake()
    {
        if (playerRinda == null || playerNaya == null)
        {
            Debug.LogError("Referensi player belum diatur di _GP_HandHold_Mng.");
        }

        if (handHoldPivotTransform == null)
        {
            Debug.LogError("Referensi handHoldPivotTransform belum diatur di _GP_HandHold_Mng.");
        }

        ValidateHandIKSetup();
    }

    void OnEnable()
    {
        // Subscribe to Naya input action
        if (playerNaya != null)
        {
            playerComponentsNaya.moduleInputPlay.OnAction += HandleNayaHandHoldAction;
            playerComponentsRinda.moduleInputPlay.OnAction += HandleRindaHandHoldAction;
        }
    }

    void OnDisable()
    {
        if (playerRinda != null)
        {
            playerComponentsNaya.moduleInputPlay.OnAction -= HandleNayaHandHoldAction;
            playerComponentsRinda.moduleInputPlay.OnAction -= HandleRindaHandHoldAction;
        }
    }

    void Update()
    {
        CheckPlayerDistance();
        CheckPlayerLOS();
        OnRindaState();
        OnNayaState();
        HandleHandHoldState();
        UpdatePendingJump();
        OnHandHold();
        OnPivotFollowTarget();
        UpdateHandHoldIK();
    }

    #region Controller Methods
    private void HandleRindaHandHoldAction(ActionState actionState)
    {
        if (actionState != ActionState.HandHold) return;

        if (currentRindaState == RindaState.None)
        {
            currentRindaState = RindaState.Reaching;
        }
        else if (currentRindaState == RindaState.Reaching)
        {
            currentRindaState = RindaState.None;
        }
        else if (currentRindaState == RindaState.Holding)
        {
            currentRindaState = RindaState.None;
            CharacterController rindaController = playerRinda.GetComponent<CharacterController>();
            if (rindaController != null)
            {
                rindaController.enabled = true; // Re-enable CharacterController when releasing hand-hold
            }
        }
        // Logika untuk menangani input hand-hold dari Rinda
        Debug.Log("Rinda melakukan aksi hand-hold.");
    }

    private void HandleNayaHandHoldAction(ActionState actionState)
    {
        // Handle hand-hold action
        if (actionState == ActionState.HandHold)
        {
            if (currentNayaState == NayaState.None)
            {
                currentNayaState = NayaState.Reaching;
            }
            else if (currentNayaState == NayaState.Reaching)
            {
                currentNayaState = NayaState.None;
            }
            else if (currentNayaState == NayaState.Holding)
            {
                currentNayaState = NayaState.None;
                CharacterController nayaController = playerNaya.GetComponent<CharacterController>();
                if (nayaController != null)
                {
                    nayaController.enabled = true; // Re-enable CharacterController when releasing hand-hold
                }
            }
            Debug.Log("Naya melakukan aksi hand-hold.");
        }
        
        // Handle jump action - queue Rinda's jump with delay
        if (actionState == ActionState.Jump && currentNayaState == NayaState.Holding && currentRindaState == RindaState.Holding)
        {
            // Queue Rinda's jump to happen after the delay
            pendingRindaJumpTime = Time.time + jumpDelaySeconds;
            Debug.Log("Naya jumped! Rinda will jump in " + jumpDelaySeconds + " seconds");
        }
    }
    #endregion

    #region Hand Holding Methods
    private void HandleHandHoldState()
    {
        if (isPlayersInRange && !isObstacleBetweenPlayers)
        {
            if (currentNayaState == NayaState.None && currentRindaState == RindaState.None)
            {
                Debug.Log("Players in range, no obstacles, but not reaching.");
                return;
            }

            if (currentNayaState == NayaState.Reaching && currentRindaState == RindaState.Reaching)
            {
                currentNayaState = NayaState.Holding;
                currentRindaState = RindaState.Holding;
                isHandHoldActive = true;
                Debug.Log("Players are now holding hands.");
                
                // Reset both players' head turning when they enter Holding state
                if (rindaHeadTurning != null)
                {
                    rindaHeadTurning.ResetHeadRotation();
                    Debug.Log("Rinda's head reset on hand-hold.");
                }
                if (nayaHeadTurning != null)
                {
                    nayaHeadTurning.ResetHeadRotation();
                    Debug.Log("Naya's head reset on hand-hold.");
                }
            }

            // if both are currently holding but one of them realesed the button, we should reset both to None
            if ((currentNayaState == NayaState.Holding && currentRindaState != RindaState.Holding) ||
                (currentRindaState == RindaState.Holding && currentNayaState != NayaState.Holding))
            {
                currentNayaState = NayaState.None;
                currentRindaState = RindaState.None;
                Debug.Log("One player released hand-hold, resetting both states to None.");
            }
        }

        isHandHoldActive = currentNayaState == NayaState.Holding && currentRindaState == RindaState.Holding;
    }

    private void OnHandHold()
    {
        if (currentNayaState == NayaState.Holding && currentRindaState == RindaState.Holding)
        {
            // Mengatur posisi Rinda mengikuti handHoldPivotTransform saat hand-holding
            if (playerRinda != null && handHoldPivotTransform != null)
            {
                CharacterController rindaController = playerRinda.GetComponent<CharacterController>();
                
                if (rindaController != null && rindaController.enabled)
                {
                    // Keep CharacterController enabled to maintain ground collision detection
                    
                    Vector3 currentPos = playerRinda.transform.position;
                    Vector3 targetPos = handHoldPivotTransform.position;
                    
                    // Calculate desired position using Lerp for smooth following
                    Vector3 desiredPos = new Vector3(
                        Mathf.Lerp(currentPos.x, targetPos.x, Time.deltaTime * handHoldFollowSpeedX),
                        currentPos.y,
                        Mathf.Lerp(currentPos.z, targetPos.z, Time.deltaTime * handHoldFollowSpeedZ)
                    );
                    
                    // Calculate movement velocity for this frame
                    Vector3 moveVelocity = (desiredPos - currentPos) / Time.deltaTime;
                    
                    // Use CharacterController.Move() to respect collisions with terrain
                    rindaController.Move(moveVelocity * Time.deltaTime);
                }
                
                // Handle rotation
                playerRinda.transform.rotation = Quaternion.Slerp(playerRinda.transform.rotation, handHoldPivotTransform.rotation, Time.deltaTime * handHoldRotationFollowSpeed);
            }
        }
        else
        {
            // Reset jump tracking when not hand-holding
            pendingRindaJumpTime = -999f;
        }
    }

    private void UpdateHandHoldIK()
    {
        if (handHoldPivotTransform == null)
            return;

        bool hasActiveHandHoldState = currentRindaState != RindaState.None || currentNayaState != NayaState.None;

        if (hasActiveHandHoldState)
        {
            UpdateIKTargets();
        }

        float rindaTargetWeight = GetTargetIKWeight(currentRindaState);
        float nayaTargetWeight = GetTargetIKWeight(currentNayaState);

        currentRindaHandIKWeight = Mathf.Lerp(currentRindaHandIKWeight, rindaTargetWeight, Time.deltaTime * handHoldIKBlendSpeed);
        currentNayaHandIKWeight = Mathf.Lerp(currentNayaHandIKWeight, nayaTargetWeight, Time.deltaTime * handHoldIKBlendSpeed);

        ApplyHandHoldIKWeights();
    }

    private float GetTargetIKWeight(RindaState state)
    {
        switch (state)
        {
            case RindaState.Holding:
                return handHoldFullIKWeight;
            case RindaState.Reaching:
                return handHoldReachIKWeight;
            default:
                return 0f;
        }
    }

    private float GetTargetIKWeight(NayaState state)
    {
        switch (state)
        {
            case NayaState.Holding:
                return handHoldFullIKWeight;
            case NayaState.Reaching:
                return handHoldReachIKWeight;
            default:
                return 0f;
        }
    }

    private void UpdateIKTargets()
    {
        // Update IK target positions and rotations relative to the pivot
        if (handHoldPivotTransform == null)
            return;

        // Update Rinda's hand IK target
        if (rindaHandIKTarget != null)
        {
            Vector3 rindaTargetPos = handHoldPivotTransform.position + handHoldPivotTransform.TransformDirection(rindaHandIKOffset);
            rindaHandIKTarget.position = rindaTargetPos;
            
            // Apply rotation with offset
            Quaternion baseRotation = handHoldPivotTransform.rotation;
            Quaternion rotationOffset = Quaternion.Euler(rindaHandIKRotationOffset);
            rindaHandIKTarget.rotation = baseRotation * rotationOffset;
        }

        // Update Naya's hand IK target
        if (nayaHandIKTarget != null)
        {
            Vector3 nayaTargetPos = handHoldPivotTransform.position + handHoldPivotTransform.TransformDirection(nayaHandIKOffset);
            nayaHandIKTarget.position = nayaTargetPos;
            
            // Apply rotation with offset
            Quaternion baseRotation = handHoldPivotTransform.rotation;
            Quaternion rotationOffset = Quaternion.Euler(nayaHandIKRotationOffset);
            nayaHandIKTarget.rotation = baseRotation * rotationOffset;
        }
    }

    private void ApplyHandHoldIKWeights()
    {
        if (rindaHandIKConstraint != null)
        {
            rindaHandIKConstraint.weight = currentRindaHandIKWeight;
        }

        if (nayaHandIKConstraint != null)
        {
            nayaHandIKConstraint.weight = currentNayaHandIKWeight;
        }
    }

    private void ValidateHandIKSetup()
    {
        if (rindaHandIKConstraint == null)
        {
            Debug.LogWarning("Referensi rindaHandIKConstraint belum diatur di _GP_HandHold_Mng.");
        }

        if (nayaHandIKConstraint == null)
        {
            Debug.LogWarning("Referensi nayaHandIKConstraint belum diatur di _GP_HandHold_Mng.");
        }

        if (rindaHandIKTarget == null)
        {
            Debug.LogWarning("Referensi rindaHandIKTarget belum diatur di _GP_HandHold_Mng.");
        }

        if (nayaHandIKTarget == null)
        {
            Debug.LogWarning("Referensi nayaHandIKTarget belum diatur di _GP_HandHold_Mng.");
        }
    }

    private void UpdatePendingJump()
    {
        // Check if it's time to apply Rinda's queued jump
        if (pendingRindaJumpTime > 0 && Time.time >= pendingRindaJumpTime)
        {
            ApplyJumpToRinda();
            pendingRindaJumpTime = -999f;
        }
    }

    private void ApplyJumpToRinda()
    {
        if (playerComponentsRinda == null)
            return;

        // Trigger Rinda's jump directly - same as if the player pressed jump
        playerComponentsRinda.TriggerJump();
    }
    #endregion

    #region Checking Methods
    private void OnPivotFollowTarget()
    {
        if (handHoldPivotTarget != null && handHoldPivotTransform != null)
        {
            CharacterController pivotController = handHoldPivotTransform.GetComponent<CharacterController>();
            
            if (pivotController != null && pivotController.enabled)
            {
                // Use CharacterController for collision-aware movement
                Vector3 currentPos = handHoldPivotTransform.position;
                Vector3 targetPos = handHoldPivotTarget.position;
                
                // Calculate desired position using Lerp for smooth following
                Vector3 desiredPos = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * handHoldFollowTargetSpeed);
                
                // Calculate movement velocity for this frame
                Vector3 moveVelocity = (desiredPos - currentPos) / Time.deltaTime;
                
                // Use CharacterController.Move() to respect collisions with obstacles
                pivotController.Move(moveVelocity * Time.deltaTime);
            }
            else
            {
                // Fallback to direct Lerp if no CharacterController
                handHoldPivotTransform.position = Vector3.Lerp(handHoldPivotTransform.position, handHoldPivotTarget.position, Time.deltaTime * handHoldFollowTargetSpeed);
            }
            
            // Lerp rotation smoothly
            handHoldPivotTransform.rotation = Quaternion.Slerp(handHoldPivotTransform.rotation, handHoldPivotTarget.rotation, Time.deltaTime * handHoldFollowTargetRotationSpeed);
        }
    }
    private void CheckPlayerDistance()
    {
        if (playerRinda == null || playerNaya == null)
        {
            isPlayersInRange = false;
            return;
        }

        // Optimized distance check using sqrMagnitude
        Vector3 toPlayer = playerRinda.transform.position - playerNaya.transform.position;
        float sqrDistance = toPlayer.sqrMagnitude;
        float sqrRange = handHoldDetectionRange * handHoldDetectionRange;

        if (sqrDistance <= sqrRange)
        {
            isPlayersInRange = true;
            // Debug.Log("Pemain berada dalam jarak deteksi untuk hand-hold.");
        }
        else
        {
            isPlayersInRange = false;
            // Debug.Log("Pemain terlalu jauh untuk melakukan hand-hold.");
        }
    }

    private void CheckPlayerLOS()
    {
        if (playerRinda == null || playerNaya == null)
        {
            isObstacleBetweenPlayers = true;
            return;
        }

        // Ray origin with 1 unit Y offset
        Vector3 rayOrigin = playerNaya.transform.position + Vector3.up * 1f;
        Vector3 targetPosition = new Vector3(playerRinda.transform.position.x, rayOrigin.y, playerRinda.transform.position.z);
        Vector3 toPlayer = targetPosition - rayOrigin;
        
        // Optimized distance check using sqrMagnitude
        float sqrDistance = toPlayer.sqrMagnitude;
        float sqrRange = handHoldDetectionRange * handHoldDetectionRange;
        
        if (sqrDistance > sqrRange)
        {
            // Outside detection range
            isObstacleBetweenPlayers = true;
            Debug.DrawRay(rayOrigin, toPlayer.normalized * handHoldDetectionRange, Color.red);
            return;
        }

        // Perform raycast
        float actualDistance = Mathf.Sqrt(sqrDistance);
        if (Physics.Raycast(rayOrigin, toPlayer.normalized, out RaycastHit hit, actualDistance))
        {
            // Check if we hit Rinda specifically
            bool hitRinda = hit.collider.gameObject == playerRinda || hit.collider.transform.IsChildOf(playerRinda.transform);

            if (hitRinda && hit.collider.CompareTag("Player"))
            {
                // Check if the player has Player_Components script
                Player_Components playerComponent = hit.collider.GetComponent<Player_Components>();

                if (playerComponent != null)
                {
                    // Clear obstacle flag - line of sight is clear
                    isObstacleBetweenPlayers = false;
                    // Draw debug ray in green when player is detected
                    Debug.DrawRay(rayOrigin, toPlayer.normalized * hit.distance, Color.green);
                    // Debug.Log("Rinda memiliki line of sight ke Naya.");
                }
                else
                {
                    // Hit Rinda but without the required script
                    isObstacleBetweenPlayers = true;
                    Debug.DrawRay(rayOrigin, toPlayer.normalized * hit.distance, Color.yellow);
                    Debug.Log("Rinda terdeteksi tapi tanpa komponen yang diperlukan.");
                }
            }
            else
            {
                // Hit something else blocking the line of sight
                isObstacleBetweenPlayers = true;
                Debug.DrawRay(rayOrigin, toPlayer.normalized * hit.distance, Color.red);
                Debug.Log("Rinda tidak memiliki line of sight ke Naya - ada penghalang.");
            }
        }
        else
        {
            // No hit - clear line of sight
            isObstacleBetweenPlayers = false;
            Debug.DrawRay(rayOrigin, toPlayer.normalized * actualDistance, Color.green);
            Debug.Log("Line of sight clear to Rinda.");
        }
    }
    #endregion

    private void OnRindaState()
    {
        // Logika untuk mengelola state hand-hold Rinda
        switch (currentRindaState)
        {
            case RindaState.None:
                // Logika untuk state None
                break;
            case RindaState.Holding:
                // Logika untuk state Holding
                break;
            case RindaState.Reaching:
                // Logika untuk state Reaching
                break;
        }
    }

    private void OnNayaState()
    {
        // Logika untuk mengelola state hand-hold Naya
        switch (currentNayaState)
        {
            case NayaState.None:
                // Logika untuk state None
                break;
            case NayaState.Holding:
                // Logika untuk state Holding
                break;
            case NayaState.Reaching:
                // Logika untuk state Reaching
                break;
        }
    }
}