using UnityEngine;

/// <summary>
/// Example: Player controller that only works during Gameplay mode
/// </summary>
public class ExamplePlayerController : GameplayBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private void Update()
    {
        // Only process input when gameplay is active
        if (!isActive) return;

        // Your gameplay logic here
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        transform.Translate(new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime);
    }

    protected override void OnGameplayEnabled()
    {
        Debug.Log("Player controller enabled");
        // Enable player input, camera, etc.
    }

    protected override void OnGameplayDisabled()
    {
        Debug.Log("Player controller disabled");
        // Disable player input, stop movement, etc.
    }
}
