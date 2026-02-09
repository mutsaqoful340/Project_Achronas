using UnityEngine;

/// <summary>
/// Assigns gamepads to the correct pre-existing characters based on PlayerSessionData
/// Call AssignCharacters() after character selection is complete
/// </summary>
public class CharacterAssignmentManager : MonoBehaviour
{
    [Header("Character References")]
    [Tooltip("The Left character GameObject (index 1)")]
    public Player_Components leftCharacter;
    
    [Tooltip("The Right character GameObject (index 2)")]
    public Player_Components rightCharacter;

    [Header("Auto Assignment")]
    [Tooltip("Automatically assign characters on Start if session data exists")]
    public bool autoAssignOnStart = false;

    private void Start()
    {
        if (autoAssignOnStart)
        {
            AssignCharacters();
        }
    }

    /// <summary>
    /// Assign gamepads to characters based on PlayerSessionData
    /// Call this after character selection is complete
    /// </summary>
    public void AssignCharacters()
    {
        // Validate session data
        if (PlayerSessionData.Instance == null)
        {
            Debug.LogError("<color=red>CharacterAssignmentManager: PlayerSessionData.Instance is NULL!</color>");
            return;
        }

        if (!PlayerSessionData.Instance.IsValid())
        {
            Debug.LogError("<color=red>CharacterAssignmentManager: PlayerSessionData is incomplete! Cannot assign characters.</color>");
            return;
        }

        // Validate character references
        if (leftCharacter == null || rightCharacter == null)
        {
            Debug.LogError("<color=red>CharacterAssignmentManager: Character references not assigned!</color>");
            return;
        }

        Debug.Log("<color=cyan>=== CharacterAssignmentManager: Starting character assignment ===</color>");

        // Get session data
        var sessionData = PlayerSessionData.Instance;
        
        Debug.Log($"<color=cyan>Session Data:</color>");
        Debug.Log($"  P1: Device ID={sessionData.player1Device?.deviceId}, Character Index={sessionData.player1CharacterIndex}");
        Debug.Log($"  P2: Device ID={sessionData.player2Device?.deviceId}, Character Index={sessionData.player2CharacterIndex}");
        
        // Assign Player 1
        AssignPlayer(
            1,
            sessionData.player1Device,
            sessionData.player1CharacterIndex,
            leftCharacter,
            rightCharacter
        );

        // Assign Player 2
        AssignPlayer(
            2,
            sessionData.player2Device,
            sessionData.player2CharacterIndex,
            leftCharacter,
            rightCharacter
        );

        Debug.Log("<color=green>=== Character assignment complete! ===</color>");
    }

    private void AssignPlayer(int playerNum, UnityEngine.InputSystem.InputDevice device, int characterIndex, Player_Components left, Player_Components right)
    {
        // Determine which character this player selected
        Player_Components targetCharacter = null;
        string characterName = "";

        switch (characterIndex)
        {
            case 1: // Left character
                targetCharacter = left;
                characterName = "Left";
                break;
            case 2: // Right character
                targetCharacter = right;
                characterName = "Right";
                break;
            default:
                Debug.LogError($"<color=red>Invalid character index {characterIndex} for Player {playerNum}</color>");
                return;
        }

        // Assign device to the character's module input
        if (targetCharacter != null)
        {
            targetCharacter.AssignDevice(device);
            bool hasDevice = targetCharacter.HasDevice();
            Debug.Log($"<color=green>✓ Player {playerNum} → {characterName} character (Device: {device?.name}, ID: {device?.deviceId})</color>");
            Debug.Log($"<color=cyan>Device assignment verified: {(hasDevice ? "SUCCESS" : "FAILED")}</color>");
        }
        else
        {
            Debug.LogError($"<color=red>Player {playerNum}'s {characterName} character is NULL!</color>");
        }
    }

    /// <summary>
    /// Clear all gamepad assignments
    /// Useful when returning to character selection
    /// </summary>
    public void ClearAssignments()
    {
        if (leftCharacter != null)
        {
            leftCharacter.AssignDevice(null);
        }
        
        if (rightCharacter != null)
        {
            rightCharacter.AssignDevice(null);
        }

        Debug.Log("<color=yellow>Character assignments cleared</color>");
    }

    /// <summary>
    /// Debug method to check if assignments are valid
    /// </summary>
    [ContextMenu("Check Assignments")]
    public void CheckAssignments()
    {
        Debug.Log("=== Character Assignment Status ===");
        
        if (leftCharacter != null)
        {
            bool hasDevice = leftCharacter.HasDevice();
            Debug.Log($"Left Character: {(hasDevice ? "✓ Has device" : "✗ No device")}");
        }
        else
        {
            Debug.Log("Left Character: ✗ Not assigned");
        }

        if (rightCharacter != null)
        {
            bool hasDevice = rightCharacter.HasDevice();
            Debug.Log($"Right Character: {(hasDevice ? "✓ Has device" : "✗ No device")}");
        }
        else
        {
            Debug.Log("Right Character: ✗ Not assigned");
        }

        if (PlayerSessionData.Instance != null)
        {
            Debug.Log($"Session Data Valid: {PlayerSessionData.Instance.IsValid()}");
        }
        else
        {
            Debug.Log("PlayerSessionData: ✗ Instance is NULL");
        }
    }
}
