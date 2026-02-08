using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections.Generic;
using TMPro;

public class _CharacterSelection : MonoBehaviour
{
    #region Player Login Properties
    [Header("Player Login")]
    private InputDevice player1Device;
    private InputDevice player2Device;
    private bool player1LoggedIn = false;
    private bool player2LoggedIn = false;
    #endregion

    #region Character Selection Properties
    [Header("Cursors")]
    public GameObject player1CursorTransform;
    public GameObject player2CursorTransform;
    public GameObject player1CusorVisual;
    public GameObject player2CusorVisual;
    
    [Header("Selection Positions")]
    [Tooltip("Individual positions for each player's cursor at each state")]
    public GameObject cursorNeutralObj;
    public GameObject cursorLeftObj;
    public GameObject cursorRightObj;
    
    [Header("Visual Feedback")]
    public Image player1CursorImage;
    public Image player2CursorImage;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.cyan; // Color when valid selection made
    public Color conflictColor = Color.red;
    
    // Player selections: 0=Neutral, 1=Left, 2=Right
    private int player1SelectedCharacter = 0; // Start at neutral
    private int player2SelectedCharacter = 0; // Start at neutral
    private bool player1HasStartedSelecting = false; // Track if moved from neutral
    private bool player2HasStartedSelecting = false; // Track if moved from neutral
    private bool player1Confirmed = false;
    private bool player2Confirmed = false;
    private bool bothPlayersConfirmed = false; // Prevent spam
    private bool hasConflict = false;
    #endregion

    [Header("Debug")]
    public TextMeshProUGUI Player1Device;
    public TextMeshProUGUI Player2Device;

    public UnityEvent OnBothPlayersConfirmed;

    void Start() // Both "cursors" are disabled at start
    {
        player1CursorTransform.SetActive(false);
        player2CursorTransform.SetActive(false);
        player1CusorVisual.SetActive(false);
        player2CusorVisual.SetActive(false);
        
        // Initialize cursor images if not assigned
        if (player1CursorImage == null && player1CursorTransform != null)
            player1CursorImage = player1CursorTransform.GetComponent<Image>();
        if (player2CursorImage == null && player2CursorTransform != null)
            player2CursorImage = player2CursorTransform.GetComponent<Image>();
        
        // Initialize debug text
        if (Player1Device != null)
            Player1Device.text = "Player 1: Waiting...";
        if (Player2Device != null)
            Player2Device.text = "Player 2: Waiting...";
        
        Debug.Log("Player1Device is " + (Player1Device != null ? "assigned" : "NULL"));
        Debug.Log("Player2Device is " + (Player2Device != null ? "assigned" : "NULL"));
        Debug.Log($"Selection positions: Neutral={cursorNeutralObj!=null}, Left={cursorLeftObj!=null}, Right={cursorRightObj!=null}");
    }

    void Update()
    {
        // Handle player login
        if (!player1LoggedIn || !player2LoggedIn)
        {
            HandlePlayerLogin();
        }
        // Handle character selection after both players logged in
        else if (player1LoggedIn && player2LoggedIn)
        {
            HandleCharacterSelection();
            CheckConflict();
            HandleConfirmation();
        }
    }

    void HandlePlayerLogin()
    {
        // Check all connected gamepads for any button press
        foreach (var gamepad in Gamepad.all)
        {
            // Skip if this device is already assigned
            if ((player1Device != null && player1Device.deviceId == gamepad.deviceId) ||
                (player2Device != null && player2Device.deviceId == gamepad.deviceId))
            {
                continue;
            }

            // Check if any button was pressed on this gamepad
            if (gamepad.wasUpdatedThisFrame && IsAnyButtonPressed(gamepad))
            {
                // Assign to Player 1 if not logged in
                if (!player1LoggedIn)
                {
                    player1Device = gamepad;
                    player1LoggedIn = true;
                    player1CursorTransform.SetActive(true);
                    player1CusorVisual.SetActive(true);
                    Debug.Log("Player 1 logged in with device: " + gamepad.deviceId);
                    
                    // Update debug UI
                    if (Player1Device != null)
                        Player1Device.text = $"Player 1: {gamepad.name} (ID: {gamepad.deviceId.ToString()})";
                }
                // Assign to Player 2 if not logged in
                else if (!player2LoggedIn)
                {
                    player2Device = gamepad;
                    player2LoggedIn = true;
                    player2CursorTransform.SetActive(true);
                    player2CusorVisual.SetActive(true);
                    Debug.Log("Player 2 logged in with device: " + gamepad.deviceId);
                    
                    // Update debug UI
                    if (Player2Device != null)
                        Player2Device.text = $"Player 2: {gamepad.name} (ID: {gamepad.deviceId.ToString()})";
                }
            }
        }
    }

