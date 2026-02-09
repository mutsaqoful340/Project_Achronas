using UnityEngine;

/// <summary>
/// Component yang dipasang pada pointer GameObject untuk mendeteksi collision dengan success zone.
/// Attach script ini pada GameObject pointer yang memiliki Collider2D.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class _RadialQTE_PointerCollider : MonoBehaviour
{
    [Tooltip("Reference ke _RadialQTE parent")]
    public _RadialQTE radialQTE;

    private void Awake()
    {
        // Auto-find parent jika tidak di-assign
        if (radialQTE == null)
        {
            radialQTE = GetComponentInParent<_RadialQTE>();
        }

        Debug.Log($"<color=cyan>╔══════════════════════════════════════╗</color>");
        Debug.Log($"<color=cyan>║ [{gameObject.name}] PointerCollider Setup</color>");
        Debug.Log($"<color=cyan>║ Found parent: {radialQTE != null}</color>");

        // Check Collider2D
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            Debug.Log($"<color=cyan>║ Collider2D: {col.GetType().Name}</color>");
            Debug.Log($"<color=cyan>║   - IsTrigger: {col.isTrigger}</color>");
            Debug.Log($"<color=cyan>║   - Enabled: {col.enabled}</color>");
            Debug.Log($"<color=cyan>║   - Bounds: {col.bounds.size}</color>");
        }
        else
        {
            Debug.LogError($"<color=red>║ ✗✗✗ NO Collider2D found!</color>");
        }

        // Check Rigidbody2D
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.Log($"<color=cyan>║ Rigidbody2D: FOUND</color>");
            Debug.Log($"<color=cyan>║   - BodyType: {rb.bodyType}</color>");
            Debug.Log($"<color=cyan>║   - Simulated: {rb.simulated}</color>");
        }
        else
        {
            Debug.LogError($"<color=red>║ ✗✗✗ NO Rigidbody2D! Add Rigidbody2D (Kinematic) for collisions to work!</color>");
        }

        Debug.Log($"<color=cyan>║ GameObject Active: {gameObject.activeInHierarchy}</color>");
        Debug.Log($"<color=cyan>║ Layer: {LayerMask.LayerToName(gameObject.layer)}</color>");
        Debug.Log($"<color=cyan>║ Position: {transform.position}</color>");
        Debug.Log($"<color=cyan>║ Scale: {transform.lossyScale}</color>");
        Debug.Log($"<color=cyan>╚══════════════════════════════════════╝</color>");
    }

    private void Start()
    {
        // Additional check - look for success zones in scene
        GameObject[] successZones = GameObject.FindGameObjectsWithTag("QTE_SuccessZone");
        Debug.Log($"<color=yellow>[{gameObject.name}] Found {successZones.Length} objects with 'QTE_SuccessZone' tag in scene</color>");
        
        foreach (var zone in successZones)
        {
            Collider2D zoneCol = zone.GetComponent<Collider2D>();
            Debug.Log($"<color=yellow>  - {zone.name}: Collider={zoneCol != null}, IsTrigger={zoneCol?.isTrigger}, Layer={LayerMask.LayerToName(zone.layer)}</color>");
        }
        
        // Start checking for overlaps manually
        InvokeRepeating(nameof(CheckManualOverlap), 0.5f, 0.5f);
    }

    private void CheckManualOverlap()
    {
        Collider2D myCollider = GetComponent<Collider2D>();
        if (myCollider == null) return;

        GameObject[] successZones = GameObject.FindGameObjectsWithTag("QTE_SuccessZone");
        foreach (var zone in successZones)
        {
            Collider2D zoneCollider = zone.GetComponent<Collider2D>();
            if (zoneCollider == null) continue;

            // Manual overlap check
            bool overlapping = myCollider.bounds.Intersects(zoneCollider.bounds);
            if (overlapping)
            {
                float distance = Vector3.Distance(transform.position, zone.transform.position);
                Debug.Log($"<color=magenta>[MANUAL CHECK] {gameObject.name} OVERLAPPING with {zone.name}! Distance: {distance:F2}, but OnTriggerEnter2D not called!</color>");
                
                // Check if layers can collide
                bool canCollide = Physics2D.GetIgnoreLayerCollision(gameObject.layer, zone.layer);
                Debug.Log($"<color=magenta>  Layer collision ignored: {canCollide} (Should be false for collision to work)</color>");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"<color=cyan>[{gameObject.name}] OnTriggerEnter2D called! Other: {other.gameObject.name}, Tag: {other.tag}</color>");
        
        // Cek apakah collision dengan success zone
        if (other.CompareTag("QTE_SuccessZone"))
        {
            if (radialQTE != null)
            {
                radialQTE.OnPointerEnterSuccessZone();
                Debug.Log($"<color=green>✓ Pointer entered success zone - isInSuccessZone set to TRUE</color>");
            }
            else
            {
                Debug.LogError($"<color=red>radialQTE reference is NULL!</color>");
            }
        }
        else
        {
            Debug.Log($"<color=yellow>Tag mismatch! Expected 'QTE_SuccessZone' but got '{other.tag}'</color>");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log($"<color=yellow>[{gameObject.name}] OnTriggerExit2D called! Other: {other.gameObject.name}</color>");
        
        // Cek apakah keluar dari success zone
        if (other.CompareTag("QTE_SuccessZone") && radialQTE != null)
        {
            radialQTE.OnPointerExitSuccessZone();
            Debug.Log($"<color=yellow>✓ Pointer exited success zone - isInSuccessZone set to FALSE</color>");
        }
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(CheckManualOverlap));
    }
}
