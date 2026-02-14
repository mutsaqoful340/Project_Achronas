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
    public void SetHighPriority() 
    { 
        SetPriority(10); 
        Debug.Log($"<color=cyan>Set {virtualCamera.name} to HIGH priority</color>");
    }
    public void SetLowPriority() 
    { 
        SetPriority(0); 
        Debug.Log($"<color=cyan>Set {virtualCamera.name} to LOW priority</color>");
    }
    public void SetMediumPriority() 
    { 
        SetPriority(5); 
        Debug.Log($"<color=cyan>Set {virtualCamera.name} to MEDIUM priority</color>");
    }
}
