using Unity.VisualScripting;
using UnityEngine;

public class _GP_DoorInteract_Mng : MonoBehaviour
{
    [Header("Animator Reference")]
    [Tooltip("Animator yang mengontrol animasi pintu.")]
    public Animator doorAnimator;

    [Header("Player Componments References")]
    [Tooltip("Referensi komponen Player_Components dari pemain pertama.")]
    public Player_Components player1Reference;
    [Tooltip("Referensi komponen Player_Components dari pemain kedua.")]
    public Player_Components player2Reference;

    [Header("Player Position on Door Interaction")]
    [Tooltip("Posisi pemain pertama saat berinteraksi dengan pintu.")]
    public Transform player1InteractPosition;
    [Tooltip("Posisi pemain kedua saat berinteraksi dengan pintu.")]
    public Transform player2InteractPosition;

    [Header("Door Animation Control Collider")]
    [Tooltip("Collider yang digunakan untuk mendeteksi interaksi dengan pintu.")]
    public Collider doorInteractionCollider;

    [Header("Door Interaction Icon")]
    [Tooltip("Icon interaction yang muncul di pintu saat player mendekat.")]
    public GameObject doorInteractionIcon;

    [Header("Debug")]
    [Tooltip("Debug: Menyimpan referensi pemain yang pertama masuk ke area trigger.")]
    [SerializeField] private GameObject firstPlayerEntered;
    [Tooltip("Debug: Menyimpan referensi pemain yang kedua masuk ke area trigger.")]
    [SerializeField] private GameObject secondPlayerEntered;

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

        // Store the reference of the first and second player that entered the trigger area
        if (firstPlayerEntered == null)
        {
            firstPlayerEntered = other.gameObject;
        }
        else if (secondPlayerEntered == null && other.gameObject != firstPlayerEntered)
        {
            secondPlayerEntered = other.gameObject;
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

        // Clear the reference of the player that exited the trigger area
        firstPlayerEntered = null;
        secondPlayerEntered = null;
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