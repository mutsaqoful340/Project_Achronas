using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class UniversalTriggerCollider : MonoBehaviour
{
    public bool useSpecificName = false;
    public string specificName = "Player";
    public UnityEvent onTriggerEnterEvent;

    private int playerCount;

    void OnTriggerEnter(Collider other)
    {
        if (useSpecificName)
        {
            if (other.name == specificName && other.CompareTag("Player"))
            {
                playerCount++;
                Debug.Log($"<color=green>Trigger entered by {specificName}: {other.name} (Count: {playerCount})</color>");
                onTriggerEnterEvent.Invoke();
            }
            else
            {
                return;
            }
        }
        else
        {
            if (other.CompareTag("Player"))
            {
                playerCount++;
                Debug.Log($"<color=green>Trigger entered by Player: {other.name} (Count: {playerCount})</color>");
                onTriggerEnterEvent.Invoke();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (useSpecificName)
        {
            if (other.name == specificName && other.CompareTag("Player"))
            {
                playerCount--;
                Debug.Log($"<color=yellow>Trigger exited by {specificName}: {other.name} (Count: {playerCount})</color>");
            }
        }
        else
        {
            if (other.CompareTag("Player"))
            {
                playerCount--;
                Debug.Log($"<color=yellow>Trigger exited by Player: {other.name} (Count: {playerCount})</color>");
            }
        }
    }
}
