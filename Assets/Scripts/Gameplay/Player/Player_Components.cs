using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player_Components : GameplayBehaviour
{
    [Header("Module Input Play")]
    public _ModuleInputPlay moduleInputPlay;

    [Header("Animator")]
    [Tooltip("Animator component untuk mengendalikan animasi karakter.")]
    public Animator animator;

    [Header("Assigned Gamepad")]
    [Tooltip("The specific gamepad assigned to this player (set by CharacterAssignmentManager)")]
    [HideInInspector] public InputDevice assignedDevice;

    [Header("Camera")]
    [Tooltip("Kamera yang akan digunakan untuk menentukan arah gerakan relatif terhadap pandangan pemain.")]
    public Transform cameraTransform;

    [Header("Movement Settings")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float jumpHeight = 1.2f;
    public float gravity = -9.81f;
    public float rotationSpeed = 10f;
    public float crouchHeight = 1f;
    public float standingHeight = 2f;
    public float crouchSpeed = 2f;
    [Tooltip("Speed at which the Move parameter in the animator transitions to the target value")]
    public float moveAnimationSpeed = 5f;

    [Header("Acceleration Settings")]
    public float maxAcceleration = 20f;
    public float maxDeceleration = 20f;
    public float airControl = 0.5f;

    [Header("Friction & Slope Sliding")]
    public float groundFriction = 8f;
    public float slideGravity = 10f;
    public float slopeRayLength = 1.5f;

    [Header("Ground Check Settings")]
    [Tooltip("Distance to check for ground below the character")]
    public float groundCheckDistance = 0.2f;
    [Tooltip("Radius of the ground check sphere")]
    public float groundCheckRadius = 0.3f;
    [Tooltip("Layers that count as ground")]
    public LayerMask groundLayers = ~0; // Default to everything

    [Header("Strafe Detection Settings")]
    [Tooltip("Minimum input magnitude to consider for strafe detection (e.g., 0.3 means 30% of stick deflection)")]
    public float strafeInputThreshold = 0.3f;  // Lateral input magnitude to trigger strafe detection
    [Tooltip("Grace period after last detected strafe input to allow brief input dips without resetting strafe state")]
    public float strafeGracePeriod = 0.1f;  // Allow brief input dips without resetting strafe state
    [Tooltip("Time window to count rotation direction changes")]
    public float rotationChangeWindow = 1f;  // Time window to count rotation direction changes
    [Tooltip("Number of direction reversals to trigger strafe")]
    public int rotationChangesForStrafe = 3;  // Number of direction reversals to trigger strafe
    [Tooltip("Minimum rotation change to count as a change (degrees)")]
    public float minRotationDeltaForDetection = 15f;  // Minimum rotation change to count as a change (degrees)

    [Header("Stumble Settings")]
    // public float stumbleDuration = 1.5f; // Duration of stumble effect in seconds
    [Tooltip("DO NOT TICK MANUALLY, THIS IS FOR DEBUGGING ONLY!")]
    [SerializeField] private bool isStumbling = false;

    [Header("Player States")]
    public bool IsIdle;
    public bool IsFall;
    public bool IsJump;
    public bool IsCrouch;
    public bool IsAction1;
    public bool IsAction2;
    public bool IsDepressed;

    #region Private Variables
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCrouching = false;
    private ActionState currentActionState;
    private float currentMoveValue = 0f;

    // Strafe Detection (Hybrid: rotation speed + reversal counting)
    private float lastYRotation = 0f;
    private float rotationSpeedAccumulator = 0f;
    private float rotationSpeedResetTimer = 0f;
    private int reversalCount = 0;
    private float lastRotationDelta = 0f;
    private bool strafeTriggered = false;
    #endregion

    #region Public Properties
    public Vector3 Velocity => velocity;
    #endregion
    
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        
        if (moduleInputPlay != null)
        {
            moduleInputPlay.OnAction += Action;
        }

        // Cache camera transform if not assigned
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        isStumbling = false;
    }

    /// <summary>
    /// Assign a specific gamepad to this player
    /// </summary>
    public void AssignDevice(InputDevice device)
    {
        assignedDevice = device;
        Debug.Log($"<color=cyan>✓ {gameObject.name}: Device assigned - {device?.name} (ID: {device?.deviceId})</color>");
    }

    /// <summary>
    /// Check if this player has a device assigned
    /// </summary>
    public bool HasDevice()
    {
        return assignedDevice != null;
    }
    
    /// <summary>
    /// Debug method to check player status
    /// </summary>
    [ContextMenu("Debug Player Status")]
    private void DebugPlayerStatus()
    {
        Debug.Log($"=== {gameObject.name} Status ===");
        Debug.Log($"isActive: {isActive}");
        Debug.Log($"assignedDevice: {(assignedDevice != null ? $"{assignedDevice.name} (ID: {assignedDevice.deviceId})" : "NULL")}");
        Debug.Log($"moduleInputPlay: {(moduleInputPlay != null ? "Assigned" : "NULL")}");
        Debug.Log($"controller: {(controller != null ? "Assigned" : "NULL")}");
        
        if (assignedDevice != null && moduleInputPlay != null)
        {
            Vector3 input = moduleInputPlay.GetMoveInput(assignedDevice);
            Debug.Log($"Current Input: ({input.x:F2}, {input.y:F2}, {input.z:F2})");
        }
    }

    private void OnDestroy()
    {
        if (moduleInputPlay != null)
        {
            moduleInputPlay.OnAction -= Action;
        }
    }

    protected override void OnGameplayEnabled()
    {
        // Reset velocity when entering gameplay mode
        velocity = Vector3.zero;
        Debug.Log($"<color=green>{gameObject.name}: Player controls ENABLED</color>");
    }

    protected override void OnGameplayDisabled()
    {
        // Stop player movement when leaving gameplay mode
        velocity = Vector3.zero;
        currentMoveValue = 0f;
        if (animator != null)
            animator.SetFloat("Move", 0f);
        Debug.Log($"<color=red>Player controls DISABLED - isActive={isActive}</color>");
    }

    private void Update()
    {
        // Only allow player control during Gameplay mode
        if (!isActive)
        {
            return;
        }

        // Update input from assigned gamepad
        if (moduleInputPlay != null && assignedDevice != null)
        {
            moduleInputPlay.UpdateInput(assignedDevice);
        }

        // Improved ground check using SphereCast
        isGrounded = CheckGround();
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;


        HandleMove();
        
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
    }

    #region Movement Helpers
    private bool CheckGround()
    {
        // Get the bottom center of the CharacterController (player's feet)
        Vector3 spherePosition = transform.position + (Vector3.down * controller.height / 2f) + (Vector3.up * controller.center.y);
        
        // Perform sphere check at the feet
        return Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    // Get movement input from InputActions, resolve camera-relative direction, and return normalized movement vector
    private Vector3 GetMovementInput()
    {
        Vector3 inputDir = (moduleInputPlay != null && assignedDevice != null) 
            ? moduleInputPlay.GetMoveInput(assignedDevice) 
            : Vector3.zero;
        
        // Detect strafe behavior based on rotation changes
        DetectStrafe(inputDir);

        // Calculate movement direction relative to camera
        Vector3 moveDir;
        if (cameraTransform != null)
        {
            moveDir = cameraTransform.forward * inputDir.z + cameraTransform.right * inputDir.x;
        }
        else
        {
            Debug.LogWarning("No camera transform available — using player-local axes.");
            moveDir = transform.forward * inputDir.z + transform.right * inputDir.x;
        }

        moveDir.y = 0f;
        return moveDir.normalized;
    }

    // Detect strafe based on rotation speed AND direction reversals (left-right-left pattern)
    private void DetectStrafe(Vector3 inputDir)
    {
        float currentYRotation = transform.eulerAngles.y;
        float rotationDelta = Mathf.DeltaAngle(lastYRotation, currentYRotation);
        float absDelta = Mathf.Abs(rotationDelta);
        
        // Accumulate rotation speed
        rotationSpeedAccumulator += absDelta;
        rotationSpeedResetTimer += Time.deltaTime;
        
        // Count direction reversals (direction changes) — only meaningful rotations
        if (absDelta > minRotationDeltaForDetection)
        {
            if (Mathf.Sign(rotationDelta) != Mathf.Sign(lastRotationDelta) && lastRotationDelta != 0f)
            {
                reversalCount++;
                Debug.Log($"Rotation reversal detected! Count: {reversalCount}");
            }
            lastRotationDelta = rotationDelta;
        }
        
        // Reset if time window expires
        if (rotationSpeedResetTimer > rotationChangeWindow)
        {
            float avgRotationSpeed = rotationSpeedAccumulator / rotationSpeedResetTimer;
            
            // Trigger ONLY if both conditions met: high speed AND reversals
            if (avgRotationSpeed > minRotationDeltaForDetection && 
                reversalCount >= rotationChangesForStrafe && 
                !strafeTriggered)
            {
                strafeTriggered = true;
                isStumbling = true;
                HandleStumble();
                Debug.Log($"Aggressive strafing detected! Speed: {avgRotationSpeed:F1}°/sec, Reversals: {reversalCount}");
            }
            
            rotationSpeedAccumulator = 0f;
            rotationSpeedResetTimer = 0f;
            reversalCount = 0;
            strafeTriggered = false;
        }
        
        lastYRotation = currentYRotation;
    }

// Determines the target speed based on player state and input, and updates the Move parameter for animations
    private float CalculateTargetSpeed()
    {
        bool isSprinting = moduleInputPlay != null && currentActionState == ActionState.Sprint;
        Vector3 inputDir = (moduleInputPlay != null && assignedDevice != null) 
            ? moduleInputPlay.GetMoveInput(assignedDevice) 
            : Vector3.zero;
        bool isMoving = inputDir.sqrMagnitude > 0.01f;
        
        float targetMoveValue = 0f;
        float targetSpeed = walkSpeed;
        
        if (!isMoving)
        {
            // Idle - no movement input
            targetMoveValue = 0f;
        }
        else if (isCrouching)
        {
            targetMoveValue = 0.5f;
            targetSpeed = crouchSpeed;
        }
        else if (isSprinting)
        {
            targetMoveValue = 2f;
            targetSpeed = runSpeed;
        }
        else
        {
            // Walking
            targetMoveValue = 1f;
            targetSpeed = walkSpeed;
        }

        // Smoothly interpolate the animator parameter
        currentMoveValue = Mathf.Lerp(currentMoveValue, targetMoveValue, moveAnimationSpeed * Time.deltaTime);
        
        // Snap to target if close enough to avoid floating point precision issues
        if (Mathf.Abs(currentMoveValue - targetMoveValue) < 0.01f)
        {
            currentMoveValue = targetMoveValue;
        }
        
        if (animator != null)
            animator.SetFloat("Move", currentMoveValue);

        return targetSpeed;
    }

    // Applies acceleration and deceleration to the player's movement, as well as ground friction and slope sliding
    private Vector3 ApplyMovementPhysics(Vector3 moveDir, float targetSpeed)
    {
        Vector3 desiredVelocity = moveDir * targetSpeed;
        Vector3 currentHorizontal = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 diff = desiredVelocity - currentHorizontal;

        // Calculate acceleration rate
        float accelRate = (desiredVelocity.sqrMagnitude > 0.01f) ? maxAcceleration : maxDeceleration;
        if (!isGrounded) 
            accelRate *= airControl;

        // Apply acceleration
        Vector3 velocityChange = Vector3.ClampMagnitude(diff, accelRate * Time.deltaTime);
        currentHorizontal += velocityChange;

        // Apply ground friction when idle
        if (isGrounded && desiredVelocity.sqrMagnitude < 0.01f && currentHorizontal.magnitude > 0f)
        {
            float frictionForce = groundFriction * Time.deltaTime;
            currentHorizontal = Vector3.MoveTowards(currentHorizontal, Vector3.zero, frictionForce);
        }

        // Apply slope sliding
        if (isGrounded && OnSteepSlope(out Vector3 slopeDir))
        {
            currentHorizontal += slopeDir * slideGravity * Time.deltaTime;
        }

        return currentHorizontal;
    }

    // Rotate player to face movement direction smoothly
    private void RotateTowardsMovement(Vector3 horizontalVelocity)
    {
        Vector3 lookDir = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z);
        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    private bool OnSteepSlope(out Vector3 slopeDir)
    {
        slopeDir = Vector3.zero;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, slopeRayLength))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle > controller.slopeLimit)
            {
                slopeDir = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                return true;
            }
        }
        return false;
    }
    #endregion

    #region Action Handlers
    public void HandleMove()
    {
        // Cannot move when depressed
        if (currentActionState == ActionState.Depressed)
        {
            return;
        }
        
        Vector3 moveDir = GetMovementInput();
        float targetSpeed = CalculateTargetSpeed();
        Vector3 horizontalVelocity = ApplyMovementPhysics(moveDir, targetSpeed);
        
        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;
        
        if (!isStumbling)
        {
            RotateTowardsMovement(horizontalVelocity);
        }
        
        if (controller.enabled != false)
        {
            controller.Move(velocity * Time.deltaTime);
        }
    }

    public void HandleJump()
    {
        if (isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("IsJump");
            Debug.Log("Jump executed.");
        }
        else if (isCrouching)
        {
            Debug.Log("Cannot jump while crouching.");
        }
        else if (!isGrounded)
        {
            Debug.Log("Cannot jump while in the air.");
        }
    }

    public void HandleCrouch()
    {
        if (isGrounded)
        {
            isCrouching = !isCrouching;

            if (isCrouching)
            {
                animator.SetTrigger("DoCrouch");
                controller.height = crouchHeight;
                controller.center = new Vector3(0, crouchHeight / 2f, 0);
            }
            else
            {
                animator.SetTrigger("DoCrouch");
                controller.height = standingHeight;
                controller.center = new Vector3(0, standingHeight / 2f, 0);
            }
        }
        else
        {
            Debug.Log("Cannot crouch while in the air.");
        }
    }

    public void HandleThrow()
    {
        var throwModule = GetComponent<_GP_ThrowItem>();
        if (throwModule != null)
        {
            if (throwModule._itemToThrow != null)
            {
                animator.SetTrigger("IsThrow");
            }
            else
            {
                Debug.Log("No item to throw.");
            }
        }
        else
        {
            Debug.LogWarning("No _GP_ThrowItem component found on this player or no item to throw.");
        }
    }

    public void HandleInteract()
    {
        var throwModule = GetComponent<_GP_ThrowItem>();
        if (throwModule != null)
        {
            throwModule.OnPickUpItem();
            Debug.Log("Interact/Pickup action executed.");
        }
    }

    private void HandleStumble()
    {
        currentActionState = ActionState.Stumble;
        var cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }
        animator.SetTrigger("IsStumble");
        Debug.Log("Stumble action executed.");
    }

    public void HandleRecoverFromStumble()
    {
        isStumbling = false;
        var cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = true;
        }
        Debug.Log("Recovered from stumble.");
    }

    public void HandleDepressed()
    {
        IsDepressed = !IsDepressed;
        if (IsDepressed)
        {
            currentActionState = ActionState.Depressed;
            animator.SetBool("IsDepressed", true);
        }
        else
        {
            currentActionState = ActionState.Idle;
            animator.SetBool("IsDepressed", false);
        }
        Debug.Log("Depressed action executed.");
    }
    #endregion

    #region Action States
    private void Action(ActionState state)
    {
        switch (state)
        {
            case ActionState.Sprint:
                // If crouching, stand up first
                if (isCrouching)
                {
                    isCrouching = false;
                    controller.height = standingHeight;
                    controller.center = new Vector3(0, standingHeight / 2f, 0);
                    Debug.Log("Uncrouch to sprint");
                }
                currentActionState = ActionState.Sprint;
                Debug.Log("Sprint started");
                break;
            case ActionState.Crouch:
                currentActionState = ActionState.Crouch;
                HandleCrouch();
                Debug.Log("Crouch Action Triggered");
                break;
            case ActionState.Jump:
                currentActionState = ActionState.Jump;
                HandleJump();
                Debug.Log("Jump Action Triggered");
                break;
            case ActionState.Interact:
                currentActionState = ActionState.Interact;
                HandleInteract();
                Debug.Log("Interact Action Triggered");
                break;
            case ActionState.Throw:
                currentActionState = ActionState.Throw;
                HandleThrow();
                Debug.Log("Throw Action Triggered");
                break;
            case ActionState.Action1:
                currentActionState = ActionState.Action1;
                animator.SetTrigger("IsAction1");
                Debug.Log("Action1 Triggered");
                break;
            case ActionState.Action2:
                currentActionState = ActionState.Action2;
                animator.SetTrigger("IsAction2");
                Debug.Log("Action2 Triggered");
                break;
        }
    }
    #endregion

    #region Debug Visualization
    private void OnDrawGizmosSelected()
    {
        if (controller == null) return;

        // Visualize ground check sphere at player's feet
        Vector3 spherePosition = transform.position + (Vector3.down * controller.height / 2f) + (Vector3.up * controller.center.y);
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }
    #endregion
}