using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class GP_DoorInteract_Mng_Refactored : MonoBehaviour
{
    public enum DoorState
    {
        idle,
        waiting,
        open,
        close
    }
    
    private const int PLAYER_COUNT = 2;
    
    [Header("State")]
    public DoorState currentDoorState = DoorState.idle;

    [Header("Timeline References")]
    public PlayableDirector idleTimeline;
    public PlayableDirector openTimeline;
    public PlayableDirector closeTimeline;

    [Header("Player References")]
    public Player_Components[] playerReferences = new Player_Components[PLAYER_COUNT];

    [Header("Player Interaction Positions")]
    public Transform[] interactPositions = new Transform[PLAYER_COUNT];

    [Header("Door Interaction Icon")]
    public GameObject doorInteractionIcon;

    // Private state tracking (simplified)
    private GameObject[] playersEntered = new GameObject[PLAYER_COUNT];
    private GameObject[] playersInteracted = new GameObject[PLAYER_COUNT];
    private UnityAction<ActionState>[] playerActionDelegates = new UnityAction<ActionState>[PLAYER_COUNT];
    
    private Collider interactCollider;
    private DoorState lastDoorState = DoorState.idle;

    void Start()
    {
        if (doorInteractionIcon != null)
        {
            doorInteractionIcon.SetActive(false);
            if (interactCollider == null)
            {
                interactCollider = GetComponent<Collider>();
            }
        }
    }
    
    void Update()
    {
        // Check for cancel input for players in UI mode
        for (int i = 0; i < PLAYER_COUNT; i++)
        {
            if (IsPlayerInUI(i) && playerReferences[i] != null && CheckCancelInput(playerReferences[i]))
            {
                ExitDoorInteraction(i);
            }
        }

        NormalizePlayersPosition();
    }
    
    private bool CheckCancelInput(Player_Components player)
    {
        if (player.assignedDevice != null)
        {
            var gamepad = player.assignedDevice as UnityEngine.InputSystem.Gamepad;
            if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame)
            {
                return true;
            }
        }
        return false;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        ShowInteractionIcon(true);

        Player_Components playerComponent = other.GetComponent<Player_Components>();
        int playerIndex = GetPlayerIndex(playerComponent);

        if (playerIndex == -1) return;

        // Subscribe to input event
        SubscribeToPlayer(playerIndex, playerComponent);

        // Track player entry
        RegisterPlayerEntry(other.gameObject);

        // Auto-assign second player if first already interacted
        if (playersInteracted[0] != null && playersInteracted[1] == null && other.gameObject != playersInteracted[0])
        {
            playersInteracted[1] = other.gameObject;
            Player_Components secondPlayer = playersInteracted[1].GetComponent<Player_Components>();
            if (secondPlayer != null)
            {
                SwitchPlayerUIMode(1, true);
            }
        }
    }

    public void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NormalizePlayersPosition();
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Player_Components playerComponent = other.GetComponent<Player_Components>();
        int playerIndex = GetPlayerIndex(playerComponent);

        if (playerIndex == -1) return;

        // Unsubscribe from input event
        UnsubscribeFromPlayer(playerIndex);

        // Clear player from tracking
        ClearPlayerFromTracking(other.gameObject);

        // Hide icon if no players in area
        if (playersEntered[0] == null && playersEntered[1] == null)
        {
            ShowInteractionIcon(false);
        }
    }

    public void OnDetachPlayers()
    {
        Debug.Log("OnDetachPlayers called - releasing players from door interaction");
        
        for (int i = 0; i < PLAYER_COUNT; i++)
        {
            if (playersInteracted[i] != null)
            {
                DetachPlayer(i);
            }
        }

        ResetDoorState();
        Debug.Log("Players detached, door reset to idle state");
    }

    private void HandlePlayerAction(ActionState state, int playerIndex)
    {
        if (state != ActionState.Interact || playersInteracted[0] != null) return;

        // First player interaction
        playersInteracted[playerIndex] = playerReferences[playerIndex].gameObject;
        Debug.Log($"Player {playerIndex} interacted with door");
        
        SwitchPlayerUIMode(playerIndex, true);
        Debug.Log($"Player {playerIndex} entered door UI mode. Press B/Circle to exit.");

        // Auto-assign second player if in area
        int otherPlayerIndex = 1 - playerIndex;
        if (playersEntered[otherPlayerIndex] != null)
        {
            playersInteracted[otherPlayerIndex] = playersEntered[otherPlayerIndex];
            Player_Components otherPlayer = playersInteracted[otherPlayerIndex].GetComponent<Player_Components>();
            if (otherPlayer != null)
            {
                SwitchPlayerUIMode(otherPlayerIndex, true);
            }
        }
    }
    
    private void SwitchPlayerUIMode(int playerIndex, bool enableUI)
    {
        if (playerReferences[playerIndex] == null) return;

        UnsubscribeFromPlayer(playerIndex);
        
        playerReferences[playerIndex].enabled = !enableUI;
        
        if (Sys_GameModeSwitch.Instance != null)
        {
            var targetMode = enableUI ? Sys_GameModeSwitch.GameMode.UI : Sys_GameModeSwitch.GameMode.Player;
            Sys_GameModeSwitch.Instance.SetPlayerMode(playerIndex, targetMode);
        }

        if (enableUI)
        {
            SubscribeToPlayer(playerIndex, playerReferences[playerIndex]);
        }
    }
    
    private void ExitDoorInteraction(int playerIndex)
    {
        Debug.Log($"Player {playerIndex} exited door UI mode.");
        SwitchPlayerUIMode(playerIndex, false);
        DetachPlayer(playerIndex);
    }

    private void NormalizePlayersPosition()
    {
        for (int i = 0; i < PLAYER_COUNT; i++)
        {
            if (playersInteracted[i] != null && interactPositions[i] != null)
            {
                NormalizePlayerTransform(i);
                UpdatePlayerState(playersInteracted[i], i, false);
            }
        }

        UpdateDoorState();
    }

    private void NormalizePlayerTransform(int playerIndex)
    {
        Transform playerTransform = playersInteracted[playerIndex].transform;
        
        if (playerTransform.parent != interactPositions[playerIndex])
        {
            playerTransform.SetParent(interactPositions[playerIndex], false);
        }

        if (playerTransform.localPosition != Vector3.zero || playerTransform.localRotation != Quaternion.identity)
        {
            playerTransform.localPosition = Vector3.zero;
            playerTransform.localRotation = Quaternion.identity;
            Debug.Log($"Normalized player {playerIndex} position and rotation to interact point.");
        }
    }

    private void UpdatePlayerState(GameObject player, int playerIndex, bool enabled)
    {
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = enabled;
        }
    }

    private void UpdateDoorState()
    {
        int playersReady = (playersInteracted[0] != null ? 1 : 0) + (playersInteracted[1] != null ? 1 : 0);

        DoorState newState = playersReady switch
        {
            0 => DoorState.idle,
            1 => DoorState.open,
            2 => DoorState.close,
            _ => DoorState.idle
        };

        if (currentDoorState != newState)
        {
            currentDoorState = newState;
            OnDoorState();
        }
    }

    private void OnDoorState()
    {
        Debug.Log($"Door state: {currentDoorState}");
        
        switch (currentDoorState)
        {
            case DoorState.idle:
                PlayTimeline(idleTimeline, "IDLE");
                break;
            case DoorState.open:
                PlayTimeline(openTimeline, "OPEN");
                break;
            case DoorState.close:
                if (interactCollider != null)
                {
                    interactCollider.enabled = false;
                }
                PlayTimeline(closeTimeline, "CLOSE (Timeline Signal will trigger OnDetachPlayers)");
                break;
        }
        
        lastDoorState = currentDoorState;
    }

    private void PlayTimeline(PlayableDirector timeline, string stateName)
    {
        if (timeline == null)
        {
            Debug.LogError($"Door {stateName} Timeline is null!");
            return;
        }
        
        timeline.Play();
        Debug.Log($"Door state: {stateName} - Playing Timeline");
    }

    #region Helper Methods
    
    private int GetPlayerIndex(Player_Components player)
    {
        if (player == playerReferences[0]) return 0;
        if (player == playerReferences[1]) return 1;
        return -1;
    }

    private bool IsPlayerInUI(int playerIndex)
    {
        return playersInteracted[playerIndex] != null;
    }

    private void SubscribeToPlayer(int playerIndex, Player_Components player)
    {
        if (player.moduleInputPlay != null)
        {
            playerActionDelegates[playerIndex] = (state) => HandlePlayerAction(state, playerIndex);
            player.moduleInputPlay.OnAction += playerActionDelegates[playerIndex];
        }
    }

    private void UnsubscribeFromPlayer(int playerIndex)
    {
        if (playerReferences[playerIndex] != null && playerReferences[playerIndex].moduleInputPlay != null && playerActionDelegates[playerIndex] != null)
        {
            playerReferences[playerIndex].moduleInputPlay.OnAction -= playerActionDelegates[playerIndex];
        }
    }

    private void RegisterPlayerEntry(GameObject playerGO)
    {
        if (playersEntered[0] == null)
        {
            playersEntered[0] = playerGO;
            Debug.Log($"Player entered: {playerGO.name}");
        }
        else if (playersEntered[1] == null && playerGO != playersEntered[0])
        {
            playersEntered[1] = playerGO;
            Debug.Log($"Player entered: {playerGO.name}");
        }
    }

    private void ClearPlayerFromTracking(GameObject playerGO)
    {
        if (playersEntered[0] == playerGO)
            playersEntered[0] = null;
        if (playersEntered[1] == playerGO)
            playersEntered[1] = null;
        if (playersInteracted[0] == playerGO)
            playersInteracted[0] = null;
        if (playersInteracted[1] == playerGO)
            playersInteracted[1] = null;
    }

    private void DetachPlayer(int playerIndex)
    {
        GameObject player = playersInteracted[playerIndex];
        if (player == null) return;

        player.transform.SetParent(null);
        
        Player_Components playerComponent = player.GetComponent<Player_Components>();
        if (playerComponent != null)
        {
            playerComponent.enabled = true;
            if (Sys_GameModeSwitch.Instance != null)
            {
                Sys_GameModeSwitch.Instance.SetPlayerMode(playerIndex, Sys_GameModeSwitch.GameMode.Player);
            }
        }

        UpdatePlayerState(player, playerIndex, true);
        playersInteracted[playerIndex] = null;
    }

    private void ResetDoorState()
    {
        playersEntered[0] = null;
        playersEntered[1] = null;
        playersInteracted[0] = null;
        playersInteracted[1] = null;
        currentDoorState = DoorState.idle;
        
        if (interactCollider != null)
        {
            interactCollider.enabled = true;
        }
    }

    private void ShowInteractionIcon(bool show)
    {
        if (doorInteractionIcon != null)
        {
            doorInteractionIcon.SetActive(show);
        }
    }

    #endregion
}