    bool IsAnyButtonPressed(Gamepad gamepad)
    {
        // Check all buttons
        return gamepad.buttonSouth.wasPressedThisFrame ||
               gamepad.buttonNorth.wasPressedThisFrame ||
               gamepad.buttonEast.wasPressedThisFrame ||
               gamepad.buttonWest.wasPressedThisFrame ||
               gamepad.leftShoulder.wasPressedThisFrame ||
               gamepad.rightShoulder.wasPressedThisFrame ||
               gamepad.startButton.wasPressedThisFrame ||
               gamepad.selectButton.wasPressedThisFrame ||
               gamepad.leftStickButton.wasPressedThisFrame ||
               gamepad.rightStickButton.wasPressedThisFrame ||
               gamepad.dpad.up.wasPressedThisFrame ||
               gamepad.dpad.down.wasPressedThisFrame ||
               gamepad.dpad.left.wasPressedThisFrame ||
               gamepad.dpad.right.wasPressedThisFrame;
    }

    void HandleCharacterSelection()
    {
        // Player 1 character selection
        if (!player1Confirmed && player1Device is Gamepad gamepad1)
        {
            float horizontalInput = gamepad1.leftStick.x.ReadValue();
            
            if ((horizontalInput > 0.5f || horizontalInput < -0.5f) && !IsInputCooldown(1))
            {
                if (!player1HasStartedSelecting)
                {
                    // First movement from neutral - go to left (1) or right (2) based on direction
                    player1SelectedCharacter = (horizontalInput > 0) ? 1 : 2;
                    player1HasStartedSelecting = true;
                    Debug.Log($"Player 1 started selecting: {GetIndexName(player1SelectedCharacter)}");
                }
                else
                {
                    // After first selection, toggle between 1 (Left) and 2 (Right)
                    player1SelectedCharacter = (player1SelectedCharacter == 1) ? 2 : 1;
                    Debug.Log($"Player 1 switched to: {GetIndexName(player1SelectedCharacter)}");
                }
                
                UpdateCharacterCursor(1);
                StartInputCooldown(1);
            }
        }

        // Player 2 character selection
        if (!player2Confirmed && player2Device is Gamepad gamepad2)
        {
            float horizontalInput = gamepad2.leftStick.x.ReadValue();
            
            if ((horizontalInput > 0.5f || horizontalInput < -0.5f) && !IsInputCooldown(2))
            {
                if (!player2HasStartedSelecting)
                {
                    // First movement from neutral - go to left (1) or right (2) based on direction
                    player2SelectedCharacter = (horizontalInput > 0) ? 1 : 2;
                    player2HasStartedSelecting = true;
                    Debug.Log($"Player 2 started selecting: {GetIndexName(player2SelectedCharacter)}");
                }
                else
                {
                    // After first selection, toggle between 1 (Left) and 2 (Right)
                    player2SelectedCharacter = (player2SelectedCharacter == 1) ? 2 : 1;
                    Debug.Log($"Player 2 switched to: {GetIndexName(player2SelectedCharacter)}");
                }
                
                UpdateCharacterCursor(2);
                StartInputCooldown(2);
            }
        }
    }

    void HandleConfirmation()
    {
        // Can't confirm if there's a conflict
        if (hasConflict)
        {
            // If they try to confirm during conflict, cancel any confirmations
            if (player1Confirmed || player2Confirmed)
            {
                player1Confirmed = false;
                player2Confirmed = false;
                bothPlayersConfirmed = false;
                UpdateConfirmVisual();
                Debug.LogWarning("Cannot confirm - conflict detected!");
            }
            return;
        }
        
        // Player 1 confirmation
        if (player1Device is Gamepad gamepad1)
        {
            // Confirm with A button (buttonSouth)
            if (!player1Confirmed && gamepad1.buttonSouth.wasPressedThisFrame)
            {
                if (player1HasStartedSelecting) // Must have selected a character
                {
                    player1Confirmed = true;
                    Debug.Log($"Player 1 confirmed: {GetIndexName(player1SelectedCharacter)}");
                    UpdateConfirmVisual();
                }
                else
                {
                    Debug.LogWarning("Player 1 cannot confirm - please select a character first!");
                }
            }
            // Cancel with B button (buttonEast)
            else if (player1Confirmed && gamepad1.buttonEast.wasPressedThisFrame)
            {
                player1Confirmed = false;
                bothPlayersConfirmed = false; // Reset on cancel
                Debug.Log("Player 1 cancelled confirmation");
                UpdateConfirmVisual();
            }
        }

        // Player 2 confirmation
        if (player2Device is Gamepad gamepad2)
        {
            // Confirm with A button (buttonSouth)
            if (!player2Confirmed && gamepad2.buttonSouth.wasPressedThisFrame)
            {
                if (player2HasStartedSelecting) // Must have selected a character
                {
                    player2Confirmed = true;
                    Debug.Log($"Player 2 confirmed: {GetIndexName(player2SelectedCharacter)}");
                    UpdateConfirmVisual();
                }
                else
                {
                    Debug.LogWarning("Player 2 cannot confirm - please select a character first!");
                }
            }
            // Cancel with B button (buttonEast)
            else if (player2Confirmed && gamepad2.buttonEast.wasPressedThisFrame)
            {
                player2Confirmed = false;
                bothPlayersConfirmed = false; // Reset on cancel
                Debug.Log("Player 2 cancelled confirmation");
                UpdateConfirmVisual();
            }
        }

        // Check if both players confirmed (only invoke once)
        if (player1Confirmed && player2Confirmed && !bothPlayersConfirmed)
        {
            bothPlayersConfirmed = true;
            Debug.Log($"Both players confirmed! P1={GetIndexName(player1SelectedCharacter)}, P2={GetIndexName(player2SelectedCharacter)}");
            SaveSelections();
            OnBothPlayersConfirmed?.Invoke();
        }
    }

