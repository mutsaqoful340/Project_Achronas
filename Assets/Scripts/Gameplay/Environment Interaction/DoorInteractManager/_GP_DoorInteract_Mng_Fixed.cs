using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class GP_DoorInteract_Mng_Fixed : MonoBehaviour
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
    public PlayableDirector timelineIdle;
    public PlayableDirector timelineOpen;
    public PlayableDirector timelineClose;

    [Header("Player References")]
    public Player_Components[] playerReferences = new Player_Components[PLAYER_COUNT];

    [Header("Player Position on Door Interaction")]
    public Transform[] interactPositions = new Transform[PLAYER_COUNT];

    [Header("Door Interaction Icon")]
    public GameObject doorInteractionIcon;

    [Header("Key Requirement (Optional)")]
    [Tooltip("Assign a GP_Key if this door requires keys to open. Leave empty for no lock.")]
    public GP_Key requiredKey;
    public UnityEvent onDoorFailedToOpen; // Event triggered when the door fails to open due to missing keys

    // Simplified state tracking
    private GameObject[] playersEntered = new GameObject[PLAYER_COUNT];
    private GameObject[] playersInteracted = new GameObject[PLAYER_COUNT];
    private UnityAction<ActionState>[] playerActionDelegates = new UnityAction<ActionState>[PLAYER_COUNT];
    
    private Collider interactCollider;

    void Start()
    {
        if (doorInteractionIcon != null)
        {
            doorInteractionIcon.SetActive(false);
        }
        
        if (interactCollider == null)
        {
            interactCollider = GetComponent<Collider>();
        }
    }
    
    void Update()
    {
        // Check for cancel input for players in UI mode
        for (int i = 0; i < PLAYER_COUNT; i++)
        {
            if (IsPlayerInUI(i) && playerReferences[i] != null && CheckCancelInput(playerReferences[i]))
            {
                // Only allow cancel if the other player hasn't interacted yet (prevents mid-sequence exit)
                int playersInteractedCount = (playersInteracted[0] != null ? 1 : 0) + (playersInteracted[1] != null ? 1 : 0);
                if (playersInteractedCount < 2)
                {
                    ExitDoorInteraction(i);
                }
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
        int playerIndex = GetPlayerRefIndex(playerComponent);

        if (playerIndex == -1) return;

        playerComponent.SetNearInteraction(true);
        // Subscribe to input event
        SubscribeToPlayer(playerIndex, playerComponent);

        // Track player entry
        RegisterPlayerEntry(other.gameObject);

        // Auto-assign second player if first already interacted
        bool someoneInteracted = playersInteracted[0] != null || playersInteracted[1] != null;
        bool notBothInteracted = playersInteracted[0] == null || playersInteracted[1] == null;
        if (someoneInteracted && notBothInteracted && GetInteractedIndex(other.gameObject) == -1)
        {
            int entryIndex = GetEntryIndex(other.gameObject);
            if (entryIndex != -1)
            {
                playersInteracted[entryIndex] = other.gameObject;
                Player_Components secondPlayer = playersInteracted[entryIndex].GetComponent<Player_Components>();
                if (secondPlayer != null)
                {
                    int secondPlayerIndex = GetPlayerRefIndex(secondPlayer);
                    if (secondPlayerIndex != -1)
                    {
                        SwitchPlayerUIMode(secondPlayerIndex, true);
                    }
                }
            }
        }
    }

    public void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Foolproof player registration - handles teleport/respawn cases
        Player_Components playerComponent = other.GetComponent<Player_Components>();
        int playerIndex = GetPlayerRefIndex(playerComponent);

        if (playerIndex != -1)
        {
            // Ensure player is registered even if they were teleported here
            int entryIndex = GetEntryIndex(other.gameObject);
            if (entryIndex == -1)
            {
                RegisterPlayerEntry(other.gameObject);
                Debug.Log($"Player {playerIndex} registered via OnTriggerStay (likely teleported/respawned)");
            }

            // Ensure subscription exists (in case it was lost during respawn)
            if (playerActionDelegates[playerIndex] == null)
            {
                SubscribeToPlayer(playerIndex, playerComponent);
                Debug.Log($"Player {playerIndex} re-subscribed via OnTriggerStay");
            }

            // Show icon if not already shown
            ShowInteractionIcon(true);
        }

        NormalizePlayersPosition();
    }

    public void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Player_Components playerComponent = other.GetComponent<Player_Components>();
        int playerIndex = GetPlayerRefIndex(playerComponent);

        if (playerIndex == -1) return;

        playerComponent.SetNearInteraction(false);
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

    private void HandlePlayerAction(ActionState state, int playerRefIndex)
    {
        if (state != ActionState.Interact || (playersInteracted[0] != null && playersInteracted[1] != null)) return;

        if (requiredKey != null && !requiredKey.isCollected)
        {
            onDoorFailedToOpen?.Invoke();
            GP_Notification notification = FindAnyObjectByType<GP_Notification>();
            if (notification != null)
            {
                notification.OnShowNotification("Door is locked! You need a key to open it.");
            }
            else
            {
                Debug.LogWarning("GP_Notification instance not found in the scene. Cannot show notification.");
            }
            return;
        }

        GameObject playerGO = playerReferences[playerRefIndex].gameObject;
        int entryIndex = GetEntryIndex(playerGO);
        if (entryIndex == -1) return;

        // Only lock the interacting player — the other player remains free to walk up
        playersInteracted[entryIndex] = playerGO;
        SwitchPlayerUIMode(playerRefIndex, true);
        Debug.Log($"Player {playerRefIndex} interacted with door → interact position [{entryIndex}]. Waiting for other player...");
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
            playerReferences[playerIndex].CurrentState(ActionState.InCutscene);
            SubscribeToPlayer(playerIndex, playerReferences[playerIndex]);
        }
        else
        {
            playerReferences[playerIndex].CurrentState(ActionState.Idle);
        }
    }
    
    private void ExitDoorInteraction(int playerRefIndex)
    {
        Debug.Log($"Player {playerRefIndex} exited door UI mode.");
        SwitchPlayerUIMode(playerRefIndex, false);

        int entryIndex = GetInteractedIndex(playerReferences[playerRefIndex].gameObject);
        if (entryIndex != -1)
        {
            DetachPlayer(entryIndex);
            Debug.Log($"DetachPlayer called for entry {entryIndex}. Remaining interacted: P0={playersInteracted[0] != null}, P1={playersInteracted[1] != null}");
        }
    }

    private void NormalizePlayersPosition()
    {
        for (int i = 0; i < PLAYER_COUNT; i++)
        {
            if (playersInteracted[i] != null && interactPositions[i] != null)
            {
                NormalizePlayerTransform(i);
                UpdatePlayerCharacterController(playersInteracted[i], false);
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

    private void UpdatePlayerCharacterController(GameObject player, bool enabled)
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
            Debug.Log($"Door state changing: {currentDoorState} -> {newState} (playersReady: {playersReady})");
            currentDoorState = newState;
            OnDoorState();
        }
    }

    private void OnDoorState()
    {
        Debug.Log($"OnDoorState called with state: {currentDoorState}");
        
        switch (currentDoorState)
        {
            case DoorState.idle:
                PlayTimeline(timelineIdle, "IDLE");
                break;
            case DoorState.waiting:
                // Waiting is handled by open state for now
                break;
            case DoorState.open:
                PlayTimeline(timelineOpen, "OPEN");
                doorInteractionIcon.SetActive(false);
                break;
            case DoorState.close:
                if (interactCollider != null)
                {
                    interactCollider.enabled = false;
                }
                PlayTimeline(timelineClose, "CLOSE");
                break;
        }
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
        Debug.Log($"Door state: {stateName} - Playing Timeline");
    }

    #region Helper Methods
    
    private int GetPlayerRefIndex(Player_Components player)
    {
        if (player == playerReferences[0]) return 0;
        if (player == playerReferences[1]) return 1;
        return -1;
    }

    private int GetEntryIndex(GameObject playerGO)
    {
        if (playersEntered[0] == playerGO) return 0;
        if (playersEntered[1] == playerGO) return 1;
        return -1;
    }

    private int GetInteractedIndex(GameObject playerGO)
    {
        if (playersInteracted[0] == playerGO) return 0;
        if (playersInteracted[1] == playerGO) return 1;
        return -1;
    }

    private bool IsPlayerInUI(int playerRefIndex)
    {
        if (playerReferences[playerRefIndex] == null) return false;
        return GetInteractedIndex(playerReferences[playerRefIndex].gameObject) != -1;
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
            Debug.Log($"First player entered: {playerGO.name}");
        }
        else if (playersEntered[1] == null && playerGO != playersEntered[0])
        {
            playersEntered[1] = playerGO;
            Debug.Log($"Second player entered: {playerGO.name}");
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

    private void DetachPlayer(int entryIndex)
    {
        GameObject player = playersInteracted[entryIndex];
        if (player == null) return;

        player.transform.SetParent(null);
        
        Player_Components playerComponent = player.GetComponent<Player_Components>();
        if (playerComponent != null)
        {
            playerComponent.CurrentState(ActionState.Idle);
            playerComponent.enabled = true;
            int refIndex = GetPlayerRefIndex(playerComponent);
            if (Sys_GameModeSwitch.Instance != null && refIndex != -1)
            {
                Sys_GameModeSwitch.Instance.SetPlayerMode(refIndex, Sys_GameModeSwitch.GameMode.Player);
            }
        }

        UpdatePlayerCharacterController(player, true);
        playersInteracted[entryIndex] = null;
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