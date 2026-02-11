using UnityEngine;
using TMPro;
using UnityEngine.Events;
using NUnit.Framework.Internal;
using Unity.Collections;
using UnityEngine.UI;

public class _GP_SingaBarong : MonoBehaviour
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

    [Header("Debug")]
    #region Debug
    public TextMeshProUGUI debugText;
    #endregion

    public UnityEvent playerGotLockedOn;

    private bool IsPlayerInLOS(out GameObject player)
    {
        player = null;

        if (_DetectionSpotlight == null)
            return false;

        // Find ALL players in scene
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        if (allPlayers.Length == 0)
            return false;

        Vector3 spotlightPosition = _DetectionSpotlight.transform.position;
        Vector3 spotlightForward = _DetectionSpotlight.transform.forward;

        // Check each player
        foreach (GameObject potentialPlayer in allPlayers)
        {
            // Aim at player's center (chest height) instead of feet for more reliable detection
            Vector3 playerCenter = potentialPlayer.transform.position + Vector3.up * raycastOffset;
            Vector3 directionToPlayer = (playerCenter - spotlightPosition).normalized;
            float distanceToPlayer = Vector3.Distance(spotlightPosition, playerCenter);

            // 1. Distance check - is player within spotlight range?
            if (distanceToPlayer > _DetectionSpotlight.range)
                continue;

            // 2. Angle check - is player within spotlight cone?
            float angleToPlayer = Vector3.Angle(spotlightForward, directionToPlayer);
            if (angleToPlayer > _DetectionSpotlight.spotAngle / 2f)
                continue;

            // 3. Raycast check - is there clear line of sight?
            if (Physics.Raycast(spotlightPosition, directionToPlayer, out RaycastHit hit, distanceToPlayer))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    Debug.DrawRay(spotlightPosition, directionToPlayer * distanceToPlayer, Color.green);
                    player = potentialPlayer;
                    return true;
                }
                else
                {
                    // Hit something else (wall, obstacle)
                    Debug.DrawRay(spotlightPosition, directionToPlayer * distanceToPlayer, Color.yellow);
                    continue;
                }
            }

            Debug.DrawRay(spotlightPosition, directionToPlayer * distanceToPlayer, Color.red);
        }

        return false;
    }

    private void Update()
    {
        _IsPlayerDetected = false;
        
        // Check if player is in line of sight
        if (IsPlayerInLOS(out GameObject detectedPlayer))
        {
            _TargetObject = detectedPlayer;
            _TargetObjectName = _TargetObject.name;
            _IsPlayerDetected = true;

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
            if (_IsLockedOn)
            {
                Vector3 targetDirection = (_TargetObject.transform.position - transform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _RotationSpeed * Time.deltaTime);
            }
        }
        
        // Decrease threshold when player is not detected
        if (!_IsPlayerDetected)
        {
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
