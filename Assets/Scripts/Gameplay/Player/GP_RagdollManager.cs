using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages ragdoll physics for a humanoid character.
/// Controls when the character transitions between animated and ragdoll states.
/// </summary>
public class GP_RagdollManager : MonoBehaviour
{
    [Header("Ragdoll Settings")]
    [Tooltip("Enable ragdoll on start")]
    [SerializeField] private bool startAsRagdoll = false;
    
    [Tooltip("Time in seconds before auto-recovery from ragdoll")]
    [SerializeField] private float autoRecoveryTime = 3f;
    
    [Tooltip("Enable auto-recovery")]
    [SerializeField] private bool enableAutoRecovery = true;

    [Header("Transition Settings")]
    [Tooltip("How smoothly to blend back to standing position")]
    [SerializeField] private float recoveryBlendTime = 0.5f;
    
    [Tooltip("Minimum velocity to maintain ragdoll")]
    [SerializeField] private float minimumVelocityThreshold = 0.1f;

    [Header("Component References")]
    [Tooltip("The Animator component controlling this character")]
    [SerializeField] private Animator animator;
    
    [Tooltip("Optional: Character Controller to disable during ragdoll")]
    [SerializeField] private CharacterController characterController;
    
    [Tooltip("Optional: NavMeshAgent to disable during ragdoll")]
    [SerializeField] private UnityEngine.AI.NavMeshAgent navMeshAgent;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // Internal state
    private bool isRagdoll = false;
    private bool ragdollOverride = false; // Prevents state handlers from interfering with ragdoll
    private float ragdollStartTime;
    private List<RagdollBone> ragdollBones = new List<RagdollBone>();
    
    // Component state cache
    private bool wasCharacterControllerEnabled;
    private bool wasNavMeshAgentEnabled;
    private bool wasAnimatorEnabled;

    /// <summary>
    /// Stores information about each bone in the ragdoll
    /// </summary>
    private class RagdollBone
    {
        public Transform transform;
        public Rigidbody rigidbody;
        public Collider collider;
        public Vector3 storedPosition;
        public Quaternion storedRotation;

        public void StoreState()
        {
            storedPosition = transform.localPosition;
            storedRotation = transform.localRotation;
        }

        public void SetKinematic(bool kinematic)
        {
            if (rigidbody != null)
            {
                rigidbody.isKinematic = kinematic;
            }
        }

        public void SetColliderEnabled(bool enabled)
        {
            if (collider != null)
            {
                collider.enabled = enabled;
            }
        }

        public float GetVelocity()
        {
            return rigidbody != null ? rigidbody.linearVelocity.magnitude : 0f;
        }
    }

    #region Unity Lifecycle

    private void Awake()
    {
        // Auto-find animator if not assigned (check children too)
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // Auto-find character controller if not assigned
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        // Auto-find nav mesh agent if not assigned
        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        }

        // Initialize ragdoll components
        InitializeRagdoll();

