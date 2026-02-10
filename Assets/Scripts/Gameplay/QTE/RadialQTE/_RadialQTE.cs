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

    // Callback to Manager when button is pressed
    public System.Action<_RadialQTE, string> OnButtonPressedCallback;

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
                // Report to Manager for evaluation
                OnButtonPressedCallback?.Invoke(this, pressedButton);
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
        Debug.Log("Pointer entered success zone");
    }

    /// <summary>
    /// Callback dari Collider2D pointer saat keluar success zone
    /// </summary>
    public void OnPointerExitSuccessZone()
    {
        isInSuccessZone = false;
        Debug.Log("Pointer exited success zone");
    }

    // Public getters for Manager to check state
    public bool IsInSuccessZone => isInSuccessZone;
    public string CurrentExpectedInput => currentExpectedInput;
    public bool IsQTEActive => isQTEActive;

    // Called by Manager to disable QTE after evaluation
    public void DisableQTE()
    {
        isQTEActive = false;
        gameObject.SetActive(false);
    }
}
