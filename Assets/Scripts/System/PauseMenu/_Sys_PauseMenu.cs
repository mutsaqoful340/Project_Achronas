using UnityEngine;
using Unity.Cinemachine;

public class _Sys_PauseMenu : MonoBehaviour
{
    private bool isPaused = false;

    [Header("Player References")]
    public Player_Components player1Reference;
    public Player_Components player2Reference;

    [Header("Pause Menu References")]
    public Transform player2PauseMenuPosition; // The position where player 2's character will be moved to when the pause menu is active

    [Header("Pause Menu UI")]
    public GameObject pauseMenuUI; // Assign the pause menu UI panel in inspector

    [Header("Camera Control")]
    public _Sys_VCamPriorityController priorityController;
    public CinemachineVirtualCameraBase pauseMenuCamera; // The camera to show when paused

    [Header("System References")]
    public _Sys_GameModeSwitch gameModeSwitch; // Reference to the game mode switch system (if needed for additional functionality)
    
    private CinemachineVirtualCameraBase previousCamera; // Store the camera that was active before pause

    private void OnEnable()
    {
        // Subscribe to both players' input events
        if (player1Reference != null && player1Reference.moduleInputPlay != null)
            player1Reference.moduleInputPlay.OnAction += OnPlayerAction;
        
        if (player2Reference != null && player2Reference.moduleInputPlay != null)
            player2Reference.moduleInputPlay.OnAction += OnPlayerAction;
    }

    private void OnDisable()
    {
        // Unsubscribe from both players' input events
        if (player1Reference != null && player1Reference.moduleInputPlay != null)
            player1Reference.moduleInputPlay.OnAction -= OnPlayerAction;
        
        if (player2Reference != null && player2Reference.moduleInputPlay != null)
            player2Reference.moduleInputPlay.OnAction -= OnPlayerAction;
    }

    void Start()
    {
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
            Resume();
            if (gameModeSwitch != null)
                gameModeSwitch.SetMode(_Sys_GameModeSwitch.GameMode.Player); // Switch back to Player mode when resuming
        }
        else
        {
            Pause();
            if (gameModeSwitch != null)
                gameModeSwitch.SetMode(_Sys_GameModeSwitch.GameMode.UI); // Switch to UI mode when paused
        }
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
        if (player2Reference != null && player2PauseMenuPosition != null)
        {
            player2Reference.transform.SetParent(player2PauseMenuPosition);
            player2Reference.transform.localPosition = Vector3.zero; // Optional: reset local position to align with the pause menu position
            player2Reference.transform.localRotation = Quaternion.identity; // Optional: reset local rotation
            Debug.Log("<color=cyan>[PauseMenu] Parented Player 2 to pause menu position</color>");
        }
    }

    private void OnPlayerPosPauseMenuExit()
    {
        if (player2Reference != null)
        {
            player2Reference.transform.SetParent(null);
            Debug.Log("<color=cyan>[PauseMenu] Unparented Player 2 from pause menu position</color>");
        }
    }
}