        // Set initial state
        if (startAsRagdoll)
        {
            EnableRagdoll();
        }
        else
        {
            // Manually disable ragdoll components at startup
            SetRagdollPhysicsState(false);
        }
    }

    private void Update()
    {
        // Don't process auto-recovery if ragdoll is being externally controlled (override active)
        if (isRagdoll && enableAutoRecovery && !ragdollOverride)
        {
            // Check if enough time has passed for auto-recovery
            if (Time.time - ragdollStartTime >= autoRecoveryTime)
            {
                // Check if all bones are nearly stationary
                if (IsRagdollStationary())
                {
                    DisableRagdoll();
                }
            }
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Finds and caches all rigidbodies and colliders in the ragdoll
    /// </summary>
    private void InitializeRagdoll()
    {
        ragdollBones.Clear();

        // Find all rigidbodies in children (ragdoll bones)
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in rigidbodies)
        {
            // Skip the root rigidbody if it exists
            if (rb.transform == transform)
                continue;

            RagdollBone bone = new RagdollBone
            {
                transform = rb.transform,
                rigidbody = rb,
                collider = rb.GetComponent<Collider>()
            };

            ragdollBones.Add(bone);
        }

        if (showDebugInfo)
        {
            Debug.Log($"[GP_RagdollManager] Initialized {ragdollBones.Count} ragdoll bones");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Activates ragdoll physics
    /// </summary>
    public void EnableRagdoll()
    {
        if (isRagdoll) return;

        isRagdoll = true;
        ragdollOverride = true; // Set override flag to prevent state handlers from interfering
        ragdollStartTime = Time.time;

        // Disable animator
        if (animator != null)
        {
            wasAnimatorEnabled = animator.enabled;
            animator.enabled = false;
        }

        // Disable character controller
        if (characterController != null)
        {
            wasCharacterControllerEnabled = characterController.enabled;
            characterController.enabled = false;
        }

        // Disable nav mesh agent explicitly
        if (navMeshAgent != null)
        {
            wasNavMeshAgentEnabled = navMeshAgent.enabled;
            navMeshAgent.enabled = false;
            Debug.Log("[GP_RagdollManager] NavMeshAgent disabled for ragdoll");
        }

        // Enable ragdoll physics
        SetRagdollPhysicsState(true);

        if (showDebugInfo)
        {
            Debug.Log("[GP_RagdollManager] Ragdoll enabled with override flag set");
        }
    }

    /// <summary>
    /// Deactivates ragdoll physics and returns to animated state
    /// </summary>
    public void DisableRagdoll()
    {
        if (!isRagdoll) return;

        isRagdoll = false;
        ragdollOverride = false; // Clear override flag to allow state handlers to resume control

        // Disable ragdoll physics
        SetRagdollPhysicsState(false);

        // Re-enable animator
        if (animator != null)
        {
            animator.enabled = wasAnimatorEnabled;
        }

        // Re-enable character controller
        if (characterController != null)
        {
            characterController.enabled = wasCharacterControllerEnabled;
        }

        // Re-enable nav mesh agent
        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = wasNavMeshAgentEnabled;
        }

        if (showDebugInfo)
        {
            Debug.Log("[GP_RagdollManager] Ragdoll disabled, override flag cleared");
        }
    }

    /// <summary>
    /// Toggles ragdoll state
    /// </summary>
    public void ToggleRagdoll()
    {
        if (isRagdoll)
        {
            DisableRagdoll();
        }
        else
        {
            EnableRagdoll();
        }
    }

    /// <summary>
    /// Enables ragdoll and applies a force to a specific bone
    /// </summary>
    /// <param name="boneName">Name of the bone to apply force to</param>
    /// <param name="force">Force vector to apply</param>
    /// <param name="forceMode">Type of force to apply</param>
    public void EnableRagdollWithForce(string boneName, Vector3 force, ForceMode forceMode = ForceMode.Impulse)
    {
        EnableRagdoll();

        RagdollBone bone = ragdollBones.Find(b => b.transform.name == boneName);
        if (bone != null && bone.rigidbody != null)
        {
            bone.rigidbody.AddForce(force, forceMode);
        }
    }

    /// <summary>
    /// Enables ragdoll and applies a force to all bones
    /// </summary>
    /// <param name="force">Force vector to apply</param>
    /// <param name="forceMode">Type of force to apply</param>
    public void EnableRagdollWithExplosion(Vector3 explosionPosition, float explosionForce, float explosionRadius)
    {
        EnableRagdoll();

        foreach (RagdollBone bone in ragdollBones)
        {
            if (bone.rigidbody != null)
            {
                bone.rigidbody.AddExplosionForce(explosionForce, explosionPosition, explosionRadius);
            }
        }
    }

    /// <summary>
    /// Gets the current ragdoll state
    /// </summary>
    public bool IsRagdollActive()
    {
        return isRagdoll;
    }

    /// <summary>
    /// Checks if ragdoll override is active (prevents state handlers from interfering)
    /// </summary>
    public bool IsRagdollOverrideActive()
    {
        return ragdollOverride;
    }

    /// <summary>
    /// Sets the auto-recovery time
    /// </summary>
    public void SetAutoRecoveryTime(float time)
    {
        autoRecoveryTime = time;
    }

    /// <summary>
    /// Enables or disables auto-recovery
    /// </summary>
    public void SetAutoRecoveryEnabled(bool enabled)
    {
        enableAutoRecovery = enabled;
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Sets the physics state of all ragdoll bones
    /// </summary>
    private void SetRagdollPhysicsState(bool enablePhysics)
    {
        foreach (RagdollBone bone in ragdollBones)
        {
            bone.SetKinematic(!enablePhysics);
            bone.SetColliderEnabled(enablePhysics);
        }
    }

    /// <summary>
    /// Checks if the ragdoll is nearly stationary
    /// </summary>
    private bool IsRagdollStationary()
    {
        foreach (RagdollBone bone in ragdollBones)
        {
            if (bone.GetVelocity() > minimumVelocityThreshold)
            {
                return false;
            }
        }
        return true;
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!showDebugInfo || !isRagdoll) return;

        // Draw velocity vectors for each bone
        foreach (RagdollBone bone in ragdollBones)
        {
            if (bone.rigidbody != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(bone.transform.position, 
                    bone.transform.position + bone.rigidbody.linearVelocity);
            }
        }
    }

    #endregion
}
