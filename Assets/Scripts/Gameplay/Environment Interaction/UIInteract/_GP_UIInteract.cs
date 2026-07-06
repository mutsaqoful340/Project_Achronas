using UnityEngine;

public class _GP_UIInteract : MonoBehaviour
{
    [Header("UI GameObject References")]
    [Tooltip("Icon Interact yang muncul saat player mendekat.")]
    public GameObject interactIcon;

    void Start()
    {
        if (interactIcon == null)
        {
           Debug.LogWarning("Interact icon not assigned in inspector!");
        }
    }

    // void Update()
    // {
    //     OnUIOrientationController();
    // }

    // private void OnUIOrientationController()
    // {
    //     Camera mainCamera = Camera.main;
    //     if (mainCamera != null)
    //     {
    //         interactIcon.transform.LookAt(mainCamera.transform);
    //     }
    // }
}
