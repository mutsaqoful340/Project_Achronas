using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Assigns gamepads to the correct pre-existing characters based on PlayerSessionData
/// Call AssignCharacters() after character selection is complete
/// Also monitors for controller disconnections and auto-reassigns to available player slots
/// </summary>
public class _Sys_CharacterAssignmentManager : MonoBehaviour
{
    [Header("Character References")]
    [Tooltip("The Left character GameObject (index 1)")]
    public Player_Components leftCharacter;
    
    [Tooltip("The Right character GameObject (index 2)")]
    public Player_Components rightCharacter;

    [Header("Auto Assignment")]
    [Tooltip("Automatically assign characters on Start if session data exists")]
    public bool autoAssignOnStart = false;
    
    [Header("Dynamic Reassignment")]
    [Tooltip("Enable auto-reassignment when controllers disconnect")]
    public bool enableDynamicReassignment = true;

    // Track device ownership per player
    private UnityEngine.InputSystem.InputDevice player1Device;
    private UnityEngine.InputSystem.InputDevice player2Device;
    private Player_Components player1Character;  // Which character is player 1 controlling
    private Player_Components player2Character;  // Which character is player 2 controlling
    private bool isInitialized = false;

    private void Start()
    {
        if (autoAssignOnStart)
        {
            AssignCharacters();
            if (enableDynamicReassignment)
            {
                Initialize();
            }
        }
    }

    /// <summary>
    /// Initialize dynamic reassignment monitoring (call after initial character assignment)
    /// This will auto-reassign devices when controllers disconnect/reconnect
    /// </summary>
    public void Initialize()
    {
        if (isInitialized)
            return;

        isInitialized = true;
        InputSystem.onDeviceChange += OnDeviceChanged;
        Debug.Log("<color=cyan>CharacterAssignmentManager: Dynamic reassignment initialized</color>");
    }

    private void OnDestroy()
    {
        // Unsubscribe from device change events
        if (isInitialized)
        {
            InputSystem.onDeviceChange -= OnDeviceChanged;
        }
    }

    /// <summary>
    /// Assign gamepads to characters based on PlayerSessionData
    /// Call this after character selection is complete
    /// </summary>
    public void AssignCharacters()
    {
        // Validate session data
        if (_Sys_PlayerSessionData.Instance == null)
        {
            Debug.LogError("<color=red>CharacterAssignmentManager: PlayerSessionData.Instance is NULL!</color>");
            return;
        }

        if (!_Sys_PlayerSessionData.Instance.IsValid())
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

        Debug.Log("<color=cyan>=== CharacterAssignmentManager: Starting assignment ===</color>");

        // Get session data
        var sessionData = _Sys_PlayerSessionData.Instance;
        
        // Track devices for reassignment monitoring
        player1Device = sessionData.player1Device;
        player2Device = sessionData.player2Device;
        
        // Assign Player 1 and track which character they're controlling
        if (sessionData.player1CharacterIndex == 1)
        {
            player1Character = leftCharacter;
        }
        else if (sessionData.player1CharacterIndex == 2)
        {
            player1Character = rightCharacter;
        }
        
        AssignPlayer(
            1,
            sessionData.player1Device,
            sessionData.player1CharacterIndex,
            leftCharacter,
            rightCharacter
        );

        // Assign Player 2 and track which character they're controlling
        if (sessionData.player2CharacterIndex == 1)
        {
            player2Character = leftCharacter;
        }
        else if (sessionData.player2CharacterIndex == 2)
        {
            player2Character = rightCharacter;
        }
        
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
            Debug.Log($"<color=green>✓ Player {playerNum} → {characterName} character</color>");
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

        player1Device = null;
        player2Device = null;
        player1Character = null;
        player2Character = null;

        Debug.Log("<color=yellow>Character assignments cleared</color>");
    }

