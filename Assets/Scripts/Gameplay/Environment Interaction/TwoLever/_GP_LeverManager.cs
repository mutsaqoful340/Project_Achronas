using UnityEngine;
using UnityEngine.Events;

public class _GP_LeverManager : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private GameObject playerLever1;
    [SerializeField] private GameObject playerLever2;

    [Header("Lever References")]
    public _GP_Lever lever1Reference;
    public _GP_Lever lever2Reference;

    [Header("Animator Reference")]
    [SerializeField] private Animator leverAnimator;

    [Header("Events")]
    public UnityEvent onBothLeversActivated;

    void Start()
    {
        leverAnimator = GetComponent<Animator>();
    }

    public void SetPlayerLever(GameObject player)
    {
        if (playerLever1 == null)
        {
            playerLever1 = player;
        }
        else if (playerLever2 == null)
        {
            playerLever2 = player;
        }
        CheckBothLeversActivated();
    }

    private void CheckBothLeversActivated()
    {
        if (playerLever1 != null && playerLever2 != null)
        {
            // Both levers are activated, trigger the desired event
            Debug.Log("Both levers activated! Triggering event...");
            leverAnimator.SetTrigger("IsDetachPlayer");
            onBothLeversActivated?.Invoke();
        }
    }

    public void AnimationFinish()
    {
        lever1Reference.RestorePlayerControl();
        lever2Reference.RestorePlayerControl();
    }
}