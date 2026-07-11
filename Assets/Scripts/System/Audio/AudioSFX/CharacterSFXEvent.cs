using UnityEngine;

public class CharacterSFXEvent : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Footstep Sounds")]
    [SerializeField] private AudioClip[] concreteFootsteps;
    [SerializeField] private AudioClip[] woodFootsteps;
    [SerializeField] private AudioClip[] marbleFootsteps;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float rayDistance = 1f;

    public void PlayFootstep()
    {
        Debug.Log("1. Footstep Event");

        if (groundCheck == null)
        {
            Debug.LogError("GroundCheck belum diisi!");
            return;
        }

        Vector3 origin = groundCheck.position + Vector3.up * 0.2f;

        Debug.Log("2. GroundCheck : " + origin);

        // Debug Sphere
        Collider[] nearbyColliders = Physics.OverlapSphere(origin, 1f);

        Debug.Log("3. Collider sekitar : " + nearbyColliders.Length);

        foreach (Collider col in nearbyColliders)
        {
            Debug.Log("Nearby : " + col.name);
        }

        // Debug Ray
        Debug.DrawRay(origin, Vector3.down * rayDistance, Color.red, 2f);

        bool hitSomething = Physics.SphereCast(
            origin,
            0.15f,
            Vector3.down,
            out RaycastHit hit,
            rayDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        Debug.Log("4. SphereCast Result : " + hitSomething);

        if (!hitSomething)
        {
            Debug.LogWarning("SphereCast tidak kena apa-apa");
            return;
        }

        Debug.Log("5. Hit : " + hit.collider.name);

        Surface surface = hit.collider.GetComponentInParent<Surface>();

        if (surface == null)
        {
            Debug.LogWarning("Surface tidak ditemukan");
            return;
        }

        Debug.Log("6. Surface : " + surface.surfaceType);

        switch (surface.surfaceType)
        {
            case SurfaceType.Concrete:
                PlayRandomClip(concreteFootsteps);
                break;

            case SurfaceType.Wood:
                PlayRandomClip(woodFootsteps);
                break;

            case SurfaceType.Marble:
                PlayRandomClip(marbleFootsteps);
                break;
        }
    }

    private void PlayRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("AudioClip belum diisi!");
            return;
        }

        audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Vector3 origin = groundCheck.position + Vector3.up * 0.2f;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + Vector3.down * rayDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(origin, 0.15f);
    }
}