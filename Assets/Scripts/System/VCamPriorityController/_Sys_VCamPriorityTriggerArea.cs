using Unity.Cinemachine;
using UnityEngine;
using TMPro;
using Unity.AppUI.UI;

public class _Sys_VCamPriorityTriggerArea : MonoBehaviour
{
    [Header("Priority Controller")]
    public _Sys_VCamPriorityController priorityController;
    
    [Header("Camera for this Area")]
    public CinemachineVirtualCameraBase areaCinemachineCamera; // The camera to activate when both players are inside
    
    [Header("Settings")]
    [SerializeField] private string areaName = "TriggerArea"; // For debug logging
    [Tooltip("Kalau ingin pindah section, check ini dan ")]
    public bool isSwitchSection = false; // Optional setting to indicate if this area is a section switch

    public TextMeshProUGUI debugText; // Optional UI text element for debugging purposes
    
    private int playersInside = 0;
    private bool isAreaActive = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersInside++;
            
            // Both players entered the trigger area
            if (playersInside == 2 && !isAreaActive)
            {
                isAreaActive = true;
                
                if (priorityController != null && areaCinemachineCamera != null)
                {
                    priorityController.SetCameraActive(areaCinemachineCamera);
                    Debug.Log($"<color=cyan>[{areaName}] Both players inside, activated camera</color>");
                }
                else
                {
                    if (priorityController == null)
                        Debug.LogError($"<color=red>[{areaName}] priorityController is NULL! Please assign it in inspector!</color>");
                    if (areaCinemachineCamera == null)
                        Debug.LogError($"<color=red>[{areaName}] areaCinemachineCamera is NULL! Please assign it in inspector!</color>");
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersInside--;
            
            // A player left, deactivate this area
            if (playersInside < 2 && isAreaActive)
            {
                isAreaActive = false;
                Debug.Log($"<color=cyan>[{areaName}] Player left, deactivated area camera</color>");
            }
        }
    }
}
