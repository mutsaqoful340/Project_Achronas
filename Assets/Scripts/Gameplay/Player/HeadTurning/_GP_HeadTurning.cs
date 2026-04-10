using System;
using System.Reflection;
using UnityEngine;

public class _GP_HeadTurning : MonoBehaviour
{
    [Header("References")]
    public Transform headBone;
    public Transform headTurnTarget;
    public _ModuleInputPlay _inputPlayer;
    public Player_Components playerComponents;
    public Animator animator;

    [Header("Rotation Settings")]
    public float rotationSpeed = 15f;
    public float resetSpeed = 5f;  // Slower speed for resetting
    public float maxHorizontalAngle = 90f;
    public float maxVerticalAngle = 60f;

    private Quaternion initialHeadRotation;
    private Quaternion currentHeadRotation;  // Track rotation independently from animator
    private Quaternion targetHeadRotation;   // Target for reset animation
    private bool isFocusing = false;
    private bool isResetting = false;
    private ActionState currentActionState = ActionState.Idle;
    private ActionState previousActionState = ActionState.Idle;

    void OnEnable()
    {
        _inputPlayer.OnAction += OnFocusTarget;
        _inputPlayer.OnAction += TrackActionState;
    }

    void OnDisable()
    {
        _inputPlayer.OnAction -= OnFocusTarget;
        _inputPlayer.OnAction -= TrackActionState;
    }

    private void Start()
    {
        if (headBone == null)
        {
            Debug.LogError("Head bone not assigned on " + gameObject.name);
            enabled = false;
            return;
        }

        if (_inputPlayer == null)
        {
            _inputPlayer = GetComponent<_ModuleInputPlay>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Enable IK Pass on this layer so OnAnimatorIK() gets called
        if (animator != null)
        {
            animator.SetLayerWeight(0, 1f); // Ensure base layer is active
            // Note: You may need to manually enable "IK Pass" on animator layers in the inspector
        }

        initialHeadRotation = headBone.localRotation;
        currentHeadRotation = initialHeadRotation;
    }

    private void LateUpdate()
    {
        // Handle resetting (smooth interpolation back to initial) - prioritize this
        if (isResetting)
        {
            currentHeadRotation = Quaternion.Slerp(
                currentHeadRotation,
                targetHeadRotation,
                Time.deltaTime * resetSpeed  // Use slower reset speed
            );
            headBone.localRotation = currentHeadRotation;
            
            // Stop resetting once close enough
            if (Quaternion.Angle(currentHeadRotation, targetHeadRotation) < 0.1f)
            {
                isResetting = false;
                currentHeadRotation = targetHeadRotation;
                headBone.localRotation = targetHeadRotation;
            }
            
            // Don't focus while resetting
            return;
        }

        if (isFocusing && headTurnTarget != null)
        {
            TurnHeadToTarget(headTurnTarget.position);
        }

        // Only reset when transitioning OUT of HandHold
        if (previousActionState == ActionState.HandHold && currentActionState != ActionState.HandHold)
        {
            targetHeadRotation = initialHeadRotation;
            isResetting = true;
            isFocusing = false;
            Debug.Log("Transitioning out of HandHold state.");
        }
        
        previousActionState = currentActionState;
    }

    private void TrackActionState(ActionState state)
    {
        currentActionState = state;
    }

    public void OnFocusTarget(ActionState state)
    {
        if (state != ActionState.HandHold)
            return;

        // Don't allow focus while resetting
        if (isResetting)
            return;

        Debug.Log("<color=cyan>_GP_HeadTurning: Received HandHold action.</color>");
        
        // Toggle focus on/off
        if (isFocusing)
        {
            isFocusing = false;
            Debug.Log("<color=red>Stop focusing</color>");
        }
        else
        {
            if (headTurnTarget == null)
            {
                Debug.LogError("Head Turn Target is NULL!");
                return;
            }

            // Reset currentHeadRotation to current bone position to allow smooth interpolation
            currentHeadRotation = headBone.localRotation;
            isResetting = false;
            isFocusing = true;
            Debug.Log($"<color=green>Start focusing on {headTurnTarget.name}</color>");
        }
    }

    public void SetLookAtTarget(Transform target)
    {
        if (target == null)
            return;
            
        headTurnTarget = target;
        isFocusing = true;
    }

    public void TurnHeadToTarget(Vector3 targetPosition)
    {
        Vector3 directionToTarget = (targetPosition - headBone.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);

        // Convert to local rotation if has parent
        if (headBone.parent != null)
        {
            targetRotation = Quaternion.Inverse(headBone.parent.rotation) * targetRotation;
        }

        // Apply rotation limits
        targetRotation = ClampRotation(targetRotation);

        // Interpolate from our stored rotation (not from animator's modified value)
        currentHeadRotation = Quaternion.Slerp(
            currentHeadRotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
        
        // Apply directly to bone (overrides animator)
        headBone.localRotation = currentHeadRotation;
    }

    private Quaternion ClampRotation(Quaternion targetRotation)
    {
        Quaternion relativeRotation = Quaternion.Inverse(initialHeadRotation) * targetRotation;
        Vector3 eulerAngles = relativeRotation.eulerAngles;

        eulerAngles.x = NormalizeAngle(eulerAngles.x);
        eulerAngles.y = NormalizeAngle(eulerAngles.y);

        eulerAngles.x = Mathf.Clamp(eulerAngles.x, -maxVerticalAngle, maxVerticalAngle);
        eulerAngles.y = Mathf.Clamp(eulerAngles.y, -maxHorizontalAngle, maxHorizontalAngle);

        relativeRotation = Quaternion.Euler(eulerAngles);
        return initialHeadRotation * relativeRotation;
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    public void StopFocusing()
    {
        isFocusing = false;
    }

    public void ResetHeadRotation()
    {
        // Start the smooth reset animation instead of snapping
        targetHeadRotation = initialHeadRotation;
        isResetting = true;
        isFocusing = false;
    }
}