using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class Player_Components : GameplayBehaviour
{
    [Header("Module Input Play")]
    public _ModuleInputPlay moduleInputPlay;

    public Transform cameraTransform;
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float jumpHeight = 1.2f;
    public float gravity = -9.81f;
    public float rotationSpeed = 10f;
    public float crouchHeight = 1f;
    public float standingHeight = 2f;
    public float crouchSpeed = 2f;

    [Header("Acceleration Settings")]
    public float maxAcceleration = 20f;
    public float maxDeceleration = 20f;
    public float airControl = 0.5f;

    [Header("Friction & Slope Sliding")]
    public float groundFriction = 8f;
    public float slideGravity = 10f;
    public float slopeRayLength = 1.5f;

    [Header("Player States")]
    public bool IsIdle;
    public bool IsFall;
    public bool IsJump;
    public bool IsCrouch;
    public bool IsAction1;
    public bool IsAction2;

    #region Private Variables
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCrouching = false;
    private Animator anim;
    private ActionState currentActionState;
    private float currentMoveValue = 0f;
    public float moveAnimationSpeed = 5f;
    #endregion

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        
        if (moduleInputPlay != null)
        {
            moduleInputPlay.OnAction += Action;
            anim = GetComponent<Animator>();
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
        Debug.Log($"<color=green>Player controls ENABLED - isActive={isActive}</color>");
    }

    protected override void OnGameplayDisabled()
    {
        // Stop player movement when leaving gameplay mode
        velocity = Vector3.zero;
        currentMoveValue = 0f;
        if (anim != null)
            anim.SetFloat("Move", 0f);
        Debug.Log($"<color=red>Player controls DISABLED - isActive={isActive}</color>");
    }

    private void Update()
    {
        // Only allow player control during Gameplay mode
        if (!isActive)
        {
            return;
        }

        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        HandleMove();
        
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
    }

    #region Movement Helpers
    private Vector3 GetMovementInput()
    {
        Vector3 inputDir = moduleInputPlay != null ? moduleInputPlay.MoveHandler : Vector3.zero;
        
        // Resolve camera transform
        Transform cam = cameraTransform != null ? cameraTransform : Camera.main != null ? Camera.main.transform : null;
        if (cameraTransform == null && cam != null)
        {
            cameraTransform = cam;
        }

        // Calculate movement direction relative to camera
        Vector3 moveDir;
        if (cam != null)
        {
            moveDir = cam.forward * inputDir.z + cam.right * inputDir.x;
        }
        else
        {
            Debug.LogWarning("No camera transform available — using player-local axes.");
            moveDir = transform.forward * inputDir.z + transform.right * inputDir.x;
        }

        moveDir.y = 0f;
        return moveDir.normalized;
    }

    private float CalculateTargetSpeed()
    {
        bool isSprinting = moduleInputPlay != null && currentActionState == ActionState.Sprint;
        Vector3 inputDir = moduleInputPlay != null ? moduleInputPlay.MoveHandler : Vector3.zero;
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
        
        if (anim != null)
            anim.SetFloat("Move", currentMoveValue);

        return targetSpeed;
    }

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

    private void RotateTowardsMovement(Vector3 horizontalVelocity)
    {
        Vector3 lookDir = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z);
        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }
    #endregion

    private void HandleMove()
    {
        Vector3 moveDir = GetMovementInput();
        float targetSpeed = CalculateTargetSpeed();
        Vector3 horizontalVelocity = ApplyMovementPhysics(moveDir, targetSpeed);
        
        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;
        
        RotateTowardsMovement(horizontalVelocity);
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleJump()
    {
        if (isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
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

    private void HandleCrouch()
    {
        if (isGrounded)
        {
            isCrouching = !isCrouching;

            if (isCrouching)
            {
                controller.height = crouchHeight;
                controller.center = new Vector3(0, crouchHeight / 2f, 0);
            }
            else
            {
                controller.height = standingHeight;
                controller.center = new Vector3(0, standingHeight / 2f, 0);
            }
        }
        else
        {
            Debug.Log("Cannot crouch while in the air.");
        }
    }

    private void HandleIdle()
    {
        if (isGrounded && velocity.x == 0 && velocity.z == 0)
        {
            IsIdle = true;
        }
        else
        {
            IsIdle = false;
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

        #region Action States
    private void Action(ActionState state)
    {
        switch (state)
        {
            case ActionState.Idle:
                currentActionState = ActionState.Idle;
                HandleIdle();
                break;
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
                Debug.Log("Interact Action Triggered");
                break;
            case ActionState.Action1:
                currentActionState = ActionState.Action1;
                anim.SetTrigger("IsAction1");
                Debug.Log("Action1 Triggered");
                break;
            case ActionState.Action2:
                currentActionState = ActionState.Action2;
                anim.SetTrigger("IsAction2");
                Debug.Log("Action2 Triggered");
                break;
        }
    }
    #endregion
}
