using UnityEngine;
using UnityEngine.InputSystem;

public class MapInputHandler : MonoBehaviour
{
    [Header("References")]
    public MapController mapController;

    [Header("Sensitivities - Keyboard/Mouse")]
    public float panKeySensitivity = 1f;
    public float panDragSensitivity = 0.25f;
    public float rotateSensitivity = 1f;
    public float pitchDragSensitivity = 0.3f;
    public float scrollSensitivity = 4f;

    [Header("Sensitivities - Gamepad")]
    public float gamepadPanSensitivity = 1f;
    public float gamepadRotateSensitivity = 1f;
    public float gamepadPitchSensitivity = 1f;
    public float gamepadZoomSensitivity = 1f;
    public float stickDeadzone = 0.15f;

    private Vector3 _lastMousePos;
    private bool _isPanDragging;
    private bool _isPitchDragging;

    void Update()
    {
        if (mapController == null) return;

        HandleToggleAndFocus();

        if (!mapController.isOpen) return;

        HandleKeyboardPan();
        HandleMiddleMousePan();
        HandleRightMousePitch();
        HandleRotate();
        HandleZoom();

        HandleGamepad();
    }

    void HandleToggleAndFocus()
    {
        // Keyboard
        if (Keyboard.current != null)
        {
            if (Keyboard.current.mKey.wasPressedThisFrame ||
                Keyboard.current.tabKey.wasPressedThisFrame)
                mapController.ToggleMap();

            if (Keyboard.current.fKey.wasPressedThisFrame)
                mapController.FocusOnPlayer();
        }

        // Gamepad
        Gamepad gp = Gamepad.current;
        if (gp != null)
        {
            // Select/View button untuk toggle map
            if (gp.selectButton.wasPressedThisFrame)
                mapController.ToggleMap();

            // Y / Triangle untuk focus ke player
            if (gp.buttonNorth.wasPressedThisFrame)
                mapController.FocusOnPlayer();
        }
    }

    void HandleKeyboardPan()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (h != 0f || v != 0f)
            mapController.ApplyPan(new Vector2(h, v) * panKeySensitivity);
    }

    void HandleMiddleMousePan()
    {
        if (Input.GetMouseButtonDown(2))
        {
            _isPanDragging = true;
            _lastMousePos = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(2)) _isPanDragging = false;

        if (_isPanDragging)
        {
            Vector3 delta = Input.mousePosition - _lastMousePos;
            mapController.ApplyPan(new Vector2(-delta.x, -delta.y) * panDragSensitivity);
            _lastMousePos = Input.mousePosition;
        }
    }

    void HandleRightMousePitch()
    {
        if (Input.GetMouseButtonDown(1))
        {
            _isPitchDragging = true;
            _lastMousePos = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(1)) _isPitchDragging = false;

        if (_isPitchDragging)
        {
            float dy = Input.mousePosition.y - _lastMousePos.y;
            mapController.ApplyPitch(dy * pitchDragSensitivity);
            _lastMousePos = Input.mousePosition;
        }
    }

    void HandleRotate()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.Q)) input = -1f;
        if (Input.GetKey(KeyCode.E)) input = 1f;
        if (input != 0f)
            mapController.ApplyRotate(input * rotateSensitivity);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
            mapController.ApplyZoom(scroll * scrollSensitivity);
    }

    void HandleGamepad()
    {
        Gamepad gp = Gamepad.current;
        if (gp == null) return;

        // Left stick = Pan
        Vector2 left = gp.leftStick.ReadValue();
        if (left.magnitude > stickDeadzone)
            mapController.ApplyPan(left * gamepadPanSensitivity);

        // Right stick X = Rotate, Right stick Y = Pitch
        Vector2 right = gp.rightStick.ReadValue();
        if (Mathf.Abs(right.x) > stickDeadzone)
            mapController.ApplyRotate(right.x * gamepadRotateSensitivity);

        if (Mathf.Abs(right.y) > stickDeadzone)
            mapController.ApplyPitch(right.y * gamepadPitchSensitivity);

        // Triggers / Bumpers = Zoom
        float zoomInput = gp.rightTrigger.ReadValue() - gp.leftTrigger.ReadValue();
        if (Mathf.Abs(zoomInput) > 0.01f)
            mapController.ApplyZoom(zoomInput * gamepadZoomSensitivity);
    }
}