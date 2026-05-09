using Unity.VisualScripting;
using UnityEngine;
using TMPro;

public class _GP_HandHold_Naya : MonoBehaviour
{
    public enum NayaState
    {
        None,
        Holding,
        Reaching,
        Releasing
    }
    public NayaState currentNayaState = NayaState.None;

    [Header("Hand-Hold Settings")]
    [Tooltip("Jarak maksimal untuk hand-holding")]
    public float handHoldRange = 2f; // Jarak maksimal untuk hand-holding

    [Header("References")]
    [Tooltip("Referensi ke script hand-hold Rinda")]
    public _GP_HandHold_Rinda rindaHandHold; // Referensi ke script hand-hold Rinda

    [Header("Handhold Indicator")]
    [Tooltip("Referensi ke indikator hand-hold untuk menunjukkan status hand-hold")]
    public GameObject handHoldIndicator;
    
    [Header("Private References")]
    [Tooltip("[Private] Referensi ke Player_Components untuk mengakses state dan data player")]
    [SerializeField] private Player_Components playerComponents;
    
    [Tooltip("[Private] Referensi ke transform tangan pemain lain untuk hand-holding")]
    [SerializeField] private GameObject otherPlayerTransform;

    [Header("Debug Settings")]
    public TextMeshPro debugText; // Referensi ke TextMeshPro untuk menampilkan debug info
    public bool enableDebugLogs = true; // Aktifkan atau nonaktifkan debug logs

    void Awake()
    {
        if (playerComponents == null)
        {
            playerComponents = GetComponent<Player_Components>();
        }

        if (enableDebugLogs)
        {
            if (handHoldIndicator == null)
            {
                return;
            }
        }
    }

    #region Input Handler
    public void OnEnable()
    {
        // Subscribe to input action
        if (playerComponents != null && playerComponents.moduleInputPlay != null)
        {
            playerComponents.moduleInputPlay.OnAction += HandleHandHoldAction;
            playerComponents.moduleInputPlay.OnAction += HandleBackAction;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Cannot subscribe - Player_Components or ModuleInputPlay is null");
        }
    }

    public void OnDisable()
    {
        // Unsubscribe from input action
        if (playerComponents != null && playerComponents.moduleInputPlay != null)
        {
            playerComponents.moduleInputPlay.OnAction -= HandleHandHoldAction;
            playerComponents.moduleInputPlay.OnAction -= HandleBackAction;
        }
    }

    /// <summary>
    /// Handler untuk input HandHold dari Player_Components
    /// </summary>
    private void HandleHandHoldAction(ActionState action)
    {
        if (action != ActionState.HandHold) return;

        Debug.Log($"{gameObject.name}: HandHold button pressed!");

        if (otherPlayerTransform != null)
        {
            _GP_HandHold_Rinda rindaScript = otherPlayerTransform.GetComponent<_GP_HandHold_Rinda>();
            if (rindaScript != null)
            {
                // If other is reaching, both start holding
                if (rindaScript.currentRindaState == _GP_HandHold_Rinda.RindaState.Reaching)
                {
                    currentNayaState = NayaState.Holding;
                    rindaScript.currentRindaState = _GP_HandHold_Rinda.RindaState.Holding;
                    Debug.Log($"{gameObject.name}: Successfully holding hands with {otherPlayerTransform.name}");
                }
                //
                else if (currentNayaState == NayaState.Reaching)
                {
                    currentNayaState = NayaState.None;
                }
                // Otherwise, just set to reaching
                else
                {
                    currentNayaState = NayaState.Reaching;
                    Debug.Log($"{gameObject.name}: Ready to hold hands with {otherPlayerTransform.name}");
                }
            }
        }
        else
        {
            Debug.Log($"{gameObject.name}: No other player nearby to hold hands with");
        }
    }

