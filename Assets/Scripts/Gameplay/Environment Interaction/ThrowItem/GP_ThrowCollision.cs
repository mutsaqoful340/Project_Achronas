using UnityEngine;
using UnityEngine.Events;

public class GP_ThrowCollision : MonoBehaviour
{
    public UnityEvent onProjectileHit;
    public string targetTag;

    public void OnCollisionEnter(Collision collision)
    {
        // Check if what hit us is a thrown projectile
        if (collision.gameObject.CompareTag(targetTag))
        {
            HandleProjectileHit(collision);
            onProjectileHit.Invoke();
        }
    }

    private void HandleProjectileHit(Collision collision)
    {
        // Apply damage, effects, etc. to THIS object (target)
        Debug.Log("Target hit by: " + collision.gameObject.name);
        
        // Example: take damage
        // TakeDamage(10);
        
        // Example: trigger animation
        // PlayHitAnimation();
        
        // Example: apply knockback
        // ApplyKnockback(collision);
    }
}
