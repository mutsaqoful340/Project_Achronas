using Unity.Cinemachine;
using UnityEngine;

public class _VCamPriorityController : MonoBehaviour
{
    public CinemachineVirtualCameraBase virtualCamera;

    // Call this from UnityEvents with a specific priority value
    public void SetPriority(int priority)
    {
        if (virtualCamera != null)
        {
            virtualCamera.Priority = priority;
        }
    }

    // Convenience methods for common priorities
    public void SetHighPriority() => SetPriority(10);
    public void SetLowPriority() => SetPriority(0);
    public void SetMediumPriority() => SetPriority(5);
}
