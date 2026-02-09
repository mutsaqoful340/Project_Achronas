using UnityEngine;

/// <summary>
/// Comprehensive diagnostic for 2D collision issues.
/// Attach this to the POINTER GameObject to diagnose why collisions aren't working.
/// </summary>
public class _RadialQTE_CollisionDiagnostic : MonoBehaviour
{
    [Header("References")]
    public GameObject successZone;

    private Collider2D myCollider;
    private Collider2D zoneCollider;
    private Rigidbody2D myRigidbody;

    private void Start()
    {
        RunDiagnostic();
    }

    [ContextMenu("Run Diagnostic")]
    public void RunDiagnostic()
    {
        Debug.Log($"<color=yellow>╔══════════════════════════════════════════════════════╗</color>");
        Debug.Log($"<color=yellow>║ COLLISION DIAGNOSTIC for {gameObject.name}</color>");
        Debug.Log($"<color=yellow>╠══════════════════════════════════════════════════════╣</color>");

        // Step 1: Check this GameObject
        myCollider = GetComponent<Collider2D>();
        myRigidbody = GetComponent<Rigidbody2D>();

        Debug.Log($"<color=white>1. POINTER SETUP ({gameObject.name}):</color>");
        
        if (myCollider == null)
        {
            Debug.LogError($"<color=red>   ✗ NO COLLIDER2D! Add CircleCollider2D or BoxCollider2D</color>");
        }
        else
        {
            Debug.Log($"<color=lime>   ✓ Collider2D: {myCollider.GetType().Name}</color>");
            Debug.Log($"   - IsTrigger: {myCollider.isTrigger} {(myCollider.isTrigger ? "✓" : "✗ MUST BE TRUE!")}");
            Debug.Log($"   - Enabled: {myCollider.enabled} {(myCollider.enabled ? "✓" : "✗")}");
            Debug.Log($"   - Bounds: {myCollider.bounds.size}");
            
            if (myCollider.bounds.size.x < 0.01f || myCollider.bounds.size.y < 0.01f)
            {
                Debug.LogError($"<color=red>   ✗ COLLIDER TOO SMALL! Size: {myCollider.bounds.size}</color>");
            }
        }

        if (myRigidbody == null)
        {
            Debug.LogError($"<color=red>   ✗ NO RIGIDBODY2D! Add Rigidbody2D with BodyType=Kinematic</color>");
            Debug.LogError($"<color=red>     At least ONE object needs Rigidbody2D for trigger detection!</color>");
        }
        else
        {
            Debug.Log($"<color=lime>   ✓ Rigidbody2D found</color>");
            Debug.Log($"   - BodyType: {myRigidbody.bodyType} {(myRigidbody.bodyType == RigidbodyType2D.Kinematic ? "✓" : "(Consider Kinematic)")}");
            Debug.Log($"   - Simulated: {myRigidbody.simulated} {(myRigidbody.simulated ? "✓" : "✗ MUST BE TRUE!")}");
        }

        Debug.Log($"   - Layer: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer})");
        Debug.Log($"   - Active: {gameObject.activeInHierarchy} {(gameObject.activeInHierarchy ? "✓" : "✗")}");
        Debug.Log($"   - Position: {transform.position}");

        // Step 2: Check Success Zone
        Debug.Log($"<color=white>2. SUCCESS ZONE SETUP:</color>");
        
        if (successZone == null)
        {
            Debug.LogError($"<color=red>   ✗ Success Zone reference is NULL! Assign it in inspector</color>");
            
            // Try to find by tag
            GameObject[] zones = GameObject.FindGameObjectsWithTag("QTE_SuccessZone");
            Debug.Log($"   Found {zones.Length} objects with 'QTE_SuccessZone' tag");
            if (zones.Length > 0)
            {
                successZone = zones[0];
                Debug.Log($"<color=yellow>   Using first found: {successZone.name}</color>");
            }
        }

        if (successZone != null)
        {
            Debug.Log($"<color=lime>   ✓ Success Zone: {successZone.name}</color>");
            
            zoneCollider = successZone.GetComponent<Collider2D>();
            if (zoneCollider == null)
            {
                Debug.LogError($"<color=red>   ✗ NO COLLIDER2D on success zone!</color>");
            }
            else
            {
                Debug.Log($"<color=lime>   ✓ Collider2D: {zoneCollider.GetType().Name}</color>");
                Debug.Log($"   - IsTrigger: {zoneCollider.isTrigger} {(zoneCollider.isTrigger ? "✓" : "✗ MUST BE TRUE!")}");
                Debug.Log($"   - Enabled: {zoneCollider.enabled} {(zoneCollider.enabled ? "✓" : "✗")}");
                Debug.Log($"   - Bounds: {zoneCollider.bounds.size}");
            }

            Rigidbody2D zoneRb = successZone.GetComponent<Rigidbody2D>();
            if (zoneRb != null)
            {
                Debug.Log($"   ✓ Has Rigidbody2D");
            }
            else
            {
                Debug.Log($"   - No Rigidbody2D (OK if pointer has one)");
            }

            Debug.Log($"   - Tag: {successZone.tag} {(successZone.CompareTag("QTE_SuccessZone") ? "✓" : "✗ WRONG TAG!")}");
            Debug.Log($"   - Layer: {LayerMask.LayerToName(successZone.layer)} ({successZone.layer})");
            Debug.Log($"   - Active: {successZone.activeInHierarchy} {(successZone.activeInHierarchy ? "✓" : "✗")}");
            Debug.Log($"   - Position: {successZone.transform.position}");
        }

        // Step 3: Check Layer Collision Matrix
        Debug.Log($"<color=white>3. LAYER COLLISION CHECK:</color>");
        
        if (myCollider != null && zoneCollider != null)
        {
            bool layerIgnored = Physics2D.GetIgnoreLayerCollision(gameObject.layer, successZone.layer);
            
            if (layerIgnored)
            {
                Debug.LogError($"<color=red>   ✗ LAYERS CANNOT COLLIDE!</color>");
                Debug.LogError($"<color=red>     {LayerMask.LayerToName(gameObject.layer)} cannot collide with {LayerMask.LayerToName(successZone.layer)}</color>");
                Debug.LogError($"<color=red>     Fix: Edit → Project Settings → Physics 2D → Layer Collision Matrix</color>");
            }
            else
            {
                Debug.Log($"<color=lime>   ✓ Layers CAN collide</color>");
                Debug.Log($"     {LayerMask.LayerToName(gameObject.layer)} ↔ {LayerMask.LayerToName(successZone.layer)}");
            }
        }

        // Step 4: Check Distance
        Debug.Log($"<color=white>4. DISTANCE CHECK:</color>");
        
        if (successZone != null)
        {
            float distance = Vector3.Distance(transform.position, successZone.transform.position);
            Debug.Log($"   Distance: {distance:F3}");
            
            if (myCollider != null && zoneCollider != null)
            {
                float maxCollisionDistance = (myCollider.bounds.extents.magnitude + zoneCollider.bounds.extents.magnitude);
                Debug.Log($"   Max collision range: {maxCollisionDistance:F3}");
                
                if (distance < maxCollisionDistance)
                {
                    Debug.Log($"<color=lime>   ✓ Objects are within collision range</color>");
                    
                    // Check if bounds actually overlap
                    bool overlapping = myCollider.bounds.Intersects(zoneCollider.bounds);
                    Debug.Log($"   Bounds overlapping: {overlapping} {(overlapping ? "✓" : "✗")}");
                }
                else
                {
                    Debug.Log($"<color=yellow>   ⚠ Objects are far apart, will only collide when pointer rotates</color>");
                }
            }
        }

        // Step 5: Check Z Position (common issue)
        Debug.Log($"<color=white>5. Z-POSITION CHECK:</color>");
        
        if (successZone != null)
        {
            float zDiff = Mathf.Abs(transform.position.z - successZone.transform.position.z);
            Debug.Log($"   Pointer Z: {transform.position.z:F3}");
            Debug.Log($"   Zone Z: {successZone.transform.position.z:F3}");
            Debug.Log($"   Difference: {zDiff:F3}");
            
            if (zDiff > 0.1f)
            {
                Debug.LogWarning($"<color=orange>   ⚠ Z positions differ significantly!</color>");
                Debug.LogWarning($"<color=orange>     For 2D collisions, Z should typically be the same (e.g., both 0)</color>");
            }
            else
            {
                Debug.Log($"<color=lime>   ✓ Z positions are similar</color>");
            }
        }

        Debug.Log($"<color=yellow>╚══════════════════════════════════════════════════════╝</color>");
        
        // Summary
        int errors = 0;
        if (myCollider == null) errors++;
        if (myRigidbody == null) errors++;
        if (zoneCollider == null) errors++;
        if (myCollider != null && !myCollider.isTrigger) errors++;
        if (zoneCollider != null && !zoneCollider.isTrigger) errors++;
        
        if (errors == 0)
        {
            Debug.Log($"<color=lime>★ DIAGNOSTIC COMPLETE: No obvious issues found!</color>");
            Debug.Log($"<color=yellow>If collisions still don't work, check:</color>");
            Debug.Log($"<color=yellow>- Is the pointer actually rotating/moving?</color>");
            Debug.Log($"<color=yellow>- Are the objects on screen and visible?</color>");
            Debug.Log($"<color=yellow>- Try adding _RadialQTE_DebugHelper to both objects</color>");
        }
        else
        {
            Debug.LogError($"<color=red>✗ FOUND {errors} CRITICAL ISSUES! Fix them above.</color>");
        }
    }
}
