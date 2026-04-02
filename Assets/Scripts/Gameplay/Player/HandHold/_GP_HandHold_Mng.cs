using System;
using Unity.VisualScripting;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class _GP_HandHold_Mng : MonoBehaviour
{
    public enum RindaState
    {
        None,
        Holding,
        Reaching
    }

    public enum NayaState
    {
        None,
        Holding,
        Reaching
    }

    [Header("HandHold States [DEBUG]")]
    [Tooltip(@"State hand-hold untuk Rinda.
    DEBUG ONLY. DO NOT CHANGE MANUALLY!")]
    public RindaState currentRindaState = RindaState.None;
    [Tooltip(@"State hand-hold untuk Naya.
    DEBUG ONLY. DO NOT CHANGE MANUALLY!")]
    public NayaState currentNayaState = NayaState.None;

    [Header("Bool Flags [DEBUG]")]
    [Tooltip(@"Flag indikator apakah ada obstacle antara Rinda dan Naya.")]
    [SerializeField] private bool isObstacleBetweenPlayers = false;
    [Tooltip(@"Flag indikator apakah Rinda dan Naya berada dalam jarak deteksi hand-hold.")]
    [SerializeField] private bool isPlayersInRange = false;

    [Header("Player Object References")]
    [Tooltip("Referensi Rinda.")]
    public GameObject playerRinda;
    [Tooltip("Referensi komponen Player_Components Rinda.")]
    public Player_Components playerComponentsRinda;
    [Tooltip("Referensi Naya.")]
    public GameObject playerNaya;
    [Tooltip("Referensi komponen Player_Components Naya.")]
    public Player_Components playerComponentsNaya;

    [Header("HandHold Settings")]
    [Tooltip("Referensi Transform object yang akan diikuti gameobject Rinda ketika hand-holding.")]
    public Transform handHoldPivotTransform;
    public float handHoldFollowSpeedXZ = 10f;
    public float handHoldFollowSpeedY = 10f;
    public float handHoldRotationFollowSpeed = 10f;

    [Header("Detection Settings")]
    [Tooltip("Jarak deteksi minimal untuk menentukan apakah pemain sedang berdekatan.")]
    public float handHoldDetectionRange = 2f;

    void Awake()
    {
        if (playerRinda == null || playerNaya == null)
        {
            Debug.LogError("Referensi player belum diatur di _GP_HandHold_Mng.");
        }

        if (handHoldPivotTransform == null)
        {
            Debug.LogError("Referensi handHoldPivotTransform belum diatur di _GP_HandHold_Mng.");
        }
    }

    void OnEnable()
    {
        // Subscribe to Naya input action
        if (playerNaya != null)
        {
            playerComponentsNaya.moduleInputPlay.OnAction += HandleNayaHandHoldAction;
            playerComponentsRinda.moduleInputPlay.OnAction += HandleRindaHandHoldAction;
        }
    }

    void OnDisable()
    {
        if (playerRinda != null)
        {
            playerComponentsNaya.moduleInputPlay.OnAction -= HandleNayaHandHoldAction;
            playerComponentsRinda.moduleInputPlay.OnAction -= HandleRindaHandHoldAction;
        }
    }

    void Update()
    {
        CheckPlayerDistance();
        CheckPlayerLOS();
        OnRindaState();
        OnNayaState();
        HandleHandHoldState();
        OnHandHold();
    }

    #region Controller Methods
    private void HandleRindaHandHoldAction(ActionState actionState)
    {
        if (actionState != ActionState.HandHold) return;

        if (currentRindaState == RindaState.None)
        {
            currentRindaState = RindaState.Reaching;
        }
        else if (currentRindaState == RindaState.Reaching)
        {
            currentRindaState = RindaState.None;
        }
        else if (currentRindaState == RindaState.Holding)
        {
            currentRindaState = RindaState.None;
            CharacterController rindaController = playerRinda.GetComponent<CharacterController>();
            if (rindaController != null)
            {
                rindaController.enabled = true; // Re-enable CharacterController when releasing hand-hold
            }
        }
        // Logika untuk menangani input hand-hold dari Rinda
        Debug.Log("Rinda melakukan aksi hand-hold.");
    }

    private void HandleNayaHandHoldAction(ActionState actionState)
    {
        if (actionState != ActionState.HandHold) return;

        if (currentNayaState == NayaState.None)
        {
            currentNayaState = NayaState.Reaching;
        }
        else if (currentNayaState == NayaState.Reaching)
        {
            currentNayaState = NayaState.None;
        }
        else if (currentNayaState == NayaState.Holding)
        {
            currentNayaState = NayaState.None;
            CharacterController nayaController = playerNaya.GetComponent<CharacterController>();
            if (nayaController != null)
            {
                nayaController.enabled = true; // Re-enable CharacterController when releasing hand-hold
            }
        }
        // Logika untuk menangani input hand-hold dari Naya
        Debug.Log("Naya melakukan aksi hand-hold.");
    }
    #endregion

    #region Hand Holding Methods
    private void HandleHandHoldState()
    {
        if (isPlayersInRange && !isObstacleBetweenPlayers)
        {
            if (currentNayaState == NayaState.None && currentRindaState == RindaState.None)
            {
                Debug.Log("Players in range, no obstacles, but not reaching.");
                return;
            }

            if (currentNayaState == NayaState.Reaching && currentRindaState == RindaState.Reaching)
            {
                currentNayaState = NayaState.Holding;
                currentRindaState = RindaState.Holding;
                Debug.Log("Players are now holding hands.");
            }
        }
    }

    private void OnHandHold()
    {
        if (currentNayaState == NayaState.Holding && currentRindaState == RindaState.Holding)
        {
            // Logika untuk mengatur posisi Rinda mengikuti handHoldPivotTransform saat hand-holding
            if (playerRinda != null && handHoldPivotTransform != null)
            {
                CharacterController rindaController = playerRinda.GetComponent<CharacterController>();
                if (rindaController != null){
                    rindaController.enabled = false; // Disable CharacterController to prevent physics interference
                }
                // Separate XZ and Y movement with independent speeds
                Vector3 currentPos = playerRinda.transform.position;
                Vector3 targetPos = handHoldPivotTransform.position;
                
                // Lerp XZ axes separately
                Vector3 newPosXZ = new Vector3(
                    Mathf.Lerp(currentPos.x, targetPos.x, Time.deltaTime * handHoldFollowSpeedXZ),
                    currentPos.y,
                    Mathf.Lerp(currentPos.z, targetPos.z, Time.deltaTime * handHoldFollowSpeedXZ)
                );
                
                // Lerp Y axis separately
                Vector3 newPos = new Vector3(
                    newPosXZ.x,
                    Mathf.Lerp(currentPos.y, targetPos.y, Time.deltaTime * handHoldFollowSpeedY),
                    newPosXZ.z
                );
                
                playerRinda.transform.position = newPos;
                playerRinda.transform.rotation = Quaternion.Slerp(playerRinda.transform.rotation, handHoldPivotTransform.rotation, Time.deltaTime * handHoldRotationFollowSpeed);
            }
        }
    }
    #endregion

    #region Checking Methods
    private void CheckPlayerDistance()
    {
        if (playerRinda == null || playerNaya == null)
        {
            isPlayersInRange = false;
            return;
        }

        // Optimized distance check using sqrMagnitude
        Vector3 toPlayer = playerRinda.transform.position - playerNaya.transform.position;
        float sqrDistance = toPlayer.sqrMagnitude;
        float sqrRange = handHoldDetectionRange * handHoldDetectionRange;

        if (sqrDistance <= sqrRange)
        {
            isPlayersInRange = true;
            Debug.Log("Pemain berada dalam jarak deteksi untuk hand-hold.");
        }
        else
        {
            isPlayersInRange = false;
            Debug.Log("Pemain terlalu jauh untuk melakukan hand-hold.");
        }
    }

    private void CheckPlayerLOS()
    {
        if (playerRinda == null || playerNaya == null)
        {
            isObstacleBetweenPlayers = true;
            return;
        }

        // Ray origin with 1 unit Y offset
        Vector3 rayOrigin = playerNaya.transform.position + Vector3.up * 1f;
        Vector3 targetPosition = new Vector3(playerRinda.transform.position.x, rayOrigin.y, playerRinda.transform.position.z);
        Vector3 toPlayer = targetPosition - rayOrigin;
        
        // Optimized distance check using sqrMagnitude
        float sqrDistance = toPlayer.sqrMagnitude;
        float sqrRange = handHoldDetectionRange * handHoldDetectionRange;
        
        if (sqrDistance > sqrRange)
        {
            // Outside detection range
            isObstacleBetweenPlayers = true;
            Debug.DrawRay(rayOrigin, toPlayer.normalized * handHoldDetectionRange, Color.red);
            return;
        }

        // Perform raycast
        float actualDistance = Mathf.Sqrt(sqrDistance);
        if (Physics.Raycast(rayOrigin, toPlayer.normalized, out RaycastHit hit, actualDistance))
        {
            // Check if we hit Rinda specifically
            bool hitRinda = hit.collider.gameObject == playerRinda || hit.collider.transform.IsChildOf(playerRinda.transform);

            if (hitRinda && hit.collider.CompareTag("Player"))
            {
                // Check if the player has Player_Components script
                Player_Components playerComponent = hit.collider.GetComponent<Player_Components>();

                if (playerComponent != null)
                {
                    // Clear obstacle flag - line of sight is clear
                    isObstacleBetweenPlayers = false;
                    // Draw debug ray in green when player is detected
                    Debug.DrawRay(rayOrigin, toPlayer.normalized * hit.distance, Color.green);
                    Debug.Log("Rinda memiliki line of sight ke Naya.");
                }
                else
                {
                    // Hit Rinda but without the required script
                    isObstacleBetweenPlayers = true;
                    Debug.DrawRay(rayOrigin, toPlayer.normalized * hit.distance, Color.yellow);
                    Debug.Log("Rinda terdeteksi tapi tanpa komponen yang diperlukan.");
                }
            }
            else
            {
                // Hit something else blocking the line of sight
                isObstacleBetweenPlayers = true;
                Debug.DrawRay(rayOrigin, toPlayer.normalized * hit.distance, Color.red);
                Debug.Log("Rinda tidak memiliki line of sight ke Naya - ada penghalang.");
            }
        }
        else
        {
            // No hit - clear line of sight
            isObstacleBetweenPlayers = false;
            Debug.DrawRay(rayOrigin, toPlayer.normalized * actualDistance, Color.green);
            Debug.Log("Line of sight clear to Rinda.");
        }
    }
    #endregion

    private void OnRindaState()
    {
        // Logika untuk mengelola state hand-hold Rinda
        switch (currentRindaState)
        {
            case RindaState.None:
                // Logika untuk state None
                break;
            case RindaState.Holding:
                // Logika untuk state Holding
                break;
            case RindaState.Reaching:
                // Logika untuk state Reaching
                break;
        }
    }

    private void OnNayaState()
    {
        // Logika untuk mengelola state hand-hold Naya
        switch (currentNayaState)
        {
            case NayaState.None:
                // Logika untuk state None
                break;
            case NayaState.Holding:
                // Logika untuk state Holding
                break;
            case NayaState.Reaching:
                // Logika untuk state Reaching
                break;
        }
    }
}