    // Input cooldown to prevent rapid switching
    private float player1InputCooldown = 0f;
    private float player2InputCooldown = 0f;
    private const float INPUT_COOLDOWN_TIME = 0.2f;

    bool IsInputCooldown(int playerNum)
    {
        if (playerNum == 1)
            return Time.time < player1InputCooldown;
        else
            return Time.time < player2InputCooldown;
    }

    void StartInputCooldown(int playerNum)
    {
        if (playerNum == 1)
            player1InputCooldown = Time.time + INPUT_COOLDOWN_TIME;
        else
            player2InputCooldown = Time.time + INPUT_COOLDOWN_TIME;
    }

    void CheckConflict()
    {
        // Check if both players are on the same character (and not neutral)
        bool conflictNow = (player1SelectedCharacter == player2SelectedCharacter) && 
                           (player1SelectedCharacter != 0); // No conflict on neutral
        
        if (conflictNow && !hasConflict)
        {
            // Conflict started
            hasConflict = true;
            Debug.LogWarning($"⚠️ CONFLICT! Both players on {GetIndexName(player1SelectedCharacter)}!");
            UpdateConfirmVisual();
            
            // TODO: Add your gimmick here!
            // Example: Play sound, shake characters, show warning UI
        }
        else if (!conflictNow && hasConflict)
        {
            // Conflict resolved
            hasConflict = false;
            Debug.Log("✓ Conflict resolved");
            UpdateConfirmVisual();
        }
    }

    void UpdateCharacterCursor(int playerNum)
    {
        int selectedIndex = (playerNum == 1) ? player1SelectedCharacter : player2SelectedCharacter;
        GameObject cursor = (playerNum == 1) ? player1CursorTransform : player2CursorTransform;
        
        // Get the appropriate selection position GameObject based on index
        GameObject targetParent = null;
        
        switch (selectedIndex)
        {
            case 0: targetParent = cursorNeutralObj; break;
            case 1: targetParent = cursorLeftObj; break;
            case 2: targetParent = cursorRightObj; break;
        }
        
        // Parent cursor to selection position
        if (targetParent != null)
        {
            cursor.transform.SetParent(targetParent.transform, worldPositionStays: false);
            cursor.transform.localPosition = Vector3.zero; // Snap to parent position
        }
        else
        {
            Debug.LogWarning($"Player {playerNum} selection position for {GetIndexName(selectedIndex)} not assigned!");
        }
        
        // Update visual feedback
        UpdateConfirmVisual();
    }

    void UpdateConfirmVisual()
    {
        // Player 1 cursor color
        if (player1CursorImage != null)
        {
            if (hasConflict && player1SelectedCharacter != 0)
            {
                player1CursorImage.color = conflictColor; // Red for conflict
            }
            else if (player1HasStartedSelecting) // Valid selection made
            {
                player1CursorImage.color = selectedColor; // Cyan when on valid character
            }
            else
            {
                player1CursorImage.color = normalColor; // Normal color (at neutral)
            }
        }
        
        // Player 2 cursor color
        if (player2CursorImage != null)
        {
            if (hasConflict && player2SelectedCharacter != 0)
            {
                player2CursorImage.color = conflictColor; // Red for conflict
            }
            else if (player2HasStartedSelecting) // Valid selection made
            {
                player2CursorImage.color = selectedColor; // Cyan when on valid character
            }
            else
            {
                player2CursorImage.color = normalColor; // Normal color (at neutral)
            }
        }
    }
    
    void SaveSelections()
    {
        // Save to PlayerPrefs
        PlayerPrefs.SetInt("Player1CharacterIndex", player1SelectedCharacter);
        PlayerPrefs.SetInt("Player2CharacterIndex", player2SelectedCharacter);
        PlayerPrefs.Save();
        
        Debug.Log($"💾 Selections saved: P1={player1SelectedCharacter}, P2={player2SelectedCharacter}");
    }
    
    string GetIndexName(int index)
    {
        switch (index)
        {
            case 0: return "Neutral";
            case 1: return "Left";
            case 2: return "Right";
            default: return "Unknown";
        }
    }
}