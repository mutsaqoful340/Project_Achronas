using UnityEngine;

public class GP_PlayerCarrySystem : MonoBehaviour
{
    private Player_Components playerComponent;
    private Animator animator;

    void Start()
    {
        if (playerComponent == null)
        {
            playerComponent = GetComponent<Player_Components>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    // public void Balance
}