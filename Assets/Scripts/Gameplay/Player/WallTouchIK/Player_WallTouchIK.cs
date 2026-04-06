using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Controls hand IK to make hands touch walls/obstacles when player is close to them.
/// Requires Unity Animation Rigging package and properly configured Rig with TwoBoneIK constraints.
/// </summary>
public class Player_WallTouchIK : MonoBehaviour
{
    [Header("IK Constraints")]
    [Tooltip("TwoBoneIK constraint for the left hand")]
    public TwoBoneIKConstraint leftHandIK;
    
    [Tooltip("TwoBoneIK constraint for the right hand")]
    public TwoBoneIKConstraint rightHandIK;

    [Header("IK Targets")]
    [Tooltip("Transform that acts as IK target for left hand (must be assigned in leftHandIK constraint)")]
    public Transform leftHandTarget;
    
    [Tooltip("Transform that acts as IK target for right hand (must be assigned in rightHandIK constraint)")]
    public Transform rightHandTarget;

    [Header("Detection Settings")]
    [Tooltip("Distance from character to start detecting walls")]
    public float detectionDistance = 1.5f;
    
    [Tooltip("Distance from character where hands fully touch the wall")]
    public float touchDistance = 0.6f;
    
    [Tooltip("Height offset for hand placement from character center")]
    public float handHeightOffset = 0.3f;
    
    [Tooltip("Horizontal spacing between hands")]
    public float handSpacing = 0.4f;
    
    [Tooltip("Offset forward from wall surface to place hands")]
    public float wallSurfaceOffset = 0.05f;

    [Header("Blending Settings")]
    [Tooltip("Speed at which IK weight changes")]
    public float blendSpeed = 5f;
    
    [Tooltip("Minimum IK weight (0-1)")]
    [Range(0f, 1f)]
    public float minIKWeight = 0f;
    
    [Tooltip("Maximum IK weight (0-1)")]
    [Range(0f, 1f)]
    public float maxIKWeight = 1f;

    [Header("Raycast Settings")]
    [Tooltip("Layers that count as walls/obstacles")]
    public LayerMask wallLayers = ~0;
    
    [Tooltip("Number of raycasts to perform for better wall detection")]
    public int raycastCount = 3;

    [Header("Debug")]
    public bool showDebugRays = true;