    /// <summary>
    /// Handler untuk input Back - releases hand-hold
    /// </summary>
    private void HandleBackAction(ActionState action)
    {
        if (action != ActionState.Cancel) return;

        // Only process if currently holding or reaching
        if (currentNayaState == NayaState.Holding || currentNayaState == NayaState.Reaching)
        {
            Debug.Log($"{gameObject.name}: Back button pressed - releasing hand hold!");

            if (otherPlayerTransform != null)
            {
                _GP_HandHold_Rinda rindaScript = otherPlayerTransform.GetComponent<_GP_HandHold_Rinda>();
                if (rindaScript != null)
                {
                    rindaScript.currentRindaState = _GP_HandHold_Rinda.RindaState.None;
                }
            }

            currentNayaState = NayaState.None;
            Debug.Log($"{gameObject.name}: Released hand hold");
        }
    }
    #endregion

    void Update()
    {
        CheckForOtherPlayer();
        OnNayaState();
    }

    #region Raycast Detection
    /// <summary>
    /// Checks for the other player using raycast toward their actual position
    /// Similar to enemy detection logic in _Enemy_Boss and _Enemy_Mannequin
    /// </summary>
    private void CheckForOtherPlayer()
    {
        // Find all GameObjects with "Player" tag
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        
        Vector3 rayOrigin = transform.position + Vector3.up; // Offset slightly upward (chest height)
        bool foundValidPlayer = false;

        foreach (GameObject player in allPlayers)
        {
            // Skip self
            if (player == gameObject)
                continue;

            // Calculate direction and distance to this player
            Vector3 playerCenter = player.transform.position + Vector3.up;
            Vector3 toPlayer = playerCenter - rayOrigin;
            float distance = toPlayer.magnitude;

            // Check if within handHoldRange
            if (distance > handHoldRange)
            {
                Debug.DrawRay(rayOrigin, toPlayer.normalized * handHoldRange, Color.red);
                continue;
            }

            // Raycast toward the player's actual position
            Vector3 directionToPlayer = toPlayer.normalized;
            
            if (Physics.Raycast(rayOrigin, directionToPlayer, out RaycastHit hit, distance))
            {
                // Check if we hit THIS specific player
                bool hitThisPlayer = hit.collider.gameObject == player || hit.collider.transform.IsChildOf(player.transform);
                
                if (hitThisPlayer && hit.collider.CompareTag("Player"))
                {
                    // Check if the player has _GP_HandHold_Rinda script
                    _GP_HandHold_Rinda rindaScript = hit.collider.GetComponent<_GP_HandHold_Rinda>();
                    
                    if (rindaScript != null)
                    {
                        // Set the other player transform
                        if (otherPlayerTransform != hit.collider.gameObject)
                        {
                            otherPlayerTransform = hit.collider.gameObject;
                        }

                        foundValidPlayer = true;
                        // Draw debug ray in green when player is detected
                        Debug.DrawRay(rayOrigin, directionToPlayer * hit.distance, Color.green);
                        break; // Found valid player, no need to check others
                    }
                    else
                    {
                        // Hit a player but without the required script
                        Debug.DrawRay(rayOrigin, directionToPlayer * hit.distance, Color.yellow);
                    }
                }
                else
                {
                    // Hit something else blocking this player
                    Debug.DrawRay(rayOrigin, directionToPlayer * hit.distance, Color.red);
                }
            }
        }

        // Clear other player if no valid player found
        if (!foundValidPlayer && otherPlayerTransform != null)
        {
            otherPlayerTransform = null;
        }
    }
    #endregion

    #region State Handler
    private void HandleHolding()
    {

    }

    private void HandleReaching()
    {

    }

    private void HandleReleasing()
    {

    }
    #endregion

    #region State Management
    private void OnNayaState()
    {
        switch (currentNayaState)
        {
            case NayaState.None:
                // Logic for when not holding hands
                if (debugText != null){
                    debugText.text = "State: None";
                }
                break;
            case NayaState.Holding:
                HandleHolding();
                if (debugText != null){
                    debugText.text = "State: Holding";
                }
                // Logic for when currently holding hands
                break;
            case NayaState.Reaching:
                HandleReaching();
                if (debugText != null){
                    debugText.text = "State: Reaching";
                }
                // Logic for when reaching to hold hands
                break;
            case NayaState.Releasing:
                HandleReleasing();
                if (debugText != null){
                    debugText.text = "State: Releasing";
                }
                // Logic for when releasing hand hold
                break;
        }
    }
    #endregion
}
