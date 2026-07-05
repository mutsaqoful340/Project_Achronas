using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapInputHandler : MonoBehaviour
{
    [Header("References")]
    public MapController mapController;
    public MenuSelector menuSelector;
    public SlotSelector slotSelector;

    [Header("Map UI")]
    public Button mapBackButton;

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

    private bool _canSelectMapUI = true;
    private float _uiSelectCooldown = 0f;

    bool IsInMenu()
    {
        if (menuSelector == null) return false;

        return menuSelector.mainPanel.activeSelf
            || menuSelector.playPanel.activeSelf
            || menuSelector.settingsPanel.activeSelf
            || menuSelector.extrasPanel.activeSelf
            || menuSelector.gameplayPanel.activeSelf
            || menuSelector.audioPanel.activeSelf
            || menuSelector.videoPanel.activeSelf
            || menuSelector.controlPanel.activeSelf
            || menuSelector.continuePanel.activeSelf;
    }

    void Update()
    {
        // Kalau map belum aktif, jangan proses input map sama sekali
        if (!mapController.isOpen || !mapController.mapUIRoot.activeSelf)
            return;

        UpdateUICooldown();
        HandleToggleAndFocus();

        HandleKeyboardPan();
        HandleMiddleMousePan();
        HandleRightMousePitch();
        HandleRotate();
        HandleZoom();
        HandleGamepad();
    }

    void UpdateUICooldown()
    {
        if (_uiSelectCooldown > 0f)
        {
            _uiSelectCooldown -= Time.unscaledDeltaTime;

            if (_uiSelectCooldown <= 0f)
            {
                _uiSelectCooldown = 0f;
                _canSelectMapUI = true;
            }
        }
    }

    public void OnClickToggleMapButton()
    {
        if (mapController == null) return;
        mapController.ToggleMap();
    }

    void HandleToggleAndFocus()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.mKey.wasPressedThisFrame ||
                Keyboard.current.tabKey.wasPressedThisFrame)
                mapController.ToggleMap();

            if (Keyboard.current.fKey.wasPressedThisFrame)
                mapController.FocusOnPlayer();
        }

        Gamepad gp = Gamepad.current;

        if (gp != null && !IsInMenu())
        {
            if (slotSelector != null &&
                slotSelector.gameObject.activeInHierarchy &&
                slotSelector.isFromPauseMenu)
                return;

            if (gp.dpad.up.wasPressedThisFrame)
                mapController.ToggleMap();

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

        if (Input.GetMouseButtonUp(2))
            _isPanDragging = false;

        if (_isPanDragging)
        {
            Vector3 delta = Input.mousePosition - _lastMousePos;

            mapController.ApplyPan(
                new Vector2(-delta.x, -delta.y) * panDragSensitivity
            );

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

        if (Input.GetMouseButtonUp(1))
            _isPitchDragging = false;

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

        if (!mapController.isOpen)
            return;
        Gamepad gp = Gamepad.current;
        if (gp == null) return;

        // D-pad Down = masuk mode UI (select button Back)
        if (gp.dpad.down.wasPressedThisFrame &&
            mapBackButton != null &&
            _canSelectMapUI)
        {
            EnterMapUIFocus();
            return;
        }

        // Mode UI aktif
        if (mapController.isUIFocus)
        {
            // B = keluar UI focus
            if (gp.buttonEast.wasPressedThisFrame)
            {
                ExitMapUIFocus();
            }

            return;
        }

        // Map controls
        Vector2 left = gp.leftStick.ReadValue();

        if (left.magnitude > stickDeadzone)
            mapController.ApplyPan(left * gamepadPanSensitivity);

        Vector2 right = gp.rightStick.ReadValue();

        if (Mathf.Abs(right.x) > stickDeadzone)
            mapController.ApplyRotate(right.x * gamepadRotateSensitivity);

        if (Mathf.Abs(right.y) > stickDeadzone)
            mapController.ApplyPitch(right.y * gamepadPitchSensitivity);

        float zoomInput =
            gp.rightTrigger.ReadValue() - gp.leftTrigger.ReadValue();

        if (Mathf.Abs(zoomInput) > 0.01f)
            mapController.ApplyZoom(zoomInput * gamepadZoomSensitivity);
    }

    void EnterMapUIFocus()
    {
        mapController.isUIFocus = true;

        EventSystem.current.SetSelectedGameObject(null);

        ExecuteEvents.Execute(
            mapBackButton.gameObject,
            new BaseEventData(EventSystem.current),
            ExecuteEvents.selectHandler
        );

        EventSystem.current.SetSelectedGameObject(mapBackButton.gameObject);

        _canSelectMapUI = false;
        _uiSelectCooldown = 0.2f;
    }

    void ExitMapUIFocus()
    {
        mapController.isUIFocus = false;
        EventSystem.current.SetSelectedGameObject(null);
    }
}