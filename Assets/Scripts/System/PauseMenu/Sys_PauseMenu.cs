using UnityEngine;
using Unity.Cinemachine;

public class Sys_PauseMenu : Sys_GameplayBehaviour
{
    private bool isPaused = false;

    [Header("Player References")]
    public Player_Components player1Ref;
    public Player_Components player2Ref;

    [Header("Pause Menu References")]
    public Transform player2PauseMenuPosition; // The position where player 2's character will be moved to when the pause menu is active

    [Header("Pause Menu UI")]
    public GameObject pauseMenuUI; // Assign the pause menu UI panel in inspector

    [Header("Camera Control")]
    public _Sys_VCamPriorityController priorityController;
    public CinemachineVirtualCameraBase pauseMenuCamera; // The camera to show when paused

    [Header("System References")]
    public Sys_GameModeSwitch gameModeSwitch; // Reference to the game mode switch system (if needed for additional functionality)
    
    private CinemachineVirtualCameraBase previousCamera; // Store the camera that was active before pause

    private bool isPlayer2ParentedToPauseMenu = false;
    private CharacterController player2CC;
    private Rigidbody player2Rigidbody;
    private bool player2RigidbodyWasKinematic;

    protected override void OnEnable()
    {
        base.OnEnable(); // Call parent OnEnable
        
        // Subscribe to both players' input events IMMEDIATELY (NOT in OnGameplayEnabled)
        // This ensures pause/unpause works regardless of game mode
        if (player1Ref != null && player1Ref.moduleInputPlay != null)
            player1Ref.moduleInputPlay.OnAction += OnPlayerAction;
        
        if (player2Ref != null && player2Ref.moduleInputPlay != null)
            player2Ref.moduleInputPlay.OnAction += OnPlayerAction;
    }

    protected override void OnDisable()
    {
        // Unsubscribe when disabled
        if (player1Ref != null && player1Ref.moduleInputPlay != null)
            player1Ref.moduleInputPlay.OnAction -= OnPlayerAction;
        
        if (player2Ref != null && player2Ref.moduleInputPlay != null)
            player2Ref.moduleInputPlay.OnAction -= OnPlayerAction;
        
        base.OnDisable(); // Call parent OnDisable
    }

    protected override void OnGameplayEnabled()
    {
        // Note: Input subscription now happens in OnEnable() so it works during pause too
    }

    protected override void OnGameplayDisabled()
    {
        // Note: Input subscription is NOT unsubscribed here anymore - we need it during pause!
    }

    protected override void Start()
    {
        base.Start(); // CRITICAL: Call base.Start() to subscribe to GameplayBehaviour events
        
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false); // Ensure the pause menu is hidden at the start
        }
        else
        {
            Debug.LogWarning("Pause menu UI not assigned in inspector!");
        }

        if (priorityController == null)
        {
            Debug.LogWarning("Priority controller not assigned in inspector! Camera switching won't work.");
        }

        if (pauseMenuCamera == null)
        {
            Debug.LogWarning("Pause menu camera not assigned in inspector!");
        }

        if (gameModeSwitch == null)
        {
            Debug.LogWarning("Game mode switch not assigned in inspector! Game mode switching won't work.");
        }
    }

    /// <summary>
    /// Called when either player performs an action.
    /// Only responds to PauseMenu action.
    /// </summary>
    private void OnPlayerAction(ActionState action)
    {
        if (action != ActionState.PauseMenu)
            return;

        if (isPaused)
        {
            Debug.Log("<color=cyan>[PauseMenu] Resuming game...</color>");
            Resume();
            if (gameModeSwitch != null)
                gameModeSwitch.SetMode(Sys_GameModeSwitch.GameMode.Player); // Switch back to Player mode when resuming
        }
        else
        {
            Debug.Log("<color=cyan>[PauseMenu] Pausing game...</color>");
            Pause();
            if (gameModeSwitch != null)
                gameModeSwitch.SetMode(Sys_GameModeSwitch.GameMode.UI); // Switch to UI mode when paused
        }
    }

    void Update()
    {
        OnPlayerPosPauseMenu();
    }

    private void Pause()
    {
        isPaused = true;
        // Time.timeScale = 0f; 
        
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);
        else
            Debug.LogWarning("Pause menu UI not assigned!");

        // Store the currently active camera and switch to pause menu camera
        if (priorityController != null && pauseMenuCamera != null)
        {
            previousCamera = priorityController.GetCurrentCamera();
            priorityController.SetCameraActive(pauseMenuCamera);
            Debug.Log($"<color=cyan>[PauseMenu] Switched camera to {pauseMenuCamera.name}</color>");
        }

        OnPlayerPosPauseMenu(); // Move player 2 to the pause menu position if needed
    }

    private void Resume()
    {
        isPaused = false;
        // Time.timeScale = 1f; // Resume game time
        
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        // Restore the previous camera
        if (priorityController != null && previousCamera != null)
        {
            priorityController.SetCameraActive(previousCamera);
            Debug.Log($"<color=cyan>[PauseMenu] Restored camera to {previousCamera.name}</color>");
        }

        OnPlayerPosPauseMenuExit(); // Return player 2 to normal parenting
    }

    private void OnPlayerPosPauseMenu()
    {
        if (player2Ref != null && player2PauseMenuPosition != null)
        {
            if (isPaused)
            {
                if (!isPlayer2ParentedToPauseMenu)
                {
                    player2Ref.transform.SetParent(player2PauseMenuPosition);
                    
                    // Disable physics components
                    player2CC = player2Ref.GetComponent<CharacterController>();
                    if (player2CC != null) player2CC.enabled = false;
                    
                    player2Rigidbody = player2Ref.GetComponent<Rigidbody>();
                    if (player2Rigidbody != null)
                    {
                        player2RigidbodyWasKinematic = player2Rigidbody.isKinematic;
                        player2Rigidbody.isKinematic = true;
                    }
                    
                    Debug.Log("<color=cyan>[PauseMenu] Parent Player 2 to pause menu position</color>");
                    isPlayer2ParentedToPauseMenu = true;
                }

                player2Ref.transform.localPosition = Vector3.zero;
                player2Ref.transform.localRotation = Quaternion.identity;
            }
        }
    }

    private void OnPlayerPosPauseMenuExit()
    {
        if (player2Ref != null)
        {
            player2Ref.transform.SetParent(null);
            
            // Re-enable physics components
            if (player2CC != null) player2CC.enabled = true;
            if (player2Rigidbody != null) player2Rigidbody.isKinematic = player2RigidbodyWasKinematic;
            
            isPlayer2ParentedToPauseMenu = false;
            Debug.Log("<color=cyan>[PauseMenu] Unparented Player 2 from pause menu position</color>");
        }
    }
}