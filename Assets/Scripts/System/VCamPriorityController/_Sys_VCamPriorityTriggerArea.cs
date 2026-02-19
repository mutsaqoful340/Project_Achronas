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
    [SerializeField] private float cameraSwitchCooldown = 1f; // Cooldown time before camera can switch again
    [SerializeField] private static float globalCooldownTimer = 0f;

    public TextMeshProUGUI debugText; // Optional UI text element for debugging purposes
    
    private int playersInside = 0;
    private bool isAreaActive = false;
    
    // Static cooldown shared across all trigger areas


    private void Update()
    {
        // Decrement global cooldown timer
        if (globalCooldownTimer > 0f)
        {
            globalCooldownTimer -= Time.deltaTime;
        }
    }

    private void OnGUI()
    {
        // Display cooldown timer on screen
        GUI.Label(new Rect(10, 10, 300, 30), $"Camera Cooldown: {globalCooldownTimer:F2}s");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersInside++;
            
            // Both players entered the trigger area and global cooldown has expired
            if (playersInside == 2 && !isAreaActive && globalCooldownTimer <= 0f)
            {
                isAreaActive = true;
                globalCooldownTimer = cameraSwitchCooldown; // Start global cooldown
                
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
