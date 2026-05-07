using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class GP_UniversalInteraction : MonoBehaviour
{
    public Player_Components playerComponents;
    public InputActions input;
    public UnityEvent onInteract;
    
    private bool isPlayerInRange = false;

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerComponents = other.GetComponent<Player_Components>();
            if (playerComponents != null && input != null)
            {
                isPlayerInRange = true;
                input.Player.Interact.performed += OnInteractInput;
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && isPlayerInRange)
        {
            isPlayerInRange = false;
            if (input != null)
            {
                input.Player.Interact.performed -= OnInteractInput;
            }
            playerComponents = null;
        }
    }

    private void OnInteractInput(InputAction.CallbackContext ctx)
    {
        Interact();
    }

    private void Interact()
    {
        if (playerComponents != null)
        {
            Debug.Log("Interacted with " + gameObject.name);
            onInteract?.Invoke();
        }
    }
}
