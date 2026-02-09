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

    private InputActions inputActions;
    private InputDevice assignedDevice;
    private bool isInitialized = false;

    /// <summary>
    /// Initialize InputActions and assign a specific device
    /// </summary>
    public void Initialize(InputDevice device)
    {
        assignedDevice = device;
        
        // Create InputActions if not already created
        if (inputActions == null)
        {
            inputActions = new InputActions();
        }
        
        // Disable all devices first
        InputSystem.DisableDevice(Keyboard.current);
        
        // Set device requirements to only listen to the assigned device
        if (device != null)
        {
            inputActions.devices = new[] { device };
            Debug.Log($"<color=green>_ModuleInputPlay: Initialized with device {device.name} (ID: {device.deviceId})</color>");
        }
        else
        {
            Debug.LogWarning("<color=orange>_ModuleInputPlay: Initialized with NULL device!</color>");
        }
        
        // Subscribe to action events
        SubscribeToActions();
        
        // Enable Player action map
        inputActions.Player.Enable();
        isInitialized = true;
    }

    private void SubscribeToActions()
    {
        // Sprint
        inputActions.Player.Sprint.started += ctx => OnAction?.Invoke(ActionState.Sprint);
        inputActions.Player.Sprint.canceled += ctx => OnAction?.Invoke(ActionState.Idle);
        
        // Crouch
        inputActions.Player.Crouch.performed += ctx => OnAction?.Invoke(ActionState.Crouch);
        
        // Jump
        inputActions.Player.Jump.performed += ctx => OnAction?.Invoke(ActionState.Jump);
        
        // Action1
        inputActions.Player.Action1.performed += ctx => OnAction?.Invoke(ActionState.Action1);
        
        // Action2
        inputActions.Player.Action2.performed += ctx => OnAction?.Invoke(ActionState.Action2);
        
        // Interact
        inputActions.Player.Interact.performed += ctx => OnAction?.Invoke(ActionState.Interact);
    }

    /// <summary>
    /// Get movement input from InputActions (filtered by assigned device)
    /// </summary>
    public Vector3 GetMoveInput(InputDevice device)
    {
        // If not initialized with this device, initialize now
        if (!isInitialized || assignedDevice == null || assignedDevice.deviceId != device.deviceId)
        {
            Initialize(device);
        }
        
        if (inputActions == null || !isInitialized)
        {
            return Vector3.zero;
        }
        
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        return new Vector3(moveInput.x, 0, moveInput.y);
    }

    /// <summary>
    /// Update method - InputActions handles button presses via events
    /// This method exists for compatibility but doesn't need to do anything
    /// </summary>
    public void UpdateInput(InputDevice device)
    {
        // InputActions handles everything via events, but we ensure initialization
        if (!isInitialized || assignedDevice == null || assignedDevice.deviceId != device.deviceId)
        {
            Initialize(device);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (inputActions != null)
        {
            inputActions.Player.Sprint.started -= ctx => OnAction?.Invoke(ActionState.Sprint);
            inputActions.Player.Sprint.canceled -= ctx => OnAction?.Invoke(ActionState.Idle);
            inputActions.Player.Crouch.performed -= ctx => OnAction?.Invoke(ActionState.Crouch);
            inputActions.Player.Jump.performed -= ctx => OnAction?.Invoke(ActionState.Jump);
            inputActions.Player.Action1.performed -= ctx => OnAction?.Invoke(ActionState.Action1);
            inputActions.Player.Action2.performed -= ctx => OnAction?.Invoke(ActionState.Action2);
            inputActions.Player.Interact.performed -= ctx => OnAction?.Invoke(ActionState.Interact);
            
            inputActions.Player.Disable();
            inputActions.Dispose();
            inputActions = null;
        }
        
        assignedDevice = null;
        isInitialized = false;
    }
}
