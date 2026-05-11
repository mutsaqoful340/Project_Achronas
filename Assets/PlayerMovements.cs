using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float mouseSensitivity = 100f;
    public Transform playerBody;

    private CharacterController controller;
    private Vector2 moveInput;
    private float xRotation = 0f;
    public PauseMenuSelector pauseMenu;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        this.enabled = false;
    }

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        moveInput = Vector2.zero;
    }

    void Update()
    {
        if (pauseMenu != null && pauseMenu.isPaused) return; // ← tambahkan
        Vector3 move = playerBody.right * moveInput.x + playerBody.forward * moveInput.y;
        controller.Move(move * speed * Time.deltaTime);
    }

    public void OnMove(InputValue value)
    {
        if (!enabled) return;
        if (pauseMenu != null && pauseMenu.isPaused) return; // ← tambahkan
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        if (!enabled) return;
        if (pauseMenu != null && pauseMenu.isPaused) return; // ← tambahkan

        Vector2 look = value.Get<Vector2>();
        float mouseX = look.x * mouseSensitivity * Time.deltaTime;
        float mouseY = look.y * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}