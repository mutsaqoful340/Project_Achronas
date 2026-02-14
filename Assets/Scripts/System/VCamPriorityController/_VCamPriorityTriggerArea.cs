using UnityEngine;

public class _VCamPriorityTriggerArea : MonoBehaviour
{
    public _VCamPriorityController priorityController;
    public int playerInside;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside++;
            if (priorityController != null)
            {
                HandleCameraChange();
            }
            else
            {
                Debug.LogError("<color=red>_VCamPriorityTriggerArea: priorityController is NULL! Please assign it in inspector!</color>");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside--;
            if (priorityController != null)
            {
                HandleCameraChange();
            }
            else
            {
                Debug.LogError("<color=red>_VCamPriorityTriggerArea: priorityController is NULL! Please assign it in inspector!</color>");
            }
        }
    }

    public void HandleCameraChange()
    {
        if (playerInside == 2)
        {
            priorityController.SetHighPriority();
            Debug.Log($"<color=cyan>Both players inside trigger, set camera to high priority</color>");
        }
        else if (playerInside == 0)
        {
            priorityController.SetLowPriority();
            Debug.Log($"<color=cyan>Not both players inside trigger, set camera to low priority</color>");
        }
    }
}
