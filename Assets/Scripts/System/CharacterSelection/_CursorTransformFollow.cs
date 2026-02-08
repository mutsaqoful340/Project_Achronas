using UnityEngine;

public class _CursorTransformFollow : MonoBehaviour
{
    [Header("Transform to Follow")]
    [Tooltip("The transform that the cursor will follow.")]
    public Transform targetTransform;
    
    [Header("Movement Settings")]
    [Tooltip("Time it takes to reach the target. Lower = faster, higher = smoother.")]
    [Range(0.01f, 1f)]
    public float smoothTime = 0.15f;
    
    private Vector3 velocity = Vector3.zero;

    private void Update()
    {
        if (targetTransform != null)
        {
            // Smoothly move towards the target position
            transform.position = Vector3.SmoothDamp(
                transform.position, 
                targetTransform.position, 
                ref velocity, 
                smoothTime
            );
        }
    }
}
