using UnityEngine;
using UnityEngine.InputSystem;

public class MapInputHandler : MonoBehaviour
{
    [Header("References")]
    public MapController mapController;

    [Header("Sensitivities")]
    public float panKeySensitivity = 1f;
    public float panDragSensitivity = 0.25f;
    public float rotateSensitivity = 1f;
    public float pitchDragSensitivity = 0.3f;
    public float scrollSensitivity = 4f;

    private Vector3 _lastMousePos;
    private bool _isPanDragging;
    private bool _isPitchDragging;

    void Update()
    {
        if (mapController == null) return;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.mKey.wasPressedThisFrame ||
                Keyboard.current.tabKey.wasPressedThisFrame)
                mapController.ToggleMap();

            if (Keyboard.current.fKey.wasPressedThisFrame)
                mapController.FocusOnPlayer();
        }

        if (!mapController.isOpen) return;

        HandleKeyboardPan();
        HandleMiddleMousePan();
        HandleRightMousePitch();
        HandleRotate();
        HandleZoom();
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

    void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.F)) mapController.FocusOnPlayer();
        if (Input.GetKeyDown(KeyCode.M) ||
            Input.GetKeyDown(KeyCode.Tab)) mapController.ToggleMap();
    }
}