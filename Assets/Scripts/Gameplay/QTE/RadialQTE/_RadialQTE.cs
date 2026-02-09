using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class _RadialQTE : MonoBehaviour
{
    [Header("QTE Visual Settings")]
    [Tooltip("Pengaturan visual untuk QTE radial")]
    public Sprite btnY;
    public Sprite btnB;
    public Sprite btnA;
    public Sprite btnX;
    public Image qteImage;

    private string currentExpectedInput;

    [Header("QTE Components")]
    [Tooltip("Transform yang akan berputar mengelilingi pusat (pointer)")]
    public Transform pointerTransform;
    
    [Tooltip("GameObject success zone dengan Collider2D")]
    public GameObject successZone;

    [Header("QTE Settings")]
    [Tooltip("Kecepatan rotasi pointer (derajat per detik)")]
    public float rotationSpeed = 180f;
    
    [Tooltip("Sudut rotasi minimal untuk success zone (0-360)")]
    public float minSuccessZoneAngle = 0f;
    
    [Tooltip("Sudut rotasi maksimal untuk success zone (0-360)")]
    public float maxSuccessZoneAngle = 360f;

    [Header("Events")]
    public UnityEvent onQTESuccess;
    public UnityEvent onQTEFail;

    [SerializeField] private bool isInSuccessZone = false;
    [SerializeField] private bool isQTEActive = false;
    private InputActions inputActions;
    private InputDevice assignedDevice;
    private Gamepad assignedGamepad;
    private float currentRotation = 0f;

    private void Awake()
    {
        if (pointerTransform == null)
        {
            Debug.LogError($"[{gameObject.name}] Pointer Transform tidak di-assign!");
        }
        
        if (successZone == null)
        {
            Debug.LogError($"[{gameObject.name}] Success Zone tidak di-assign!");
        }
    }

    private void OnEnable()
    {
        // Setup akan dipanggil dari Manager, jangan randomize di sini
        isInSuccessZone = false;
        isQTEActive = true;

        // Reset pointer ke posisi awal
        currentRotation = 0f;
        if (pointerTransform != null)
        {
            pointerTransform.localRotation = Quaternion.Euler(0f, 0f, currentRotation);
        }

        // Initialize InputActions
        if (inputActions == null)
        {
            inputActions = new InputActions();
        }

        // Filter device jika sudah di-assign
        if (assignedDevice != null)
        {
            inputActions.devices = new[] { assignedDevice };
        }

        inputActions.Enable();
    }

    private void OnDisable()
    {
        isQTEActive = false;

        if (inputActions != null)
        {
            inputActions.Disable();
        }
    }

    private void Update()
    {
        if (!isQTEActive || pointerTransform == null) return;

        // Rotasi pointer terus menerus
        currentRotation += rotationSpeed * Time.deltaTime;
        if (currentRotation >= 360f)
        {
            currentRotation -= 360f;
        }

        pointerTransform.localRotation = Quaternion.Euler(0f, 0f, currentRotation);

        // Check for face button presses
        if (assignedGamepad != null)
        {
            string pressedButton = "";
            
            if (assignedGamepad.buttonNorth.wasPressedThisFrame) pressedButton = "Y";
            else if (assignedGamepad.buttonEast.wasPressedThisFrame) pressedButton = "B";
            else if (assignedGamepad.buttonSouth.wasPressedThisFrame) pressedButton = "A";
            else if (assignedGamepad.buttonWest.wasPressedThisFrame) pressedButton = "X";

            if (!string.IsNullOrEmpty(pressedButton))
            {
                Debug.Log($"<color=magenta>╔══════════════════════════════════════╗</color>");
                Debug.Log($"<color=magenta>║ [{gameObject.name}] BUTTON DETECTED</color>");
                Debug.Log($"<color=magenta>║ Gamepad: {assignedGamepad.name} (ID: {assignedGamepad.deviceId})</color>");
                Debug.Log($"<color=magenta>║ Button Pressed: {pressedButton}</color>");
                Debug.Log($"<color=magenta>║ Expected Button: {currentExpectedInput}</color>");
                Debug.Log($"<color=magenta>║ isInSuccessZone: {isInSuccessZone}</color>");
                Debug.Log($"<color=magenta>║ Pointer Rotation: {currentRotation:F1}°</color>");
                Debug.Log($"<color=magenta>╚══════════════════════════════════════╝</color>");
                OnButtonPressed(pressedButton);
            }
        }
        else
        {
            // Check if ANY gamepad is being pressed (debugging)
            foreach (var gamepad in Gamepad.all)
            {
                if (gamepad.buttonNorth.wasPressedThisFrame || 
                    gamepad.buttonEast.wasPressedThisFrame || 
                    gamepad.buttonSouth.wasPressedThisFrame || 
                    gamepad.buttonWest.wasPressedThisFrame)
                {
                    Debug.LogWarning($"<color=red>[{gameObject.name}] assignedGamepad is NULL but detected input from {gamepad.name} (ID: {gamepad.deviceId})</color>");
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Setup QTE dengan parameter yang di-coordinate oleh Manager
    /// </summary>
    public void SetupQTE(string expectedButton, float successZoneAngle)
    {
        // Set expected button input
        currentExpectedInput = expectedButton;

        // Set QTE image sprite based on expected input
        if (qteImage != null)
        {
            switch (currentExpectedInput)
            {
                case "Y":
                    qteImage.sprite = btnY;
                    break;
                case "B":
                    qteImage.sprite = btnB;
                    break;
                case "A":
                    qteImage.sprite = btnA;
                    break;
                case "X":
                    qteImage.sprite = btnX;
                    break;
            }
        }

        // Set success zone rotation
        if (successZone != null)
        {
            successZone.transform.localRotation = Quaternion.Euler(0f, 0f, successZoneAngle);
        }
    }

    /// <summary>
    /// Assign device untuk player tertentu
    /// </summary>
    public void AssignDevice(InputDevice device)
    {
        assignedDevice = device;
        assignedGamepad = device as Gamepad;
        
        Debug.Log($"<color=cyan>[{gameObject.name}] AssignDevice called:</color>");
        Debug.Log($"  Device: {device?.name} (ID: {device?.deviceId})");
        Debug.Log($"  Gamepad: {assignedGamepad != null}");
        
        if (inputActions != null && device != null)
        {
            inputActions.devices = new[] { device };
        }
    }

    /// <summary>
    /// Callback dari Collider2D pointer saat masuk success zone
    /// </summary>
    public void OnPointerEnterSuccessZone()
    {
        isInSuccessZone = true;
        Debug.Log($"<color=lime>✓✓✓ [{gameObject.name}] isInSuccessZone = TRUE (Time: {Time.time:F2}s, Rotation: {currentRotation:F1}°) ✓✓✓</color>");
    }

    /// <summary>
    /// Callback dari Collider2D pointer saat keluar success zone
    /// </summary>
    public void OnPointerExitSuccessZone()
    {
        isInSuccessZone = false;
        Debug.Log($"<color=orange>✗✗✗ [{gameObject.name}] isInSuccessZone = FALSE (Time: {Time.time:F2}s, Rotation: {currentRotation:F1}°) ✗✗✗</color>");
    }

    private void OnButtonPressed(string pressedButton)
    {
        if (!isQTEActive) return;

        Debug.Log($"<color=white>════════════════════════════════════════════════════════</color>");
        Debug.Log($"<color=white>[{gameObject.name}] EVALUATING QTE (Time: {Time.time:F2}s)</color>");
        Debug.Log($"<color=white>  Button Pressed: {pressedButton}</color>");
        Debug.Log($"<color=white>  Expected Button: {currentExpectedInput}</color>");
        Debug.Log($"<color=white>  isInSuccessZone: {isInSuccessZone}</color>");
        Debug.Log($"<color=white>  Pointer Rotation: {currentRotation:F1}°</color>");

        // Check if correct button was pressed
        bool correctButton = (pressedButton == currentExpectedInput);

        // Evaluasi success/fail (must be in success zone AND press correct button)
        if (isInSuccessZone && correctButton)
        {
            Debug.Log($"<color=green>★★★★★ [{gameObject.name}] QTE SUCCESS! ★★★★★</color>");
            onQTESuccess?.Invoke();
        }
        else
        {
            string reason = !correctButton ? $"Wrong button (Expected: {currentExpectedInput}, Pressed: {pressedButton})" : "Not in success zone";
            Debug.Log($"<color=red>✗✗✗✗✗ [{gameObject.name}] QTE FAILED! ✗✗✗✗✗</color>");
            Debug.Log($"<color=red>  Reason: {reason}</color>");
            onQTEFail?.Invoke();
        }
        Debug.Log($"<color=white>════════════════════════════════════════════════════════</color>");

        // Disable QTE setelah input diterima
        isQTEActive = false;
        gameObject.SetActive(false);
    }
}
