using UnityEngine;

/// <summary>
/// Attach ke success zone atau pointer untuk debug collision issues
/// </summary>
public class _RadialQTE_DebugHelper : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool logOnAwake = true;
    public bool logOnTriggerEnter = true;
    public bool logOnTriggerExit = true;

    private void Awake()
    {
        if (!logOnAwake) return;

        Collider2D col = GetComponent<Collider2D>();
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        
        Debug.Log($"<color=cyan>╔══════════════════════════════════════╗</color>");
        Debug.Log($"<color=cyan>║ DEBUG: {gameObject.name}</color>");
        Debug.Log($"<color=cyan>║ Tag: {tag}</color>");
        Debug.Log($"<color=cyan>║ Layer: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer})</color>");
        Debug.Log($"<color=cyan>║ Active: {gameObject.activeInHierarchy}</color>");
        Debug.Log($"<color=cyan>║ Position: {transform.position}</color>");
        Debug.Log($"<color=cyan>║ Rotation: {transform.eulerAngles}</color>");
        Debug.Log($"<color=cyan>║ Scale: {transform.lossyScale}</color>");
        
        if (col != null)
        {
            Debug.Log($"<color=cyan>║ Collider2D: {col.GetType().Name}</color>");
            Debug.Log($"<color=cyan>║   - IsTrigger: {col.isTrigger}</color>");
            Debug.Log($"<color=cyan>║   - Enabled: {col.enabled}</color>");
            Debug.Log($"<color=cyan>║   - Bounds Size: {col.bounds.size}</color>");
            Debug.Log($"<color=cyan>║   - Bounds Center: {col.bounds.center}</color>");
        }
        else
        {
            Debug.Log($"<color=red>║ Has Collider2D: FALSE ✗</color>");
        }
        
        if (rb != null)
        {
            Debug.Log($"<color=cyan>║ Rigidbody2D: YES</color>");
            Debug.Log($"<color=cyan>║   - BodyType: {rb.bodyType}</color>");
            Debug.Log($"<color=cyan>║   - Simulated: {rb.simulated}</color>");
        }
        else
        {
            Debug.Log($"<color=orange>║ Has Rigidbody2D: FALSE (May need one for triggers)</color>");
        }
        
        Debug.Log($"<color=cyan>╚══════════════════════════════════════╝</color>");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!logOnTriggerEnter) return;
        Debug.Log($"<color=green>[{gameObject.name}] TRIGGER ENTER with [{other.gameObject.name}] (Tag: {other.tag})</color>");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!logOnTriggerExit) return;
        Debug.Log($"<color=yellow>[{gameObject.name}] TRIGGER EXIT with [{other.gameObject.name}] (Tag: {other.tag})</color>");
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Uncomment to see continuous collision
        // Debug.Log($"<color=blue>[{gameObject.name}] TRIGGER STAY with [{other.gameObject.name}]</color>");
    }
}
