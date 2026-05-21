using UnityEngine;

public enum MarkerType { Player, Enemy, Objective, Item, Checkpoint }

public class MapMarker : MonoBehaviour
{
    [Header("Settings")]
    public MarkerType markerType = MarkerType.Player;
    public GameObject markerPrefab;
    public float heightOffset = 1.5f;
    public bool billboard = true;

    [Header("Pulse (untuk Objective)")]
    public bool doPulse = false;
    public float pulseSpeed = 2f;
    public float pulseMinScale = 0.8f;
    public float pulseMaxScale = 1.2f;

    private GameObject _instance;
    private Camera _mapCam;

    void Start()
    {
        var ctrl = FindObjectOfType<MapController>();
        if (ctrl != null) _mapCam = ctrl.mapCamera;
        SpawnMarker();
    }

    void SpawnMarker()
    {
        if (markerPrefab == null) return;
        _instance = Instantiate(markerPrefab,
            transform.position + Vector3.up * heightOffset,
            Quaternion.identity);
        SetLayerRecursive(_instance, LayerMask.NameToLayer("Minimap"));
    }

    void LateUpdate()
    {
        if (_instance == null) return;

        _instance.transform.position = transform.position + Vector3.up * heightOffset;

        if (billboard && _mapCam != null)
        {
            Vector3 dir = _instance.transform.position - _mapCam.transform.position;
            if (dir != Vector3.zero)
                _instance.transform.rotation = Quaternion.LookRotation(dir);
        }

        if (doPulse)
        {
            float s = Mathf.Lerp(pulseMinScale, pulseMaxScale,
                      (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            _instance.transform.localScale = Vector3.one * s;
        }
    }

    void OnDestroy()
    {
        if (_instance != null) Destroy(_instance);
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}