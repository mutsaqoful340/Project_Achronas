using UnityEngine;

/// <summary>
/// Coordinates the transition from character selection to gameplay
/// Wire _CharacterSelection.OnBothPlayersConfirmed to call OnConfirmationComplete()
/// </summary>
public class CharacterSelectionCoordinator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The character assignment manager that assigns gamepads to characters")]
    public CharacterAssignmentManager assignmentManager;

    [Tooltip("The game mode switch that controls player/UI modes")]
    public _GameModeSwitch gameModeSwitch;

    [Header("Optional: Cutscene Control")]
    [Tooltip("If assigned, will resume/unpause cutscene after confirmation")]
    public GameObject cutsceneController;
    
    private void Start()
    {
        Debug.Log($"<color=cyan>CharacterSelectionCoordinator initialized:</color>");
        Debug.Log($"  assignmentManager: {(assignmentManager != null ? "ASSIGNED" : "NULL")}");
        Debug.Log($"  gameModeSwitch: {(gameModeSwitch != null ? "ASSIGNED" : "NULL")}");
    }

    /// <summary>
    /// Call this from _CharacterSelection.OnBothPlayersConfirmed event
    /// </summary>
    public void OnConfirmationComplete()
    {
        Debug.Log("<color=magenta>=== CHARACTER SELECTION COMPLETE ===</color>");

        // Step 1: Assign devices to characters FIRST
        if (assignmentManager != null)
        {
            assignmentManager.AssignCharacters();
            assignmentManager.CheckAssignments();
        }
        else
        {
            Debug.LogError("<color=red>CharacterSelectionCoordinator: assignmentManager is NULL! Please assign it in inspector!</color>");
        }

        // Step 2: Switch to gameplay mode
        if (gameModeSwitch != null)
        {
            gameModeSwitch.SetMode(_GameModeSwitch.GameMode.Player);
        }
        else
        {
            Debug.LogError("<color=red>CharacterSelectionCoordinator: gameModeSwitch is NULL! Please assign it in inspector!</color>");
        }

        // Step 3: Resume cutscene (optional)
        if (cutsceneController != null)
        {
            // Add your cutscene resume logic here
            Debug.Log("<color=cyan>Cutscene resume placeholder - implement your cutscene resume logic</color>");
        }

        Debug.Log("<color=green>=== TRANSITION TO GAMEPLAY COMPLETE ===</color>");
    }

    /// <summary>
    /// Optional: Call this to return to character selection (e.g., from pause menu)
    /// </summary>
    public void ReturnToCharacterSelection()
    {
        Debug.Log("<color=yellow>=== Returning to Character Selection ===</color>");

        // Step 1: Switch back to UI mode
        if (gameModeSwitch != null)
        {
            gameModeSwitch.SetMode(_GameModeSwitch.GameMode.UI);
        }

        // Step 2: Clear device assignments
        if (assignmentManager != null)
        {
            assignmentManager.ClearAssignments();
        }

        // Step 3: Clear session data (optional - only if going back to main menu)
        // if (PlayerSessionData.Instance != null)
        // {
        //     PlayerSessionData.Instance.ClearData();
        // }

        Debug.Log("<color=yellow>=== Returned to Character Selection ===</color>");
    }
}
