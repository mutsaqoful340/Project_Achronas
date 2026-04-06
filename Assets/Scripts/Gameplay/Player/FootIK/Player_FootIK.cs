using UnityEngine;

/// <summary>
/// Handles foot IK to make feet adapt to uneven terrain using Unity's built-in Animator IK.
/// Requires a Humanoid rigged character with an Animator component.
/// </summary>
[RequireComponent(typeof(Animator))]
public class Player_FootIK : MonoBehaviour
{
    [Header("IK Settings")]
    [Tooltip("Enable/disable foot IK system")]
    public bool enableFootIK = true;
    
    [Tooltip("Master weight for all IK (0-1). Use to fade IK in/out.")]
    [Range(0f, 1f)]
    public float ikWeight = 1f;

    [Header("Raycast Settings")]
    [Tooltip("How far above the foot to start raycasting")]
    public float raycastUpOffset = 0.5f;
    
    [Tooltip("Maximum distance to raycast down for ground")]
    public float raycastDownDistance = 1.5f;
    
    [Tooltip("Layers that count as ground")]
    public LayerMask groundLayers = ~0;
    
    [Tooltip("Extra height offset applied to foot (useful for avoiding ground clipping)")]
    public float footHeightOffset = 0.0f;

    [Header("Pelvis Adjustment")]
    [Tooltip("Enable automatic pelvis/body height adjustment")]
    public bool adjustPelvisHeight = true;
    
    [Tooltip("How much to adjust pelvis (0-1). Lower values = less adjustment.")]
    [Range(0f, 1f)]
    public float pelvisAdjustmentWeight = 0.5f;
    
    [Tooltip("Minimum pelvis offset to prevent extreme crouching")]
    public float minPelvisOffset = -0.3f;
    
    [Tooltip("Maximum pelvis offset to prevent extreme stretching")]
    public float maxPelvisOffset = 0.1f;

    [Header("Smoothing")]
    [Tooltip("Smooth IK position changes over time")]
    public bool smoothIK = true;
    
    [Tooltip("Speed of IK position smoothing")]
    public float smoothSpeed = 10f;

    [Header("Foot Rotation")]
    [Tooltip("Enable foot rotation to match ground angle")]
    public bool enableFootRotation = true;
    
    [Tooltip("Weight for foot rotation (0-1)")]
    [Range(0f, 1f)]
    public float footRotationWeight = 1f;
    
    [Tooltip("Clamp foot rotation angle (prevents extreme angles)")]
    public float maxFootRotationAngle = 45f;

    [Header("Debug")]
    public bool showDebugRays = true;
    public bool showDebugInfo = false;

    // Private variables
    private Animator animator;
    private Vector3 leftFootPosition;
    private Vector3 rightFootPosition;
    private Quaternion leftFootRotation;
    private Quaternion rightFootRotation;
    private float lastPelvisOffset;
    private float leftFootIKWeight;
    private float rightFootIKWeight;

    private void Start()
    {
        animator = GetComponent<Animator>();
        
        // Validate setup
        if (!animator)
        {
            Debug.LogError($"[{gameObject.name}] Animator component required for Foot IK!", this);
            enabled = false;
            return;
        }

        if (!animator.isHuman)
        {
            Debug.LogError($"[{gameObject.name}] Avatar must be Humanoid for built-in IK to work!", this);
            Debug.LogError("Select your character model → Inspector → Rig tab → Set Animation Type to 'Humanoid'", this);
            enabled = false;
            return;
        }

        Debug.Log($"<color=green>[{gameObject.name}] Foot IK initialized successfully!</color>");
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!animator || !enableFootIK)
            return;

        // Process both feet
        ProcessFootIK(AvatarIKGoal.LeftFoot, ref leftFootPosition, ref leftFootRotation, ref leftFootIKWeight);
        ProcessFootIK(AvatarIKGoal.RightFoot, ref rightFootPosition, ref rightFootRotation, ref rightFootIKWeight);

        // Adjust pelvis/body height to prevent leg over-extension
        if (adjustPelvisHeight)
        {
            AdjustPelvisHeight();
        }

