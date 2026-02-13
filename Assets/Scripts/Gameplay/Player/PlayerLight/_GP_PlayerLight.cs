using UnityEngine;
using UnityEngine.Events;

public class _GP_PlayerLight : MonoBehaviour
{
    [Header("Player Light Settings")]
    public Light playerLight;

    [Header("Detection Settings")]
    [Tooltip("Layer mask for line of sight detection")]
    [SerializeField] private LayerMask detectionLayer;
    [Tooltip("Raycast offset from target position")]
    public float raycastOffset = 1f;

    [Header("Events")]
    public UnityEvent onEnemyDetected;

    [Header("References")]
    public _ModuleInputPlay _inputPlayer;

    #region Private Variables
    private bool isEnemyDetected = false;
    private GameObject detectedEnemy = null;
    #endregion

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

    void Update()
    {
        // Only check for enemies when light is enabled
        if (playerLight != null && playerLight.enabled)
        {
            isEnemyDetected = IsEnemyInLOS(out detectedEnemy);
            
            if (isEnemyDetected && detectedEnemy != null)
            {
                // Trigger event when enemy is detected
                onEnemyDetected?.Invoke();
                
                // Call reaction method on the detected enemy (enemy decides behavior based on its type)
                detectedEnemy.GetComponentInChildren<_Enemy_Mannequin>()?.OnLitByPlayerLight();
            }
        }
        else
        {
            isEnemyDetected = false;
            detectedEnemy = null;
        }
    }

    private bool IsEnemyInLOS(out GameObject enemy)
    {
        enemy = null;

        if (playerLight == null || !playerLight.enabled)
            return false;

        // Find all enemies with _Enemy_WeepingAngel component
        _Enemy_Mannequin[] allEnemies = FindObjectsByType<_Enemy_Mannequin>(FindObjectsSortMode.None);
        if (allEnemies.Length == 0)
            return false;

        Vector3 lightPosition = playerLight.transform.position;
        Vector3 lightForward = playerLight.transform.forward;

        // Check each enemy
        foreach (_Enemy_Mannequin potentialEnemy in allEnemies)
        {
            // Aim at enemy's center instead of feet for more reliable detection
            Vector3 enemyCenter = potentialEnemy.transform.position + Vector3.up * raycastOffset;
            Vector3 directionToEnemy = (enemyCenter - lightPosition).normalized;
            float distanceToEnemy = Vector3.Distance(lightPosition, enemyCenter);

            // 1. Distance check - is enemy within light range?
            if (distanceToEnemy > playerLight.range)
                continue;

            // 2. Angle check - is enemy within light cone?
            float angleToEnemy = Vector3.Angle(lightForward, directionToEnemy);
            if (angleToEnemy > playerLight.spotAngle / 2f)
                continue;

            // 3. Raycast check - is there clear line of sight?
            if (Physics.Raycast(lightPosition, directionToEnemy, out RaycastHit hit, distanceToEnemy, detectionLayer))
            {
                if (hit.collider.GetComponent<_Enemy_Mannequin>() != null)
                {
                    Debug.DrawRay(lightPosition, directionToEnemy * distanceToEnemy, Color.green);
                    enemy = potentialEnemy.gameObject;
                    return true;
                }
                else
                {
                    // Hit something else (wall, obstacle)
                    Debug.DrawRay(lightPosition, directionToEnemy * distanceToEnemy, Color.yellow);
                    continue;
                }
            }

            Debug.DrawRay(lightPosition, directionToEnemy * distanceToEnemy, Color.red);
        }

        return false;
    }
}