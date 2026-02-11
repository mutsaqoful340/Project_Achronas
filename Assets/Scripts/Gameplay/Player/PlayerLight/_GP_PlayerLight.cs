using UnityEngine;

public class _GP_PlayerLight : MonoBehaviour
{
    public _ModuleInputPlay _inputPlayer;
    public Light playerLight;

    private void OnEnable()
    {
        if (_inputPlayer != null)
        {
            _inputPlayer.OnAction += HandleAction;
        }
    }

    private void OnDisable()
    {
        if (_inputPlayer != null)
        {
            _inputPlayer.OnAction -= HandleAction;
        }
    }

    void Start()
    {
        playerLight.enabled = false;
    }

    private void HandleAction(ActionState state)
    {
        if (state == ActionState.Action2)
        {
            if (playerLight != null)
            {
                playerLight.enabled = !playerLight.enabled;
            }
        }
    }
}