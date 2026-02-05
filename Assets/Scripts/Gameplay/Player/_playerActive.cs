using System;
using UnityEngine;

public class _playerActive : MonoBehaviour
{
    // Player Singleton Instance
    public static _playerActive Instance { get; private set; }

    [Header("Character Modules")]
    public _ModuleInputPlay moduleInputPlay;

    #region Private Variables
    private Animator anim;
    #endregion

    private void Action(ActionState state)
    {
        switch (state)
        {
            case ActionState.Sprint:
                Debug.Log("Sprint Action Triggered");
                break;
            case ActionState.Crouch:
                Debug.Log("Crouch Action Triggered");
                break;
            case ActionState.Jump:
                Debug.Log("Jump Action Triggered");
                break;
            case ActionState.Interact:
                Debug.Log("Interact Action Triggered");
                break;
            case ActionState.Action1:
                Debug.Log("Action1 Triggered");
                break;
            case ActionState.Action2:
                Debug.Log("Action2 Triggered");
                break;
        }
    }
    private void Start()
    {
        // Setup Player as Singleton Instance
        Instance = this;

        // Components Setup
        anim = GetComponent<Animator>();

        moduleInputPlay.OnAction = Action;
    }
}
