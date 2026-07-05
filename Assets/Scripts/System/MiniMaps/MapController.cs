using UnityEngine;

public class MapController : MonoBehaviour
{
    [Header("References")]
    public Camera mapCamera;
    public Transform playerTransform;
    public GameObject mapUIRoot;

    [Header("Pan")]
    public float panSpeed = 30f;
    public float panDamping = 8f;

    [Header("Pan Limits")]
    public Vector2 panLimit = new Vector2(50f, 50f);
    public Vector3 mapCenter = Vector3.zero;

    [Header("Rotate (Yaw)")]
    public float rotateSpeed = 100f;
    public float rotateDamping = 12f;

    [Header("Pitch")]
    public float pitchSpeed = 60f;
    public float pitchDamping = 10f;
    public float minPitch = 30f;
    public float maxPitch = 88f;
    public float defaultPitch = 55f;

    [Header("Zoom")]
    public float zoomSpeed = 20f;
    public float minZoom = 5f;
    public float maxZoom = 120f;
    public float defaultZoom = 40f;
    public float zoomDamping = 8f;

    [Header("Camera Arm")]
    public float armLength = 80f;

    [Header("Behaviour")]
    public bool pauseGameWhenOpen = true;

    [HideInInspector] public bool isOpen = false;
    [HideInInspector] public bool isUIFocus = false;

    private Vector3 _targetLookAt;
    private float _targetYaw;
    private float _targetPitch;
    private float _targetZoom;
    private bool _isFocused = true;

    private float _currentYaw;
    private float _currentPitch;
    private float _currentZoom;
    private Vector3 _currentLookAt;

    void Start()
    {
        if (mapCamera == null)
            mapCamera = GetComponentInChildren<Camera>();

        _targetLookAt = playerTransform ? playerTransform.position : Vector3.zero;
        _currentLookAt = _targetLookAt;

        _targetYaw = 0f;
        _currentYaw = 0f;

        _targetPitch = defaultPitch;
        _currentPitch = defaultPitch;

        _targetZoom = defaultZoom;
        _currentZoom = defaultZoom;

        if (mapUIRoot) mapUIRoot.SetActive(false);
        if (mapCamera) mapCamera.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (!isOpen) return;

        if (_isFocused && playerTransform != null)
            _targetLookAt = playerTransform.position;

        float dt = Time.unscaledDeltaTime;

        _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, dt * rotateDamping);
        _currentPitch = Mathf.Lerp(_currentPitch, _targetPitch, dt * pitchDamping);
        _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, dt * zoomDamping);
        _currentLookAt = Vector3.Lerp(_currentLookAt, _targetLookAt, dt * panDamping);

        float pitchRad = _currentPitch * Mathf.Deg2Rad;
        float yawRad = _currentYaw * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
            Mathf.Sin(pitchRad),
            Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
        ) * armLength;

        mapCamera.transform.position = _currentLookAt + offset;
        mapCamera.transform.LookAt(_currentLookAt);
        mapCamera.orthographicSize = _currentZoom;
    }

    public void ApplyPan(Vector2 input)
    {
        if (input.sqrMagnitude < 0.01f) return;

        float yawRad = _currentYaw * Mathf.Deg2Rad;

        Vector3 right = new Vector3(Mathf.Cos(yawRad), 0, -Mathf.Sin(yawRad));
        Vector3 forward = new Vector3(Mathf.Sin(yawRad), 0, Mathf.Cos(yawRad));

        float speedMod = _currentZoom / defaultZoom;

        _targetLookAt += (right * input.x + forward * input.y)
                         * panSpeed * speedMod * Time.unscaledDeltaTime;

        _targetLookAt.x = Mathf.Clamp(
            _targetLookAt.x,
            mapCenter.x - panLimit.x,
            mapCenter.x + panLimit.x
        );

        _targetLookAt.z = Mathf.Clamp(
            _targetLookAt.z,
            mapCenter.z - panLimit.y,
            mapCenter.z + panLimit.y
        );

        _isFocused = false;
    }

    public void ApplyRotate(float input)
    {
        _targetYaw += input * rotateSpeed * Time.unscaledDeltaTime;
    }

    public void ApplyPitch(float input)
    {
        _targetPitch = Mathf.Clamp(
            _targetPitch + input * pitchSpeed * Time.unscaledDeltaTime,
            minPitch,
            maxPitch
        );
    }

    public void ApplyZoom(float input)
    {
        _targetZoom = Mathf.Clamp(
            _targetZoom - input * zoomSpeed,
            minZoom,
            maxZoom
        );
    }

    public void FocusOnPlayer()
    {
        _isFocused = true;

        _targetPitch = defaultPitch;
        _targetZoom = defaultZoom;

        if (playerTransform != null)
        {
            _targetLookAt = playerTransform.position;
            _currentLookAt = playerTransform.position;
        }
    }

    public void ToggleMap()
    {
        isOpen = !isOpen;

        if (mapUIRoot) mapUIRoot.SetActive(isOpen);
        if (mapCamera) mapCamera.gameObject.SetActive(isOpen);

        if (isOpen)
        {
            isUIFocus = false;
            FocusOnPlayer();

            if (pauseGameWhenOpen)
                Time.timeScale = 0f;
        }
        else
        {
            isUIFocus = false;

            if (pauseGameWhenOpen)
                Time.timeScale = 1f;
        }
    }

    // dipanggil dari PauseMenuSelectorAsli saat balik dari map
    public void CloseMap()
    {
        isOpen = false;
        isUIFocus = false;

        if (mapUIRoot) mapUIRoot.SetActive(false);
        if (mapCamera) mapCamera.gameObject.SetActive(false);

        if (pauseGameWhenOpen)
            Time.timeScale = 0f; // tetap pause karena masih di pause menu
    }
}