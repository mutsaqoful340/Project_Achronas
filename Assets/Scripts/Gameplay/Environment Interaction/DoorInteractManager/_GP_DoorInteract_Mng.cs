using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

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
    public string doorOpenTriggerName = "DoorsOpen"; // Nama trigger di Animator untuk membuka pintu

    [Header("Door Interaction Icon")]
    [Tooltip("Icon interaction yang muncul di pintu saat player mendekat.")]
    public GameObject doorInteractionIcon;

    [Header("Debug")]
    [Header("Player Entered Trigger Area")]
    [Tooltip("Menyimpan referensi pemain yang pertama masuk ke area trigger.")]
    [SerializeField] private GameObject firstPlayerEntered;
    [Tooltip("Menyimpan referensi pemain yang kedua masuk ke area trigger.")]
    [SerializeField] private GameObject secondPlayerEntered;
    [Header("Player Interacted")]
    [Tooltip("Player pertama yang melakukan interaksi dengan pintu.")]
    [SerializeField] private GameObject firstPlayerInteracted;
    [Tooltip("Player kedua yang melakukan interaksi dengan pintu.")]
    [SerializeField] private GameObject secondPlayerInteracted;

    // Event handlers for subscription/unsubscription
    private UnityAction<ActionState> player1ActionDelegate;
    private UnityAction<ActionState> player2ActionDelegate;
    
    // Track which players are in UI mode at the door
    private bool player1InDoorUI = false;
    private bool player2InDoorUI = false;


    // Listents for player "Interact" input

    void Start()
    {
        if (doorInteractionIcon != null)
        {
            doorInteractionIcon.SetActive(false);
        }
    }
    
    void Update()
    {
        // Check for exit input (Gamepad East = B/Circle button) for players in UI mode
        if (player1InDoorUI && player1Reference != null)
        {
            // Check if player 1 pressed the cancel/back button
            if (player1Reference.moduleInputPlay != null && CheckCancelInput(player1Reference))
            {
                ExitDoorInteraction(player1Reference, 0);
            }
        }
        
        if (player2InDoorUI && player2Reference != null)
        {
            // Check if player 2 pressed the cancel/back button
            if (player2Reference.moduleInputPlay != null && CheckCancelInput(player2Reference))
            {
                ExitDoorInteraction(player2Reference, 1);
            }
        }
    }
    
    private bool CheckCancelInput(Player_Components player)
    {
        // Check for B button (Gamepad East) or ESC key
        if (player.assignedDevice != null)
        {
            var gamepad = player.assignedDevice as UnityEngine.InputSystem.Gamepad;
            if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame)
            {
                return true;
            }
        }
        return false;
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
                player1ActionDelegate = (state) => HandlePlayerAction(state, player1Reference);
                player1Reference.moduleInputPlay.OnAction += player1ActionDelegate;
            }
            else if (playerComponent == player2Reference && player2Reference != null && player2Reference.moduleInputPlay != null)
            {
                player2ActionDelegate = (state) => HandlePlayerAction(state, player2Reference);
                player2Reference.moduleInputPlay.OnAction += player2ActionDelegate;
            }

            // Check if the first player entered variable is null, if it is, assign the player that entered the trigger area to the firstPlayerEntered variable. If the first player entered variable is not null, assign the player that entered the trigger area to the secondPlayerEntered variable.
            if (firstPlayerEntered == null)
            {
                firstPlayerEntered = other.gameObject;
                Debug.Log("First player entered: " + firstPlayerEntered.name);
            }
            else if (secondPlayerEntered == null && firstPlayerEntered != null && other.gameObject != firstPlayerEntered)
            {
                secondPlayerEntered = other.gameObject;
                Debug.Log("Second player entered: " + secondPlayerEntered.name);
            }

            // If someone already interacted and secondPlayerInteracted is still null, auto-assign this player
            if (firstPlayerInteracted != null && secondPlayerInteracted == null && other.gameObject != firstPlayerInteracted)
            {
                secondPlayerInteracted = other.gameObject;
                Debug.Log("Second player auto-assigned on enter: " + secondPlayerInteracted.name);
                
                // Switch the second player to UI mode as well
                Player_Components secondPlayer = secondPlayerInteracted.GetComponent<Player_Components>();
                if (secondPlayer != null)
                {
                    SwitchPlayerToDoorUIMode(secondPlayer);
                }
            }
        }
    }

    public void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OnPlayerEnterDoorArea();
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player_Components playerComponent = other.GetComponent<Player_Components>();
            
            // Check if it's Player 1
            if (playerComponent == player1Reference && player1Reference != null)
            {
                Debug.Log("Player 1 exited");
                if (player1Reference.moduleInputPlay != null && player1ActionDelegate != null)
                {
                    player1Reference.moduleInputPlay.OnAction -= player1ActionDelegate;
                }
                firstPlayerEntered = null;
                firstPlayerInteracted = null;
            }
            // Check if it's Player 2
            else if (playerComponent == player2Reference && player2Reference != null)
            {
                Debug.Log("Player 2 exited");
                if (player2Reference.moduleInputPlay != null && player2ActionDelegate != null)
                {
                    player2Reference.moduleInputPlay.OnAction -= player2ActionDelegate;
                }
                secondPlayerEntered = null;
                secondPlayerInteracted = null;
            }
        
            if (firstPlayerEntered == null && secondPlayerEntered == null)
            {
                if (doorInteractionIcon != null)
                {
                    doorInteractionIcon.SetActive(false);
                }
            }
        }

        // Clear the reference of the player that exited the trigger area
        // firstPlayerEntered = null;
        // secondPlayerEntered = null;
    }

    private void HandlePlayerAction(ActionState state, Player_Components player)
    {
        // Check if the action is Interact
        if (state == ActionState.Interact)
        {
            // Assign the first player that interacted
            if (firstPlayerInteracted == null)
            {
                firstPlayerInteracted = player.gameObject;
                Debug.Log("First player interacted: " + firstPlayerInteracted.name);
                
                // Switch first player to UI mode
                SwitchPlayerToDoorUIMode(player);
                
                Debug.Log($"{player.gameObject.name} entered door UI mode. Press B/Circle to exit.");
                
                // Automatically assign the other player as secondPlayerInteracted if they're in the area
                if (firstPlayerEntered != null && firstPlayerEntered != firstPlayerInteracted)
                {
                    secondPlayerInteracted = firstPlayerEntered;
                    Debug.Log("Second player auto-assigned: " + secondPlayerInteracted.name);
                    
                    // Switch second player to UI mode as well
                    Player_Components secondPlayer = secondPlayerInteracted.GetComponent<Player_Components>();
                    if (secondPlayer != null)
                    {
                        SwitchPlayerToDoorUIMode(secondPlayer);
                    }
                }
                else if (secondPlayerEntered != null && secondPlayerEntered != firstPlayerInteracted)
                {
                    secondPlayerInteracted = secondPlayerEntered;
                    Debug.Log("Second player auto-assigned: " + secondPlayerInteracted.name);
                    
                    // Switch second player to UI mode as well
                    Player_Components secondPlayer = secondPlayerInteracted.GetComponent<Player_Components>();
                    if (secondPlayer != null)
                    {
                        SwitchPlayerToDoorUIMode(secondPlayer);
                    }
                }
            }
        }
    }
    
    private void SwitchPlayerToDoorUIMode(Player_Components player)
    {
        // Unsubscribe from action events before switching to UI mode
        if (player == player1Reference && player1Reference.moduleInputPlay != null && player1ActionDelegate != null)
        {
            player1Reference.moduleInputPlay.OnAction -= player1ActionDelegate;
        }
        else if (player == player2Reference && player2Reference.moduleInputPlay != null && player2ActionDelegate != null)
        {
            player2Reference.moduleInputPlay.OnAction -= player2ActionDelegate;
        }
        
        // Disable the player's component to stop movement
        player.enabled = false;
        
        // Switch this player to UI mode
        int playerIndex = (player == player1Reference) ? 0 : 1;
        if (_Sys_GameModeSwitch.Instance != null)
        {
            _Sys_GameModeSwitch.Instance.SetPlayerMode(playerIndex, _Sys_GameModeSwitch.GameMode.UI);
        }
        
        // Track which player is in UI mode
        if (player == player1Reference)
        {
            player1InDoorUI = true;
        }
        else if (player == player2Reference)
        {
            player2InDoorUI = true;
        }
    }
    
    private void ExitDoorInteraction(Player_Components player, int playerIndex)
    {
        Debug.Log($"{player.gameObject.name} exited door UI mode.");
        
        // Re-enable the player's component
        player.enabled = true;
        
        // Switch player back to Player mode
        if (_Sys_GameModeSwitch.Instance != null)
        {
            _Sys_GameModeSwitch.Instance.SetPlayerMode(playerIndex, _Sys_GameModeSwitch.GameMode.Player);
        }
        
        // Resubscribe to action events after switching back to Player mode
        if (player == player1Reference && player1Reference.moduleInputPlay != null)
        {
            player1ActionDelegate = (state) => HandlePlayerAction(state, player1Reference);
            player1Reference.moduleInputPlay.OnAction += player1ActionDelegate;
        }
        else if (player == player2Reference && player2Reference.moduleInputPlay != null)
        {
            player2ActionDelegate = (state) => HandlePlayerAction(state, player2Reference);
            player2Reference.moduleInputPlay.OnAction += player2ActionDelegate;
        }
        
        // Unparent the player
        if (player == player1Reference && firstPlayerInteracted != null)
        {
            firstPlayerInteracted.transform.SetParent(null);
            player1InDoorUI = false;
        }
        else if (player == player2Reference && secondPlayerInteracted != null)
        {
            secondPlayerInteracted.transform.SetParent(null);
            player2InDoorUI = false;
        }
    }

    private void OnPlayerEnterDoorArea()
    {
        if (firstPlayerInteracted != null)
        {
            // Parent to the interact position if not already parented
            if (firstPlayerInteracted.transform.parent != player1InteractPosition)
            {
                firstPlayerInteracted.transform.SetParent(player1InteractPosition);
            }
            
            // Interpolate local position and rotation to normalize the transform
            firstPlayerInteracted.transform.localPosition = Vector3.Lerp(firstPlayerInteracted.transform.localPosition, Vector3.zero, Time.deltaTime * 5f);
            firstPlayerInteracted.transform.localRotation = Quaternion.Lerp(firstPlayerInteracted.transform.localRotation, Quaternion.identity, Time.deltaTime * 5f);
        }
        
        if (secondPlayerInteracted != null)
        {
            // Parent to the interact position if not already parented
            if (secondPlayerInteracted.transform.parent != player2InteractPosition)
            {
                secondPlayerInteracted.transform.SetParent(player2InteractPosition);
            }
            
            // Interpolate local position and rotation to normalize the transform
            secondPlayerInteracted.transform.localPosition = Vector3.Lerp(secondPlayerInteracted.transform.localPosition, Vector3.zero, Time.deltaTime * 5f);
            secondPlayerInteracted.transform.localRotation = Quaternion.Lerp(secondPlayerInteracted.transform.localRotation, Quaternion.identity, Time.deltaTime * 5f);
        }

        if (secondPlayerInteracted != null && firstPlayerInteracted != null)
        {
            // Trigger the door animation when both players are in position
            if (doorAnimator != null)
            {
                doorAnimator.SetTrigger(doorOpenTriggerName);
                Debug.Log("Both players interacted, opening door.");
            }
        }
    }
}