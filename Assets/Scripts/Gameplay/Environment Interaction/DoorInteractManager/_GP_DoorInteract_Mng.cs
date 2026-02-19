using Unity.VisualScripting;
using UnityEngine;

public class _GP_DoorInteract_Mng : MonoBehaviour
{
    [Header("Animator Reference")]
    [Tooltip("Animator yang mengontrol animasi pintu.")]
    public Animator doorAnimator;

    [Header("Player Componments References")]
    [Tooltip("Referensi Player_Component.")]
    public Player_Components player1Reference;
    public Player_Components player2Reference;

    [Header("Door Interaction Icon")]
    [Tooltip("Icon interaction yang muncul di pintu saat player mendekat.")]
    public GameObject doorInteractionIcon;

    // Listents for player "Interact" input

    void Start()
    {
        if (doorInteractionIcon != null)
        {
            doorInteractionIcon.SetActive(false);
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (doorInteractionIcon != null)
            {
                doorInteractionIcon.SetActive(true);
            }

            // Subscribe to the player's "Interact" input event and only listens if the player is within the trigger area and calls the HandlePlayerAction method.
            Player_Components playerComponent = other.GetComponent<Player_Components>();
            if (playerComponent == player1Reference && player1Reference != null && player1Reference.moduleInputPlay != null)
            {
                player1Reference.moduleInputPlay.OnAction += HandlePlayerAction;
            }
            else if (playerComponent == player2Reference && player2Reference != null && player2Reference.moduleInputPlay != null)
            {
                player2Reference.moduleInputPlay.OnAction += HandlePlayerAction;
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (doorInteractionIcon != null)
            {
                doorInteractionIcon.SetActive(false);
            }

            // Unsubscribe from the player's "Interact" input event
            Player_Components playerComponent = other.GetComponent<Player_Components>();
            if (playerComponent == player1Reference && player1Reference != null && player1Reference.moduleInputPlay != null)
            {
                player1Reference.moduleInputPlay.OnAction -= HandlePlayerAction;
            }
            else if (playerComponent == player2Reference && player2Reference != null && player2Reference.moduleInputPlay != null)
            {
                player2Reference.moduleInputPlay.OnAction -= HandlePlayerAction;
            }
        }
    }

    private void HandlePlayerAction(ActionState state)
    {
        // Check if the action is Interact and then trigger the door animation
        if (state == ActionState.Interact && doorAnimator != null)
        {
            doorAnimator.SetTrigger("DoorOpen");
        }
    }
}