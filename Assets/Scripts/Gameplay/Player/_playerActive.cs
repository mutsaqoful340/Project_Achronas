using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;


public class _playerActive : MonoBehaviour
{
    // Player Singleton Instance
    public static _playerActive Instance { get; private set; }

    [Header("Character Modules")]
    public _ModuleInputPlay moduleInputPlay;

    [Header("Player States")]
    public bool IsIdle;
    public bool IsFall;
    public bool IsJump;
    public bool IsCrouch;
    public bool IsAction1;
    public bool IsAction2;


    #region Private Variables
    private Animator anim;
    private Vector3 _moveUpdate;
    private float _moveSpeed;
    #endregion

    public void Movement()
    {
        var vector = moduleInputPlay ? moduleInputPlay.MoveHandler.normalized : Vector3.zero;
        IsIdle = (vector.x, vector.z) == (0, 0);
        // anim.SetFloat("AxisX", IsStance ? vector.x : 0f);   // Nilai axis kiri kanan yang digunakan untuk strafing
        // anim.SetFloat("AxisZ", IsStance ? vector.z : 0f);   // Nilai axis depan belakang yang digunakan untuk strafing
        if (IsIdle)
        {
            if (!IsJump)
            {
                _moveSpeed = 0;
                anim.SetFloat("Move", _moveSpeed);
            }
        }
    }

    private IEnumerator JumpCoroutine()
    {
        // Do while character not in jump state
        if (!IsJump && !IsCrouch) {
            IsFall = false;            // Deactivate fall system
            IsJump = true;             // Set state to jump
            anim.SetFloat("Move", 4f); // Change animation to jump action
            if (IsIdle) {
                // Jump action for player in idle state
                _moveUpdate.y += Mathf.Sqrt(270f); // (1f * -3 * gravity = -30f)
            } else {
                // Jump action for player in moving state
                _moveUpdate.y++;
                yield return new WaitForSeconds(0.5f);
            }
            IsFall = true;             // Activate fall system
        }
    }

    #region Action States
    private void Action(ActionState state)
    {
        switch (state)
        {
            case ActionState.Sprint:
                anim.SetFloat("Move", 2f);
                Debug.Log("Sprint Action Triggered");
                break;
            case ActionState.Crouch:
                anim.SetTrigger("IsCrouch");
                Debug.Log("Crouch Action Triggered");
                break;
            case ActionState.Jump:
                StartCoroutine(JumpCoroutine());
                Debug.Log("Jump Action Triggered");
                break;
            case ActionState.Interact:
                Debug.Log("Interact Action Triggered");
                break;
            case ActionState.Action1:
                anim.SetTrigger("IsAction1");
                Debug.Log("Action1 Triggered");
                break;
            case ActionState.Action2:
                anim.SetTrigger("IsAction2");
                Debug.Log("Action2 Triggered");
                break;
        }
    }
    #endregion

    private void Start()
    {
        // Setup Player as Singleton Instance
        Instance = this;

        // Components Setup
        anim = GetComponent<Animator>();

        moduleInputPlay.OnAction = Action;
    }
}
