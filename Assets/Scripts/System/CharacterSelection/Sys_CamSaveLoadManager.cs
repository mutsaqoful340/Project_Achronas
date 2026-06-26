using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class Sys_CamSaveLoadManager : MonoBehaviour
{
    [System.Serializable]
    public class RoomCameraBinding
    {
        public string roomID;
        public CinemachineVirtualCameraBase virtualCamera;
    }

    [Header("Room Camera Mappings")]
    public List<RoomCameraBinding> roomCameraBindings = new List<RoomCameraBinding>();

    [Header("Fallback Camera List")]
    public CinemachineVirtualCameraBase[] virtualCameras;

    [Header("Priority Settings")]
    public int activePriority = 15;
    public int inactivePriority = 0;

    private bool isSubscribedToLoadEvent = false;

    private void Start()
    {
        SubscribeToLoadEvent();
    }

    private void OnEnable()
    {
        SubscribeToLoadEvent();
    }

    private void OnDisable()
    {
        UnsubscribeFromLoadEvent();
    }

    private void SubscribeToLoadEvent()
    {
        if (isSubscribedToLoadEvent)
            return;

        if (SaveManager.Instance == null)
            return;

        SaveManager.Instance.OnLoadSuccessEvent.AddListener(HandleLoadSuccessFromUnityEvent);
        isSubscribedToLoadEvent = true;
    }

    private void UnsubscribeFromLoadEvent()
    {
        if (!isSubscribedToLoadEvent)
            return;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnLoadSuccessEvent.RemoveListener(HandleLoadSuccessFromUnityEvent);
        }

        isSubscribedToLoadEvent = false;
    }

    public void HandleLoadSuccessFromUnityEvent()
    {
        ActivateCameraForLoadedRoom();
    }

    public void ActivateCameraForLoadedRoom()
    {
        string roomID = PlayerPrefs.GetString("lastRoomID", "");
        ActivateCameraForRoom(roomID);
    }

    public void ActivateCameraForRoom(string roomID)
    {
        ResetAllCameraPriorities();

        if (string.IsNullOrEmpty(roomID))
        {
            Debug.LogWarning("[Sys_CamSaveLoadManager] Cannot activate room camera because roomID is empty.");
            return;
        }

        for (int i = 0; i < roomCameraBindings.Count; i++)
        {
            RoomCameraBinding binding = roomCameraBindings[i];
            if (binding == null || binding.virtualCamera == null)
                continue;

            if (binding.roomID == roomID)
            {
                binding.virtualCamera.Priority = activePriority;
                Debug.Log($"[Sys_CamSaveLoadManager] Activated camera '{binding.virtualCamera.name}' for room '{roomID}' with priority {activePriority}.");
                return;
            }
        }

        Debug.LogWarning($"[Sys_CamSaveLoadManager] No camera binding found for room '{roomID}'.");
    }

    public void ResetAllCameraPriorities()
    {
        for (int i = 0; i < roomCameraBindings.Count; i++)
        {
            RoomCameraBinding binding = roomCameraBindings[i];
            if (binding != null && binding.virtualCamera != null)
            {
                binding.virtualCamera.Priority = inactivePriority;
            }
        }

        if (virtualCameras == null)
            return;

        for (int i = 0; i < virtualCameras.Length; i++)
        {
            if (virtualCameras[i] != null)
            {
                virtualCameras[i].Priority = inactivePriority;
            }
        }
    }

    public void SetActiveCamera(int camIndex)
    {
        ResetAllCameraPriorities();

        if (virtualCameras == null || camIndex < 0 || camIndex >= virtualCameras.Length)
        {
            Debug.LogWarning($"[Sys_CamSaveLoadManager] Invalid camera index: {camIndex}");
            return;
        }

        CinemachineVirtualCameraBase targetCamera = virtualCameras[camIndex];
        if (targetCamera == null)
        {
            Debug.LogWarning($"[Sys_CamSaveLoadManager] Camera at index {camIndex} is null.");
            return;
        }

        targetCamera.Priority = activePriority;
        Debug.Log($"[Sys_CamSaveLoadManager] Activated camera '{targetCamera.name}' with priority {activePriority}.");
    }
}
