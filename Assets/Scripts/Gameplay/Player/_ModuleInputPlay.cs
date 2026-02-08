using UnityEngine;
using UnityEngine.Events;

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

    private InputActions input;

    public Vector3 MoveHandler {
        get {
            var axisX = input.Player.Move.ReadValue<Vector2>().x;
            var axisZ = input.Player.Move.ReadValue<Vector2>().y;
            return new Vector3(axisX, 0, axisZ);
        }
    }

    // public Vector3 LookHandler
    // {
    //     get {
    //     var axisX = input.Player.Look.ReadValue<Vector2>().x;
    //     var axisY = input.Player.Look.ReadValue<Vector2>().y;
    //     return new Vector3(axisX, axisY, 0);
    //     }
    // }

    private void ActionAwake()
    {
        input.Player.Sprint.started += (e) =>
        {
            OnAction?.Invoke(ActionState.Sprint);
        };

        input.Player.Sprint.canceled += (e) =>
        {
            OnAction?.Invoke(ActionState.Idle);
        };

        input.Player.Crouch.performed += (e) =>
        {
            OnAction?.Invoke(ActionState.Crouch);
        };

        input.Player.Jump.performed += (e) =>
        {
            OnAction?.Invoke(ActionState.Jump);
        };

        input.Player.Action1.performed += (e) =>
        {
            OnAction?.Invoke(ActionState.Action1);
        };

        input.Player.Action2.performed += (e) =>
        {
            OnAction?.Invoke(ActionState.Action2);
        };

        input.Player.Interact.performed += (e) =>
        {
            OnAction?.Invoke(ActionState.Interact);
        };

        input.UI.Confirm.performed += (e) =>
        {
            OnAction?.Invoke(ActionState.Confirm);
        };
    }

    private void OnEnable()
    {
        input = new();
        input.Player.Enable(); // Only enable Player action map
        ActionAwake();
    }

    private void OnDisable()
    {
        input.Disable();
    }
}
