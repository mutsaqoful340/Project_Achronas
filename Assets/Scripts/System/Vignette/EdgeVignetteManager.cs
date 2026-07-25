using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class EdgeVignetteManager : MonoBehaviour
{
    [Header("Volume")]
    public Volume volume;

    [Header("Wall Vignette")]
    [Range(0f, 1f)]
    public float maxIntensity = 0.45f;

    public float smoothSpeed = 8f;

    [Header("Player Distance")]
    public Transform player1;
    public Transform player2;

    [Tooltip("Mulai muncul vignette ketika jarak pemain mencapai nilai ini")]
    public float playerDistanceThreshold = 15f;

    [Tooltip("Jarak maksimal untuk vignette penuh")]
    public float playerDistanceMax = 35f;

    [Range(0f, 1f)]
    public float playerDistanceIntensity = 0.30f;

    [Header("Debug")]
    public bool debug = false;

    private Vignette vignette;
    private EdgeVignetteZone[] zones;

    private float current;

    void Awake()
    {
        if (volume == null)
        {
            Debug.LogError("Volume belum diassign!");
            enabled = false;
            return;
        }

        if (!volume.profile.TryGet(out vignette))
        {
            Debug.LogError("Vignette tidak ditemukan pada Volume Profile!");
            enabled = false;
            return;
        }

        zones = FindObjectsByType<EdgeVignetteZone>(FindObjectsSortMode.None);
    }

    void Update()
    {
        // -----------------------------
        // WALL WARNING
        // -----------------------------
        float wallWarning = 0f;

        foreach (var zone in zones)
        {
            wallWarning = Mathf.Max(wallWarning, zone.CurrentWarning);
        }

        float wallEffect = wallWarning * maxIntensity;

        // -----------------------------
        // PLAYER DISTANCE WARNING
        // -----------------------------
        float distanceEffect = 0f;

        if (player1 != null && player2 != null)
        {
            float distance = Vector3.Distance(
                player1.position,
                player2.position);

            float distanceWarning = Mathf.InverseLerp(
                playerDistanceThreshold,
                playerDistanceMax,
                distance);

            distanceEffect = distanceWarning * playerDistanceIntensity;

            if (debug)
            {
                Debug.Log(
                    $"Player Distance : {distance:F2}\n" +
                    $"Distance Warning : {distanceWarning:F2}");
            }
        }

        // -----------------------------
        // FINAL TARGET
        // -----------------------------
        float target = Mathf.Clamp01(
            wallEffect + distanceEffect);

        current = Mathf.Lerp(
            current,
            target,
            Time.deltaTime * smoothSpeed);

        vignette.intensity.value = current;

        if (debug)
        {
            Debug.Log(
                $"Wall Effect : {wallEffect:F2}\n" +
                $"Distance Effect : {distanceEffect:F2}\n" +
                $"Target : {target:F2}\n" +
                $"Current : {current:F2}");
        }
    }
}