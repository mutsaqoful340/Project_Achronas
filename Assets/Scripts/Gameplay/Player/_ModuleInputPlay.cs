using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Module for handling player input from assigned gamepad
public enum ActionState
{
    Idle,
    Sprint,
    Crouch,
    Jump,
    Interact,
    Action1,
    Action2,
    Confirm
}

[CreateAssetMenu(fileName = "ModuleInputPlay", menuName = "AddOn Module/Input Play", order = 1)]
public class _ModuleInputPlay : ScriptableObject
{
    public UnityAction<ActionState> OnAction { get; set; }

    private bool wasSprintPressed = false;

    /// <summary>
    /// Get movement input from a specific gamepad
    /// </summary>
    public Vector3 GetMoveInput(InputDevice device)
    {
        Gamepad gamepad = device as Gamepad;
        if (gamepad == null)
        {
            if (Time.frameCount % 120 == 0) // Log every 2 seconds
            {
                Debug.LogWarning($"<color=orange>GetMoveInput: Device is not a Gamepad! Type: {device?.GetType().Name}</color>");
            }
            return Vector3.zero;
        }
        
        var stick = gamepad.leftStick.ReadValue();
        
        // Debug log for first few frames or when movement detected
        if (Time.frameCount < 300 || stick.magnitude > 0.1f)
        {
            if (Time.frameCount % 30 == 0 || stick.magnitude > 0.1f)
            {
                Debug.Log($"<color=yellow>GetMoveInput: Stick = ({stick.x:F2}, {stick.y:F2})</color>");
            }
        }
        
        return new Vector3(stick.x, 0, stick.y);
    }

    /// <summary>
    /// Update method - call this from Player_Components.Update() to check for button presses
    /// </summary>
    public void UpdateInput(InputDevice device)
    {
        Gamepad gamepad = device as Gamepad;
        if (gamepad == null) return;

        // Sprint (hold)
        bool isSprintPressed = gamepad.leftTrigger.ReadValue() > 0.5f || 
                               gamepad.rightTrigger.ReadValue() > 0.5f;
        
        if (isSprintPressed && !wasSprintPressed)
        {
            OnAction?.Invoke(ActionState.Sprint);
        }
        else if (!isSprintPressed && wasSprintPressed)
        {
            OnAction?.Invoke(ActionState.Idle);
        }
        wasSprintPressed = isSprintPressed;

        // Crouch (toggle)
        if (gamepad.buttonEast.wasPressedThisFrame) // B button
        {
            OnAction?.Invoke(ActionState.Crouch);
        }

        // Jump
        if (gamepad.buttonSouth.wasPressedThisFrame) // A button
        {
            OnAction?.Invoke(ActionState.Jump);
        }

        // Action1
        if (gamepad.buttonWest.wasPressedThisFrame) // X button
        {
            OnAction?.Invoke(ActionState.Action1);
        }

        // Action2
        if (gamepad.buttonNorth.wasPressedThisFrame) // Y button
        {
            OnAction?.Invoke(ActionState.Action2);
        }

        // Interact
        if (gamepad.leftShoulder.wasPressedThisFrame || 
            gamepad.rightShoulder.wasPressedThisFrame)
        {
            OnAction?.Invoke(ActionState.Interact);
        }
    }

    private void OnDisable()
    {
        // Reset state when disabled
        wasSprintPressed = false;
    }
}