    // Private variables
    private float currentLeftWeight = 0f;
    private float currentRightWeight = 0f;
    private bool isNearWall = false;
    private Vector3 wallNormal;
    private Vector3 wallHitPoint;
    private CharacterController characterController;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        
        // Validate setup
        ValidateSetup();
    }

    private void ValidateSetup()
    {
        bool hasErrors = false;

        if (leftHandIK == null)
        {
            Debug.LogError($"[{gameObject.name}] Left Hand IK Constraint not assigned!", this);
            hasErrors = true;
        }

        if (rightHandIK == null)
        {
            Debug.LogError($"[{gameObject.name}] Right Hand IK Constraint not assigned!", this);
            hasErrors = true;
        }

        if (leftHandTarget == null)
        {
            Debug.LogError($"[{gameObject.name}] Left Hand Target not assigned!", this);
            hasErrors = true;
        }

        if (rightHandTarget == null)
        {
            Debug.LogError($"[{gameObject.name}] Right Hand Target not assigned!", this);
            hasErrors = true;
        }

        if (!hasErrors)
        {
            Debug.Log($"<color=green>[{gameObject.name}] Wall Touch IK setup validated successfully!</color>");
        }
    }

    private void LateUpdate()
    {
        // Detect wall in front of character
        isNearWall = DetectWall(out wallHitPoint, out wallNormal);

        if (isNearWall)
        {
            // Calculate target IK weights based on distance to wall
            float distanceToWall = Vector3.Distance(transform.position, wallHitPoint);
            float targetWeight = CalculateIKWeight(distanceToWall);

            // Update hand positions on wall
            UpdateHandTargets(wallHitPoint, wallNormal);

            // Blend IK weights smoothly
            currentLeftWeight = Mathf.Lerp(currentLeftWeight, targetWeight, blendSpeed * Time.deltaTime);
            currentRightWeight = Mathf.Lerp(currentRightWeight, targetWeight, blendSpeed * Time.deltaTime);
        }
        else
        {
            // Smoothly disable IK when not near wall
            currentLeftWeight = Mathf.Lerp(currentLeftWeight, 0f, blendSpeed * Time.deltaTime);
            currentRightWeight = Mathf.Lerp(currentRightWeight, 0f, blendSpeed * Time.deltaTime);
        }

        // Apply weights to IK constraints
        ApplyIKWeights();
    }

    /// <summary>
    /// Detects if there's a wall in front of the character using multiple raycasts
    /// </summary>
    private bool DetectWall(out Vector3 hitPoint, out Vector3 normal)
    {
        hitPoint = Vector3.zero;
        normal = Vector3.zero;

        Vector3 origin = transform.position + Vector3.up * (characterController != null ? characterController.height * 0.5f : 1f);
        Vector3 forward = transform.forward;

        // Perform multiple raycasts at different heights for better detection
        for (int i = 0; i < raycastCount; i++)
        {
            float heightOffset = (i - raycastCount / 2) * 0.2f;
            Vector3 rayOrigin = origin + Vector3.up * heightOffset;

            if (Physics.Raycast(rayOrigin, forward, out RaycastHit hit, detectionDistance, wallLayers, QueryTriggerInteraction.Ignore))
            {
                hitPoint = hit.point;
                normal = hit.normal;

                if (showDebugRays)
                {
                    Debug.DrawRay(rayOrigin, forward * hit.distance, Color.green);
                }

                return true;
            }

            if (showDebugRays)
            {
                Debug.DrawRay(rayOrigin, forward * detectionDistance, Color.red);
            }
        }

        return false;
    }

    /// <summary>
    /// Calculates IK weight based on distance to wall
    /// </summary>
    private float CalculateIKWeight(float distance)
    {
        if (distance <= touchDistance)
        {
            return maxIKWeight;
        }
        else if (distance >= detectionDistance)
        {
            return minIKWeight;
        }
        else
        {
            // Linear interpolation between touch and detection distance
            float t = 1f - ((distance - touchDistance) / (detectionDistance - touchDistance));
            return Mathf.Lerp(minIKWeight, maxIKWeight, t);
        }
    }

    /// <summary>
    /// Updates the position of hand IK targets on the wall surface
    /// </summary>
    private void UpdateHandTargets(Vector3 hitPoint, Vector3 normal)
    {
        if (leftHandTarget == null || rightHandTarget == null) return;

        // Calculate the base position for hands (at character's chest height, projected onto wall)
        Vector3 characterCenter = transform.position + Vector3.up * (characterController != null ? characterController.height * 0.5f : 1f);
        Vector3 handBasePosition = hitPoint + normal * wallSurfaceOffset + Vector3.up * handHeightOffset;

        // Calculate right and left directions relative to wall normal
        Vector3 rightDir = Vector3.Cross(normal, Vector3.up).normalized;
        
        // If wall is too vertical, use character's right instead
        if (rightDir.magnitude < 0.1f)
        {
            rightDir = transform.right;
        }

        // Position left and right hands
        Vector3 leftPosition = handBasePosition - rightDir * handSpacing;
        Vector3 rightPosition = handBasePosition + rightDir * handSpacing;

        // Set target positions
        leftHandTarget.position = leftPosition;
        rightHandTarget.position = rightPosition;

        // Optional: Rotate targets to align with wall normal
        Quaternion targetRotation = Quaternion.LookRotation(-normal, Vector3.up);
        leftHandTarget.rotation = targetRotation;
        rightHandTarget.rotation = targetRotation;
    }

    /// <summary>
    /// Applies the calculated weights to the IK constraints
    /// </summary>
    private void ApplyIKWeights()
    {
        if (leftHandIK != null)
        {
            leftHandIK.weight = currentLeftWeight;
        }

        if (rightHandIK != null)
        {
            rightHandIK.weight = currentRightWeight;
        }
    }

    /// <summary>
    /// Visualize detection area and hand positions in editor
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Draw detection sphere
        Gizmos.color = isNearWall ? Color.green : Color.yellow;
        Vector3 origin = transform.position + Vector3.up * (characterController != null ? characterController.height * 0.5f : 1f);
        Gizmos.DrawWireSphere(origin + transform.forward * detectionDistance, 0.1f);

        // Draw hand target positions
        if (leftHandTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(leftHandTarget.position, 0.05f);
            Gizmos.DrawLine(transform.position, leftHandTarget.position);
        }

        if (rightHandTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(rightHandTarget.position, 0.05f);
            Gizmos.DrawLine(transform.position, rightHandTarget.position);
        }

        // Draw wall contact point
        if (isNearWall)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(wallHitPoint, 0.1f);
            Gizmos.DrawRay(wallHitPoint, wallNormal * 0.3f);
        }
    }

    #region Public API
    /// <summary>
    /// Manually enable/disable the wall touch IK system
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
        
        if (!enabled)
        {
            // Reset weights when disabled
            currentLeftWeight = 0f;
            currentRightWeight = 0f;
            ApplyIKWeights();
        }
    }

    /// <summary>
    /// Get current IK activation state
    /// </summary>
    public bool IsActive()
    {
        return isNearWall && (currentLeftWeight > 0.01f || currentRightWeight > 0.01f);
    }

    /// <summary>
    /// Force update IK weights immediately (useful for testing)
    /// </summary>
    public void ForceUpdateIK(float weight)
    {
        currentLeftWeight = weight;
        currentRightWeight = weight;
        ApplyIKWeights();
    }
    #endregion
}
