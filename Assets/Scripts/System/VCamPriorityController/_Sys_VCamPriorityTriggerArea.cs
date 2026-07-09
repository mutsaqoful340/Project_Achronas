using Unity.Cinemachine;
using UnityEngine;
using TMPro;
using Unity.AppUI.UI;
using UnityEngine.Events;

public class _Sys_VCamPriorityTriggerArea : MonoBehaviour
{
    private const int PlayerSlotCount = 2;

    [Header("Priority Controller")]
    public _Sys_VCamPriorityController priorityController;
    
    [Header("Camera for this Area")]
    public CinemachineVirtualCameraBase areaCinemachineCamera; // The camera to activate when both players are inside
    
    [Header("Settings")]
    [SerializeField] private string areaName = "TriggerArea"; // For debug logging
    // [Tooltip("Kalau ingin pindah section, check ini dan ")]
    // public bool isSwitchSection = false; // Optional setting to indicate if this area is a section switch

    [Header("Additional Events")]
    public UnityEvent onBothPlayersInside;
    public UnityEvent onPlayerExit;
    
    [Header("Debugging")]
    public TextMeshProUGUI debugText; // Optional UI text element for debugging purposes
    [SerializeField] private bool isAreaActive = false;
    [SerializeField] private Player_Components[] playerInside = new Player_Components[PlayerSlotCount];

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player_Components playerComponent = other.GetComponent<Player_Components>();
            if (playerComponent == null)
            {
                return;
            }

            if (IsPlayerAlreadyInside(playerComponent))
            {
                UpdateDebugState();
                return;
            }

            AddPlayer(playerComponent);
            UpdateDebugState();
            
            // Both players entered the trigger area
            if (GetPlayerCount() == PlayerSlotCount && !isAreaActive)
            {
                isAreaActive = true;
                
                if (priorityController != null && areaCinemachineCamera != null)
                {
                    priorityController.SetCameraActive(areaCinemachineCamera);
                    onBothPlayersInside?.Invoke();
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
            Player_Components playerComponent = other.GetComponent<Player_Components>();
            if (playerComponent == null)
            {
                return;
            }

            RemovePlayer(playerComponent);

            UpdateDebugState();
            onPlayerExit?.Invoke();
            
            // A player left, deactivate this area
            if (GetPlayerCount() < PlayerSlotCount && isAreaActive)
            {
                isAreaActive = false;
                Debug.Log($"<color=cyan>[{areaName}] Player left, deactivated area camera</color>");
            }
        }
    }

    private bool IsPlayerAlreadyInside(Player_Components playerComponent)
    {
        for (int i = 0; i < playerInside.Length; i++)
        {
            if (playerInside[i] == playerComponent)
            {
                return true;
            }
        }

        return false;
    }

    private int GetPlayerCount()
    {
        int count = 0;

        for (int i = 0; i < playerInside.Length; i++)
        {
            if (playerInside[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private void AddPlayer(Player_Components playerComponent)
    {
        for (int i = 0; i < playerInside.Length; i++)
        {
            if (playerInside[i] == null)
            {
                playerInside[i] = playerComponent;
                return;
            }
        }
    }

    private void RemovePlayer(Player_Components playerComponent)
    {
        for (int i = 0; i < playerInside.Length; i++)
        {
            if (playerInside[i] == playerComponent)
            {
                playerInside[i] = null;
                return;
            }
        }
    }

    private void UpdateDebugState()
    {
        if (debugText == null)
        {
            return;
        }

        string playerOneName = playerInside.Length > 0 && playerInside[0] != null ? playerInside[0].gameObject.name : "None";
        string playerTwoName = playerInside.Length > 1 && playerInside[1] != null ? playerInside[1].gameObject.name : "None";
        debugText.text = $"{areaName}\nInside: {GetPlayerCount()}\nSlot 1: {playerOneName}\nSlot 2: {playerTwoName}";
    }

    public void ActivatePriorityCamera()
    {
        if (priorityController != null && areaCinemachineCamera != null)
        {
            priorityController.SetCameraActive(areaCinemachineCamera);
            Debug.Log($"<color=cyan>[{areaName}] Priority camera activated via signal</color>");
        }
        else
        {
            if (priorityController == null)
                Debug.LogError($"<color=red>[{areaName}] priorityController is NULL!</color>");
            if (areaCinemachineCamera == null)
                Debug.LogError($"<color=red>[{areaName}] areaCinemachineCamera is NULL!</color>");
        }
    }

    public void ResetSplineDolly()
    {
        if (areaCinemachineCamera == null)
        {
            Debug.LogError($"<color=red>[{areaName}] areaCinemachineCamera is NULL!</color>");
            return;
        }

        var splineDolly = areaCinemachineCamera.GetComponent<CinemachineSplineDolly>();
        if (splineDolly != null)
        {
            splineDolly.CameraPosition = 0f;
            Debug.Log($"<color=cyan>[{areaName}] Spline Dolly reset to position 0</color>");
        }
        else
        {
            Debug.LogError($"<color=red>[{areaName}] CinemachineSplineDolly component not found!</color>");
        }
    }
}