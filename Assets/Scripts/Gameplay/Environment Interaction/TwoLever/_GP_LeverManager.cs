using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class _GP_LeverManager : MonoBehaviour
{
    public enum LeverState
    {
        idle,
        active,
        finish
    }

    [Header("Player References")]
    [SerializeField] private GameObject playerLever1;
    [SerializeField] private GameObject playerLever2;

    [Header("Lever References")]
    public _GP_Lever lever1Reference;
    public _GP_Lever lever2Reference;

    [Header("Timeline References")]
    public PlayableDirector timelineActive;
    public PlayableDirector timelineFinish;

    [Header("State")]
    public LeverState currentLeverState = LeverState.idle;

    [Header("Events")]
    public UnityEvent onBothLeversActivated;

    void Start()
    {
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
            Debug.Log("Both levers activated! Playing timeline...");
            currentLeverState = LeverState.active;
            PlayTimeline(timelineActive, "ACTIVE");
            onBothLeversActivated?.Invoke();
        }
    }

    public void OnDetachPlayers()
    {
        Debug.Log("OnDetachPlayers called - releasing players from lever interaction");
        
        if (lever1Reference != null)
        {
            lever1Reference.RestorePlayerControl();
        }
        if (lever2Reference != null)
        {
            lever2Reference.RestorePlayerControl();
        }
        
        // Clear player references so next activation can occur
        playerLever1 = null;
        playerLever2 = null;
        
        currentLeverState = LeverState.finish;
        PlayTimeline(timelineFinish, "FINISH");
        Debug.Log("Both players detached from levers, references cleared");
    }

    private void PlayTimeline(PlayableDirector timeline, string stateName)
    {
        if (timeline == null)
        {
            Debug.LogWarning($"Timeline for state '{stateName}' is not assigned.");
            return;
        }
        timeline.Stop();
        timeline.Play();
        Debug.Log($"Lever state: {stateName} - Playing Timeline");
    }
}