        // Debug info
        if (showDebugInfo)
        {
            Debug.Log($"L_IK: {leftFootIKWeight:F2} | R_IK: {rightFootIKWeight:F2} | Pelvis: {lastPelvisOffset:F3}");
        }
    }

    /// <summary>
    /// Process IK for a single foot
    /// </summary>
    private void ProcessFootIK(AvatarIKGoal foot, ref Vector3 footPosition, ref Quaternion footRotation, ref float footIKWeight)
    {
        // Get the foot bone's current position from animation
        Vector3 footAnimatedPosition = animator.GetIKPosition(foot);
        Quaternion footAnimatedRotation = animator.GetIKRotation(foot);

        // Raycast down from above the foot to find ground
        Vector3 rayStart = footAnimatedPosition + Vector3.up * raycastUpOffset;
        Ray ray = new Ray(rayStart, Vector3.down);
        
        if (Physics.Raycast(ray, out RaycastHit hit, raycastUpOffset + raycastDownDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            // Ground detected - calculate foot IK position
            Vector3 targetPosition = hit.point + Vector3.up * footHeightOffset;
            Quaternion targetRotation = CalculateFootRotation(hit.normal, foot);

            // Smooth IK if enabled
            if (smoothIK)
            {
                footPosition = Vector3.Lerp(footPosition, targetPosition, smoothSpeed * Time.deltaTime);
                footRotation = Quaternion.Slerp(footRotation, targetRotation, smoothSpeed * Time.deltaTime);
                footIKWeight = Mathf.Lerp(footIKWeight, ikWeight, smoothSpeed * Time.deltaTime);
            }
            else
            {
                footPosition = targetPosition;
                footRotation = targetRotation;
                footIKWeight = ikWeight;
            }

            // Apply IK position
            animator.SetIKPositionWeight(foot, footIKWeight);
            animator.SetIKPosition(foot, footPosition);

            // Apply IK rotation if enabled
            if (enableFootRotation)
            {
                animator.SetIKRotationWeight(foot, footRotationWeight * footIKWeight);
                animator.SetIKRotation(foot, footRotation);
            }

            // Debug visualization
            if (showDebugRays)
            {
                Debug.DrawLine(rayStart, hit.point, Color.green);
                Debug.DrawRay(hit.point, hit.normal * 0.3f, Color.blue);
            }
        }
        else
        {
            // No ground detected - fade out IK weight
            if (smoothIK)
            {
                footIKWeight = Mathf.Lerp(footIKWeight, 0f, smoothSpeed * Time.deltaTime);
            }
            else
            {
                footIKWeight = 0f;
            }

            animator.SetIKPositionWeight(foot, footIKWeight);
            animator.SetIKRotationWeight(foot, 0f);

            // Debug visualization
            if (showDebugRays)
            {
                Debug.DrawRay(rayStart, Vector3.down * (raycastUpOffset + raycastDownDistance), Color.red);
            }
        }
    }

    /// <summary>
    /// Calculate foot rotation to align with ground normal
    /// </summary>
    private Quaternion CalculateFootRotation(Vector3 groundNormal, AvatarIKGoal foot)
    {
        // Get character's forward direction
        Vector3 forward = transform.forward;
        
        // Project forward direction onto ground plane
        Vector3 projectedForward = Vector3.ProjectOnPlane(forward, groundNormal).normalized;
        
        // Calculate target rotation
        Quaternion targetRotation = Quaternion.LookRotation(projectedForward, groundNormal);
        
        // Clamp rotation to prevent extreme angles
        Quaternion currentRotation = animator.GetIKRotation(foot);
        float angle = Quaternion.Angle(currentRotation, targetRotation);
        
        if (angle > maxFootRotationAngle)
        {
            targetRotation = Quaternion.Slerp(currentRotation, targetRotation, maxFootRotationAngle / angle);
        }
        
        return targetRotation;
    }

    /// <summary>
    /// Adjust pelvis/body height to prevent legs from over-extending or compressing too much
    /// </summary>
    private void AdjustPelvisHeight()
    {
        // Get current foot positions
        Vector3 leftFootPos = animator.GetIKPosition(AvatarIKGoal.LeftFoot);
        Vector3 rightFootPos = animator.GetIKPosition(AvatarIKGoal.RightFoot);
        
        // Get pelvis position
        Vector3 pelvisPos = animator.bodyPosition;
        
        // Calculate how much each foot has moved from its animated position
        float leftFootHeight = leftFootPos.y;
        float rightFootHeight = rightFootPos.y;
        
        // Find the lowest foot (the one that's most planted)
        float lowestFootHeight = Mathf.Min(leftFootHeight, rightFootHeight);
        
        // Calculate pelvis offset needed
        // If feet are lower than expected, lower the pelvis
        float targetPelvisOffset = (lowestFootHeight - pelvisPos.y) * pelvisAdjustmentWeight;
        
        // Clamp pelvis offset to prevent extreme body positions
        targetPelvisOffset = Mathf.Clamp(targetPelvisOffset, minPelvisOffset, maxPelvisOffset);
        
        // Smooth pelvis movement
        if (smoothIK)
        {
            lastPelvisOffset = Mathf.Lerp(lastPelvisOffset, targetPelvisOffset, smoothSpeed * Time.deltaTime);
        }
        else
        {
            lastPelvisOffset = targetPelvisOffset;
        }
        
        // Apply pelvis offset
        animator.bodyPosition += Vector3.up * lastPelvisOffset;
    }

    /// <summary>
    /// Draw gizmos for debugging
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !animator || !enableFootIK)
            return;

        // Draw foot IK positions
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(leftFootPosition, 0.05f);
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(rightFootPosition, 0.05f);
        
        // Draw raycast start positions
        if (animator.isHuman)
        {
            Vector3 leftFootAnimPos = animator.GetIKPosition(AvatarIKGoal.LeftFoot);
            Vector3 rightFootAnimPos = animator.GetIKPosition(AvatarIKGoal.RightFoot);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(leftFootAnimPos + Vector3.up * raycastUpOffset, 0.03f);
            Gizmos.DrawWireSphere(rightFootAnimPos + Vector3.up * raycastUpOffset, 0.03f);
        }
    }

    #region Public API
    /// <summary>
    /// Enable or disable the foot IK system
    /// </summary>
    public void SetFootIKEnabled(bool enabled)
    {
        enableFootIK = enabled;
        
        if (!enabled && animator)
        {
            // Reset IK weights when disabled
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
        }
    }

    /// <summary>
    /// Set the master IK weight
    /// </summary>
    public void SetIKWeight(float weight)
    {
        ikWeight = Mathf.Clamp01(weight);
    }

    /// <summary>
    /// Check if foot IK is currently active
    /// </summary>
    public bool IsIKActive()
    {
        return enableFootIK && (leftFootIKWeight > 0.01f || rightFootIKWeight > 0.01f);
    }

    /// <summary>
    /// Get the current IK weight for a specific foot
    /// </summary>
    public float GetFootIKWeight(AvatarIKGoal foot)
    {
        return foot == AvatarIKGoal.LeftFoot ? leftFootIKWeight : rightFootIKWeight;
    }
    #endregion
}
