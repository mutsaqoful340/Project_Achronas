using UnityEngine;
using UnityEngine.InputSystem;

public class _RadialQTE_Manager : MonoBehaviour
{
    [Header("Radial QTE Instances")]
    public _RadialQTE radialQTE1;
    public _RadialQTE radialQTE2;

    void Awake()
    {
        radialQTE1.gameObject.SetActive(false);
        radialQTE2.gameObject.SetActive(false);
    }

    public void OnStartRadQTE()
    {
        // Randomize success zone angle (SAME for both players)
        float sharedSuccessZoneAngle = Random.Range(0f, 360f);

        // Randomize buttons (DIFFERENT for each player)
        string[] buttons = { "Y", "B", "A", "X" };
        int button1Index = Random.Range(0, buttons.Length);
        int button2Index;
        
        // Ensure button2 is different from button1
        do
        {
            button2Index = Random.Range(0, buttons.Length);
        } while (button2Index == button1Index);

        string button1 = buttons[button1Index];
        string button2 = buttons[button2Index];

        Debug.Log($"<color=yellow>╔══════════════════════════════════════╗</color>");
        Debug.Log($"<color=yellow>║ QTE Button Assignment</color>");
        Debug.Log($"<color=yellow>║ RadialQTE1 expects: {button1}</color>");
        Debug.Log($"<color=yellow>║ RadialQTE2 expects: {button2}</color>");
        Debug.Log($"<color=yellow>║ Success Zone Angle: {sharedSuccessZoneAngle:F1}°</color>");
        Debug.Log($"<color=yellow>╚══════════════════════════════════════╝</color>");

        // Setup both QTEs with coordinated parameters
        radialQTE1.SetupQTE(button1, sharedSuccessZoneAngle);
        radialQTE2.SetupQTE(button2, sharedSuccessZoneAngle);

        // Assign devices dari PlayerSessionData
        if (PlayerSessionData.Instance != null && PlayerSessionData.Instance.IsValid())
        {
            var p1Device = PlayerSessionData.Instance.player1Device;
            var p2Device = PlayerSessionData.Instance.player2Device;
            
            Debug.Log($"<color=cyan>╔══════════════════════════════════════╗</color>");
            Debug.Log($"<color=cyan>║ QTE Manager Device Assignment</color>");
            Debug.Log($"<color=cyan>║ Player 1 Device: {p1Device?.name} (ID: {p1Device?.deviceId})</color>");
            Debug.Log($"<color=cyan>║ Player 2 Device: {p2Device?.name} (ID: {p2Device?.deviceId})</color>");
            
            if (p1Device != null && p2Device != null)
            {
                if (p1Device.deviceId == p2Device.deviceId)
                {
                    Debug.LogError($"<color=red>║ ✗✗✗ ERROR: BOTH PLAYERS HAVE THE SAME DEVICE! ✗✗✗</color>");
                }
                else
                {
                    Debug.Log($"<color=lime>║ ✓ Devices are different (CORRECT)</color>");
                }
            }
            
            Debug.Log($"<color=cyan>║ RadialQTE1 ← Player 1 Device</color>");
            Debug.Log($"<color=cyan>║ RadialQTE2 ← Player 2 Device</color>");
            Debug.Log($"<color=cyan>╚══════════════════════════════════════╝</color>");
            
            radialQTE1.AssignDevice(p1Device);
            radialQTE2.AssignDevice(p2Device);
        }
        else
        {
            Debug.LogError("<color=red>✗✗✗ PlayerSessionData not valid! Cannot assign devices to QTEs.</color>");
        }

        radialQTE1.gameObject.SetActive(true);
        radialQTE2.gameObject.SetActive(true);
    }

    public void OnEndRadQTE()
    {
        radialQTE1.gameObject.SetActive(false);
        radialQTE2.gameObject.SetActive(false);
    }
}
