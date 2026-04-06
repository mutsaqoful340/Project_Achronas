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

    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;
    public float maxHorizontalAngle = 90f;  // Left/right limits
    public float maxVerticalAngle = 60f;    // Up/down limits

    private Quaternion initialHeadRotation;
    private bool isFocusing = false;
    private ActionState currentActionState = ActionState.Idle;

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

        if (_inputPlayer == null){
            _inputPlayer = GetComponent<_ModuleInputPlay>();
        }

        initialHeadRotation = headBone.localRotation;
    }

    private void LateUpdate()
    {
        if (isFocusing && headTurnTarget != null)
        {
            TurnHeadToTarget(headTurnTarget.position);
        }

        // Immediately check if player is in "Reaching" state
        if (currentActionState != ActionState.HandHold || !isFocusing)
        {
            ResetHeadRotation();
            StopFocusing();
        }
    }

    private void TrackActionState(ActionState state)
    {
        currentActionState = state;
    }

    public void OnFocusTarget(ActionState state)
    {
        if (state != ActionState.HandHold)
            return;

        Debug.Log("<color=cyan>_GP_HeadTurning: Received HandHold action.</color>");
        
        // Toggle focus on/off
        if (isFocusing)
        {
            StopFocusing();
            Debug.Log("<color=red>Stop focusing</color>");
        }
        else
        {
            if (headTurnTarget == null)
            {
                Debug.LogError("Head Turn Target is NULL!");
                return;
            }

            isFocusing = true;
            Debug.Log($"<color=green>Start focusing on {headTurnTarget.name}</color>");
        }
    }

    public void SetLookAtTarget(Transform target)
    {
        headTurnTarget = target;
        isFocusing = true;
    }

    public void TurnHeadToTarget(Vector3 targetPosition)
    {
        Vector3 directionToTarget = (targetPosition - headBone.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);

        // Apply rotation limits relative to initial rotation
        targetRotation = ClampRotation(targetRotation);

        // Smoothly interpolate to target rotation
        headBone.rotation = Quaternion.Slerp(
            headBone.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    private Quaternion ClampRotation(Quaternion targetRotation)
    {
        // Convert to relative rotation from initial
        Quaternion relativeRotation = Quaternion.Inverse(initialHeadRotation) * targetRotation;
        Vector3 eulerAngles = relativeRotation.eulerAngles;

        // Normalize angles to -180 to 180 range
        eulerAngles.x = NormalizeAngle(eulerAngles.x);
        eulerAngles.y = NormalizeAngle(eulerAngles.y);

        // Clamp vertical (X) and horizontal (Y) rotation
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
        headBone.localRotation = initialHeadRotation;
    }
}