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
    private GameObject[] playerOldParents = new GameObject[PLAYER_COUNT];  // Track old parents for reparenting on detach
    private Vector3[] playerInteractWorldPos = new Vector3[PLAYER_COUNT];  // Track world position at time of interaction
    private bool[] hasPositionSaved = new bool[PLAYER_COUNT];  // Track whether position was actually saved
    
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

            // Auto-assign second player if first already interacted (robust detection for players already in zone)
            bool someoneInteracted = playersInteracted[0] != null || playersInteracted[1] != null;
            bool notBothInteracted = playersInteracted[0] == null || playersInteracted[1] == null;
            if (someoneInteracted && notBothInteracted && GetInteractedIndex(other.gameObject) == -1)
            {
                int interactedIndex = GetEntryIndex(other.gameObject);
                if (interactedIndex != -1)
                {
                    playersInteracted[interactedIndex] = other.gameObject;
                    Player_Components secondPlayer = playersInteracted[interactedIndex].GetComponent<Player_Components>();
                    if (secondPlayer != null)
                    {
                        int secondPlayerIndex = GetPlayerRefIndex(secondPlayer);
                        if (secondPlayerIndex != -1)
                        {
                            SwitchPlayerUIMode(secondPlayerIndex, true);
                            Debug.Log($"Player {secondPlayerIndex} auto-assigned via OnTriggerStay (was already in zone)");
                        }
                    }
                }
            }
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
                DetachPlayer(i, isCancel: false);  // Normal completion, don't restore cached position
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

        // Only save world position for the first player to interact with door
        bool isFirstPlayer = playersInteracted[0] == null && playersInteracted[1] == null;
        if (isFirstPlayer)
        {
            playerInteractWorldPos[entryIndex] = playerGO.transform.position;
            hasPositionSaved[entryIndex] = true;
            Debug.Log($"Saved first player world position at slot {entryIndex}: {playerInteractWorldPos[entryIndex]}");
        }
        
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
        
        // Disable CharacterController when entering UI mode, re-enable when leaving
        UpdatePlayerCharacterController(playerReferences[playerIndex].gameObject, !enableUI);
        
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
            DetachPlayer(entryIndex, isCancel: true);  // Cancel interaction, restore cached position
            Debug.Log($"DetachPlayer called for entry {entryIndex}. Remaining interacted: P0={playersInteracted[0] != null}, P1={playersInteracted[1] != null}");
            
            // Immediately update door state to play idle timeline when cancelling
            UpdateDoorState();
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
        
        // Save old parent before reparenting (for restoration on detach)
        if (playerTransform.parent != interactPositions[playerIndex])
        {
            playerOldParents[playerIndex] = playerTransform.parent != null ? playerTransform.parent.gameObject : null;
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
        {
            playersInteracted[0] = null;
            playerOldParents[0] = null;
        }
        if (playersInteracted[1] == playerGO)
        {
            playersInteracted[1] = null;
            playerOldParents[1] = null;
        }
    }

    private void DetachPlayer(int entryIndex, bool isCancel = false)
    {
        GameObject player = playersInteracted[entryIndex];
        if (player == null) return;

        // Only restore cached world position if this is a cancel AND we saved a position
        Vector3 positionToRestore = player.transform.position;
        if (isCancel && hasPositionSaved[entryIndex])
        {
            positionToRestore = playerInteractWorldPos[entryIndex];
            Debug.Log($"Cancel detected at slot {entryIndex} - restoring player to cached position: {positionToRestore} (saved flag: {hasPositionSaved[entryIndex]})");
        }
        else if (!isCancel)
        {
            Debug.Log($"Normal completion at slot {entryIndex} - keeping current position: {positionToRestore}");
        }
        else if (isCancel && !hasPositionSaved[entryIndex])
        {
            Debug.Log($"Cancel at slot {entryIndex} but no position was saved, keeping current: {positionToRestore}");
        }

        Quaternion worldRot = player.transform.rotation;

        // Disable CharacterController BEFORE reparenting to allow position changes
        UpdatePlayerCharacterController(player, false);

        // Reparent to old parent (or null if no old parent existed)
        if (playerOldParents[entryIndex] != null)
        {
            player.transform.SetParent(playerOldParents[entryIndex].transform);
        }
        else
        {
            player.transform.SetParent(null);
        }

        // Apply position after reparenting (while CharacterController is disabled)
        player.transform.position = positionToRestore;
        player.transform.rotation = worldRot;
        Debug.Log($"Position applied to player at slot {entryIndex}: {player.transform.position}");
        
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

        // Re-enable CharacterController AFTER position is set
        UpdatePlayerCharacterController(player, true);
        playersInteracted[entryIndex] = null;
        playerOldParents[entryIndex] = null;  // Clear old parent reference
        playerInteractWorldPos[entryIndex] = Vector3.zero;  // Clear saved position
        hasPositionSaved[entryIndex] = false;  // Clear position saved flag
    }

    private void ResetDoorState()
    {
        playersEntered[0] = null;
        playersEntered[1] = null;
        playersInteracted[0] = null;
        playersInteracted[1] = null;
        playerOldParents[0] = null;
        playerOldParents[1] = null;
        playerInteractWorldPos[0] = Vector3.zero;
        playerInteractWorldPos[1] = Vector3.zero;
        hasPositionSaved[0] = false;
        hasPositionSaved[1] = false;
        currentDoorState = DoorState.idle;
        
        if (interactCollider != null)
        {
            interactCollider.enabled = true;
        }

        // Play idle timeline when resetting door state
        OnDoorState();
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