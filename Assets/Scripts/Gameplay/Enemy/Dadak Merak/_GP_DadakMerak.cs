using UnityEngine;
using TMPro;
using UnityEngine.Events;
using NUnit.Framework.Internal;
using Unity.Collections;
using UnityEngine.UI;

public class _GP_DadakMerak : MonoBehaviour
{
    #region Private Variables
    [Header("Target Object")]
    [Tooltip("The target object to detect")]
    [SerializeField] private GameObject _TargetObject;
    [SerializeField] private string _TargetObjectName;

    [Header("Detection Settings")]
    [Tooltip("Spotlight component that visualizes the detection cone")]
    [SerializeField] private Light _DetectionSpotlight;
    [Tooltip("Layer mask for line of sight detection")]
    [SerializeField] private LayerMask _DetectionLayer;
    public float raycastOffset = 1f;
    [Tooltip("How often to check for player (in seconds). Lower = more accurate, higher = better performance")]
    [SerializeField] private float detectionInterval = 0.15f;
    #endregion

    [Header("Target Locking Settings")]
    [Tooltip("Maximum threshold value before player gets locked on")]
    [SerializeField] private float _MaxLockThreshold = 100f;
    [Tooltip("How fast the threshold increases per second when player is detected")]
    [SerializeField] private float _ThresholdFillRate = 20f;
    [Tooltip("How fast the threshold decreases per second when player is not detected")]
    [SerializeField] private float _ThresholdDecreaseRate = 30f;
    [Tooltip("Rotation speed towards player")]
    [SerializeField] private float _RotationSpeed = 0.5f;
    [SerializeField] private float _CurrentLockThreshold = 0f;
    private bool _IsPlayerDetected = false;
    private bool _IsLockedOn = false;
    
    // Optimization variables
    private GameObject _cachedPlayer;
    private float _nextDetectionTime;

    [Header("Debug")]
    #region Debug
    public TextMeshProUGUI debugText;
    #endregion

    public UnityEvent playerGotLockedOn;

    #region Performance Tracking (Remove after testing)
    private int detectionChecksThisSecond = 0;
    private float lastResetTime = 0f;
    #endregion

    private GameObject GetPlayer()
    {
        // Cache player reference, re-find if null (handles respawn)
        if (_cachedPlayer == null)
        {
            _cachedPlayer = GameObject.FindGameObjectWithTag("Player");
        }
        return _cachedPlayer;
    }

    private bool IsPlayerInLOS(out GameObject player)
    {
        player = null;

        if (_DetectionSpotlight == null)
            return false;

        // Get cached player reference
        GameObject targetPlayer = GetPlayer();
        if (targetPlayer == null)
            return false;

        Vector3 spotlightPosition = _DetectionSpotlight.transform.position;
        Vector3 spotlightForward = _DetectionSpotlight.transform.forward;
        
        // Aim at player's center (chest height) instead of feet for more reliable detection
        Vector3 playerCenter = targetPlayer.transform.position + Vector3.up * raycastOffset;
        Vector3 toPlayer = playerCenter - spotlightPosition;
        
        // 1. Distance check (optimized with sqrMagnitude - no sqrt!)
        float sqrDistance = toPlayer.sqrMagnitude;
        float sqrRange = _DetectionSpotlight.range * _DetectionSpotlight.range;
        if (sqrDistance > sqrRange)
        {
            Debug.DrawRay(spotlightPosition, toPlayer.normalized * _DetectionSpotlight.range, Color.red);
            return false;
        }

        // 2. Angle check - is player within spotlight cone?
        Vector3 directionToPlayer = toPlayer.normalized;
        float angleToPlayer = Vector3.Angle(spotlightForward, directionToPlayer);
        if (angleToPlayer > _DetectionSpotlight.spotAngle / 2f)
        {
            Debug.DrawRay(spotlightPosition, directionToPlayer * Mathf.Sqrt(sqrDistance), Color.red);
            return false;
        }

        // 3. Raycast check - is there clear line of sight? (most expensive, done last)
        float actualDistance = Mathf.Sqrt(sqrDistance);
        if (Physics.Raycast(spotlightPosition, directionToPlayer, out RaycastHit hit, actualDistance, _DetectionLayer))
        {
            if (hit.collider.CompareTag("Player"))
            {
                Debug.DrawRay(spotlightPosition, directionToPlayer * actualDistance, Color.green);
                player = targetPlayer;
                return true;
            }
            else
            {
                // Hit something else (wall, obstacle) blocking the view
                Debug.DrawRay(spotlightPosition, directionToPlayer * actualDistance, Color.yellow);
                return false;
            }
        }

        // Raycast didn't hit anything (no obstacles in the way, but also didn't reach player)
        Debug.DrawRay(spotlightPosition, directionToPlayer * actualDistance, Color.red);
        return false;
    }

    private void Update()
    {
        // OPTIMIZATION: Only check line of sight at intervals, not every frame
        if (Time.time >= _nextDetectionTime)
        {
            _nextDetectionTime = Time.time + detectionInterval;
            
            // Performance tracking
            detectionChecksThisSecond++;
            if (Time.time - lastResetTime >= 1f)
            {
                Debug.Log($"[PERFORMANCE] Detection checks per second: {detectionChecksThisSecond} (Target: ~{1f/detectionInterval:F0})");
                detectionChecksThisSecond = 0;
                lastResetTime = Time.time;
            }
            
            // Perform the expensive detection check
            if (IsPlayerInLOS(out GameObject detectedPlayer))
            {
                _TargetObject = detectedPlayer;
                _TargetObjectName = _TargetObject.name;
                _IsPlayerDetected = true;
            }
            else
            {
                _IsPlayerDetected = false;
            }
        }
        
        // Update threshold every frame for smooth behavior (based on last detection result)
        if (_IsPlayerDetected)
        {
            // Increase lock threshold
            _CurrentLockThreshold += _ThresholdFillRate * Time.deltaTime;
            _CurrentLockThreshold = Mathf.Clamp(_CurrentLockThreshold, 0f, _MaxLockThreshold);

            debugText.text = $"Player Detected: {_TargetObjectName}\nThreshold: {_CurrentLockThreshold:F1}/{_MaxLockThreshold}";
            
            // Check if threshold reached maximum
            if (_CurrentLockThreshold >= _MaxLockThreshold && !_IsLockedOn)
            {
                _IsLockedOn = true;
                playerGotLockedOn?.Invoke();
                debugText.text += "\n<color=red>LOCKED ON!</color>";
            }

            // Only rotate to face player after locked on
            if (_IsLockedOn && _TargetObject != null)
            {
                Vector3 targetDirection = (_TargetObject.transform.position - transform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _RotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            // Decrease threshold when player is not detected
            _CurrentLockThreshold -= _ThresholdDecreaseRate * Time.deltaTime;
            _CurrentLockThreshold = Mathf.Clamp(_CurrentLockThreshold, 0f, _MaxLockThreshold);
            
            if (_CurrentLockThreshold <= 0f)
            {
                _IsLockedOn = false;
                debugText.text = "No Player Detected";
            }
            else
            {
                debugText.text = $"Losing Target...\nThreshold: {_CurrentLockThreshold:F1}/{_MaxLockThreshold}";
            }
        }
    }
}