    /// <summary>
    /// Handle controller connect/disconnect events and auto-reassign if needed
    /// </summary>
    private void OnDeviceChanged(InputDevice device, InputDeviceChange change)
    {
        if (!enableDynamicReassignment)
            return;

        switch (change)
        {
            case InputDeviceChange.Removed:
                HandleDeviceDisconnect(device);
                break;
            case InputDeviceChange.Added:
                HandleDeviceConnect(device);
                break;
        }
    }

    /// <summary>
    /// Handle when a controller disconnects
    /// </summary>
    private void HandleDeviceDisconnect(InputDevice disconnectedDevice)
    {
        // Check which character is currently using this device
        if (player1Character != null && player1Character.assignedDevice != null && 
            player1Character.assignedDevice.deviceId == disconnectedDevice.deviceId)
        {
            Debug.LogWarning($"<color=orange>Player 1 controller disconnected!</color>");
            player1Device = null;
            Debug.Log($"<color=yellow>Waiting for new controller to reassign to Player 1...</color>");
        }
        else if (player2Character != null && player2Character.assignedDevice != null && 
                 player2Character.assignedDevice.deviceId == disconnectedDevice.deviceId)
        {
            Debug.LogWarning($"<color=orange>Player 2 controller disconnected!</color>");
            player2Device = null;
            Debug.Log($"<color=yellow>Waiting for new controller to reassign to Player 2...</color>");
        }
    }

    /// <summary>
    /// Handle when a controller connects and auto-assign to disconnected player if applicable
    /// </summary>
    private void HandleDeviceConnect(InputDevice connectedDevice)
    {
        // Only reassign if it's a gamepad
        if (!(connectedDevice is Gamepad))
            return;

        // If Player 1 is missing their device, assign this one to them
        if (player1Device == null && player1Character != null)
        {
            player1Device = connectedDevice;
            Debug.Log($"<color=green>✓ New controller auto-assigned to Player 1</color>");
            
            player1Character.AssignDevice(connectedDevice);
            if (_Sys_PlayerSessionData.Instance != null)
                _Sys_PlayerSessionData.Instance.player1Device = connectedDevice;
            Debug.Log($"<color=green>Player 1 character updated with new device</color>");
        }
        // If Player 2 is missing their device, assign this one to them
        else if (player2Device == null && player2Character != null)
        {
            player2Device = connectedDevice;
            Debug.Log($"<color=green>✓ New controller auto-assigned to Player 2</color>");
            
            player2Character.AssignDevice(connectedDevice);
            if (_Sys_PlayerSessionData.Instance != null)
                _Sys_PlayerSessionData.Instance.player2Device = connectedDevice;
            Debug.Log($"<color=green>Player 2 character updated with new device</color>");
        }
        else
        {
            Debug.Log($"<color=yellow>Both players already have controllers. Ignoring extra connected device.</color>");
        }
    }

    /// <summary>
    /// Manually reassign a device to a player (used for dynamic reassignment or manual override)
    /// </summary>
    public void ReassignDevice(int playerNum, InputDevice newDevice)
    {
        if (playerNum == 1)
        {
            player1Device = newDevice;
            if (player1Character != null)
            {
                player1Character.AssignDevice(newDevice);
                if (_Sys_PlayerSessionData.Instance != null)
                    _Sys_PlayerSessionData.Instance.player1Device = newDevice;
                Debug.Log($"<color=cyan>Player 1 reassigned to new device</color>");
            }
        }
        else if (playerNum == 2)
        {
            player2Device = newDevice;
            if (player2Character != null)
            {
                player2Character.AssignDevice(newDevice);
                if (_Sys_PlayerSessionData.Instance != null)
                    _Sys_PlayerSessionData.Instance.player2Device = newDevice;
                Debug.Log($"<color=cyan>Player 2 reassigned to new device</color>");
            }
        }
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

        if (_Sys_PlayerSessionData.Instance != null)
        {
            Debug.Log($"Session Data Valid: {_Sys_PlayerSessionData.Instance.IsValid()}");
        }
        else
        {
            Debug.Log("PlayerSessionData: ✗ Instance is NULL");
        }
    }
}
