using Unity.Cinemachine;
using UnityEngine;

public class _Sys_VCamPriorityController : MonoBehaviour
{
    [Header("Priority Values")]
    [SerializeField] private int highPriority = 10;
    [SerializeField] private int lowPriority = 0;

    private CinemachineVirtualCameraBase currentActiveCamera;

    /// <summary>
    /// Switch the active camera. Deactivates the previous camera and activates the new one.
    /// </summary>
    public void SetCameraActive(CinemachineVirtualCameraBase newCamera)
    {
        if (newCamera == null)
        {
            Debug.LogError("<color=red>SetCameraActive: newCamera is NULL!</color>");
            return;
        }

        // Deactivate the old camera
        if (currentActiveCamera != null && currentActiveCamera != newCamera)
        {
            currentActiveCamera.Priority = lowPriority;
            Debug.Log($"<color=orange>✗ Deactivated: {currentActiveCamera.name}</color>");
        }

        // Activate the new camera
        currentActiveCamera = newCamera;
        currentActiveCamera.Priority = highPriority;
        Debug.Log($"<color=green>✓ Activated: {currentActiveCamera.name}</color>");
    }

    /// <summary>
    /// Get the currently active camera.
    /// </summary>
    public CinemachineVirtualCameraBase GetCurrentCamera() => currentActiveCamera;

    /// <summary>
    /// Get the name of the currently active camera.
    /// </summary>
    public string GetCurrentCameraName()
    {
        return currentActiveCamera != null ? currentActiveCamera.name : "None";
    }
}
