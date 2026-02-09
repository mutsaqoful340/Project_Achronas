using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class _OnTriggerTester : MonoBehaviour
{
    public UnityEvent onTriggerEnterEvent;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"<color=green>Trigger entered by Player: {other.name}</color>");
            onTriggerEnterEvent.Invoke();
        }
    }
}
