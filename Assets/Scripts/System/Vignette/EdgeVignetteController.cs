using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class EdgeVignetteZone : MonoBehaviour
{
    [Header("Players")]
    public Transform player1;
    public Transform player2;

    [Header("Settings")]
    [Tooltip("Jarak dari dinding saat vignette mulai muncul")]
    public float warningDistance = 2f;

    [Header("Debug")]
    public bool debug = false;

    [HideInInspector]
    public float CurrentWarning;

    private BoxCollider box;

    void Awake()
    {
        box = GetComponent<BoxCollider>();
    }

    void Update()
    {
        float p1 = GetWarning(player1);
        float p2 = GetWarning(player2);

        // Ambil warning terbesar dari kedua player
        CurrentWarning = Mathf.Max(p1, p2);

        if (debug)
        {
            Debug.Log($"{name} | P1: {p1:F2} | P2: {p2:F2} | Current: {CurrentWarning:F2}");
        }
    }

    float GetWarning(Transform player)
    {
        if (player == null)
            return 0f;

        // Pastikan player benar-benar di dalam collider
        Vector3 closest = box.ClosestPoint(player.position);

        if (closest != player.position)
            return 0f;

        Vector3 local = transform.InverseTransformPoint(player.position);

        Vector3 center = box.center;
        Vector3 half = box.size * 0.5f;

        float leftDistance =
            local.x - (center.x - half.x);

        float rightDistance =
            (center.x + half.x) - local.x;

        float nearestSide = Mathf.Min(leftDistance, rightDistance);

        if (nearestSide < 0f)
            return 0f;

        float warning = Mathf.InverseLerp(
            warningDistance,
            0f,
            nearestSide);

        if (debug)
        {
            Debug.Log(
$@"===== {name} =====
Player : {player.name}
Left   : {leftDistance:F2}
Right  : {rightDistance:F2}
Near   : {nearestSide:F2}
Warn   : {warning:F2}");
        }

        return warning;
    }

    private void OnDrawGizmosSelected()
    {
        if (box == null)
            box = GetComponent<BoxCollider>();

        Gizmos.matrix = transform.localToWorldMatrix;

        // Collider
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(box.center, box.size);

        // Area warning kiri
        Gizmos.color = Color.yellow;

        Vector3 warningSize = new Vector3(
            warningDistance,
            box.size.y,
            box.size.z);

        Vector3 left = box.center;
        left.x = box.center.x - box.size.x * 0.5f + warningDistance * 0.5f;

        Gizmos.DrawWireCube(left, warningSize);

        // Area warning kanan
        Vector3 right = box.center;
        right.x = box.center.x + box.size.x * 0.5f - warningDistance * 0.5f;

        Gizmos.DrawWireCube(right, warningSize);
    }
}