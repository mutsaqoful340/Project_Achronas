using UnityEngine;

public class _GP_Lever : MonoBehaviour
{
    [Header("Temporary Lever Player Reference")]
    [Tooltip("Referensi temporary buat player yang masuk ke area trigger Lever.")]
    [SerializeField] private GameObject tempPlayerLever;
    [Tooltip("Animator milik player.")]
    [SerializeField] private Animator playerAnimator;
    [Tooltip("Animator milik lever.")]
    [SerializeField] private Animator leverAnimator;
    public Animator leverManagerAnimator;

    [Tooltip("Posisi yang akan digunakan untuk memindahkan player saat berinteraksi dengan lever.")]
    public Transform playerOnLeverPosition;

    [Header("Lever Manager Reference")]
    [SerializeField] private _GP_LeverManager leverManager;

    // Private reference to player's input module
    private _ModuleInputPlay playerInputModule;

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            tempPlayerLever = other.gameObject;
            
            // Get player's input module and subscribe to interact action
            Player_Components playerComponents = tempPlayerLever.GetComponent<Player_Components>();
            playerAnimator = tempPlayerLever.GetComponent<Animator>();
            leverAnimator = GetComponent<Animator>();
            if (playerComponents != null && playerComponents.moduleInputPlay != null)
            {
                playerInputModule = playerComponents.moduleInputPlay;
                playerInputModule.OnAction += HandlePlayerAction; // Subscribe to player's action input
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Unsubscribe from interact action
            if (playerInputModule != null)
            {
                playerInputModule.OnAction -= HandlePlayerAction;
                playerInputModule = null;
            }
            
            tempPlayerLever = null;
            playerAnimator = null;
            leverAnimator = null;
        }
    }
    
    // Handle player action input
    private void HandlePlayerAction(ActionState actionState)
    {
        if (actionState == ActionState.Interact)
        {
            OnSendPlayerReference();
            OnPlayerParent();
            OnSwitchGamemode();
        }
    }

    private void OnSendPlayerReference()
    {
        leverManager.SetPlayerLever(tempPlayerLever);
    }
    
    private void OnPlayerParent()
    {
        tempPlayerLever.transform.SetParent(playerOnLeverPosition); 
        tempPlayerLever.transform.localPosition = Vector3.zero;
        tempPlayerLever.transform.localRotation = Quaternion.identity;
    }

    private void OnSwitchGamemode()
    {
        if (tempPlayerLever == null)
            return;
        
        // Lock player to InCutscene state and disable CharacterController
        Player_Components playerComponent = tempPlayerLever.GetComponent<Player_Components>();
        if (playerComponent != null)
        {
            playerComponent.CurrentState(ActionState.InCutscene);
        }
        
        CharacterController cc = tempPlayerLever.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }
    }
    
    // Call this method when the player should exit lever interaction (e.g., from animation event or manager)
    public void RestorePlayerControl()
    {
        if (tempPlayerLever == null)
            return;
        
        // Unparent the player
        tempPlayerLever.transform.SetParent(null);
        
        // Restore player to Idle state and re-enable CharacterController
        Player_Components playerComponent = tempPlayerLever.GetComponent<Player_Components>();
        if (playerComponent != null)
        {
            playerComponent.CurrentState(ActionState.Idle);
        }
        
        CharacterController cc = tempPlayerLever.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = true;
        }
        
        // Complete cleanup to prevent leaks and re-interaction issues
        CleanupPlayerReferences();
    }
    
    // Complete cleanup of all player references and subscriptions
    private void CleanupPlayerReferences()
    {
        // Unsubscribe from input events
        if (playerInputModule != null)
        {
            playerInputModule.OnAction -= HandlePlayerAction;
            playerInputModule = null;
        }
        
        // Clear all references
        tempPlayerLever = null;
        playerAnimator = null;
        leverAnimator = null;
        
        Debug.Log($"Lever {gameObject.name} - All player references cleaned up");
    }
}