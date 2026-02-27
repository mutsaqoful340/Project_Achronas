using UnityEngine;
using UnityEngine.Events;

public class _GP_LeverManager : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private GameObject playerLever1;
    [SerializeField] private GameObject playerLever2;

    [Header("Events")]
    public UnityEvent onBothLeversActivated;
    
    public void SetPlayerLever(GameObject player)
    {
        if (playerLever1 == null)
        {
            playerLever1 = player;
        }
        else if (playerLever2 == null)
        {
            playerLever2 = player;
        }
        CheckBothLeversActivated();
    }

    private void CheckBothLeversActivated()
    {
        if (playerLever1 != null && playerLever2 != null)
        {
            // Both levers are activated, trigger the desired event
            Debug.Log("Both levers activated! Triggering event...");
            onBothLeversActivated?.Invoke();
        }
    }
}
