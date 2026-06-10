using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class UniversalTriggerCollider : MonoBehaviour
{
    public bool useSpecificName = false;
    public string specificName = "Player";
    public UnityEvent onTriggerEnterEvent;

    void OnTriggerEnter(Collider other)
    {
        if (useSpecificName)
        {
            if (other.name == specificName && other.CompareTag("Player"))
            {
                Debug.Log($"<color=green>Trigger entered by {specificName}: {other.name}</color>");
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
                Debug.Log($"<color=green>Trigger entered by Player: {other.name}</color>");
                onTriggerEnterEvent.Invoke();
            }
        }
    }
}
