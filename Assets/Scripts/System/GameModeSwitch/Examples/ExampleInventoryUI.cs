using UnityEngine;

/// <summary>
/// Example: Inventory UI that only works during UI mode
/// </summary>
public class ExampleInventoryUI : UIBehaviour
{
    [SerializeField] private GameObject inventoryPanel;

    private void Update()
    {
        // Only process UI input when UI mode is active
        if (!isActive) return;

        // Your UI logic here
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInventory();
        }
    }

    protected override void OnUIEnabled()
    {
        Debug.Log("Inventory UI enabled");
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
        }
        // Show cursor, enable UI interactions, etc.
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    protected override void OnUIDisabled()
    {
        Debug.Log("Inventory UI disabled");
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
        // Hide cursor, disable UI interactions, etc.
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void CloseInventory()
    {
        // Switch back to gameplay mode
        _GameModeSwitch.Instance.SwitchMode();
    }
}
