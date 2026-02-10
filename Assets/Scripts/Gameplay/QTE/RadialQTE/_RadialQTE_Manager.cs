using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class _RadialQTE_Manager : MonoBehaviour
{
    [Header("Radial QTE Instances")]
    public _RadialQTE radialQTE1;
    public _RadialQTE radialQTE2;

    [Header("Events")]
    public UnityEvent onPlayer1Success;
    public UnityEvent onPlayer1Fail;
    public UnityEvent onPlayer2Success;
    public UnityEvent onPlayer2Fail;
    public UnityEvent onBothPlayersComplete;

    void Awake()
    {
        radialQTE1.gameObject.SetActive(false);
        radialQTE2.gameObject.SetActive(false);

        // Subscribe to button press callbacks
        radialQTE1.OnButtonPressedCallback = OnQTEButtonPressed;
        radialQTE2.OnButtonPressedCallback = OnQTEButtonPressed;
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

        // Setup both QTEs with coordinated parameters
        radialQTE1.SetupQTE(button1, sharedSuccessZoneAngle);
        radialQTE2.SetupQTE(button2, sharedSuccessZoneAngle);

        // Assign devices dari PlayerSessionData
        if (PlayerSessionData.Instance != null && PlayerSessionData.Instance.IsValid())
        {
            radialQTE1.AssignDevice(PlayerSessionData.Instance.player1Device);
            radialQTE2.AssignDevice(PlayerSessionData.Instance.player2Device);
        }

        radialQTE1.gameObject.SetActive(true);
        radialQTE2.gameObject.SetActive(true);
    }

    public void OnEndRadQTE()
    {
        radialQTE1.gameObject.SetActive(false);
        radialQTE2.gameObject.SetActive(false);
    }

    /// <summary>
    /// Called when any QTE detects a button press
    /// </summary>
    private void OnQTEButtonPressed(_RadialQTE qte, string pressedButton)
    {
        if (!qte.IsQTEActive) return;

        // Determine which player
        bool isPlayer1 = (qte == radialQTE1);
        string playerName = isPlayer1 ? "Player 1" : "Player 2";

        // Check if correct button was pressed
        bool correctButton = (pressedButton == qte.CurrentExpectedInput);
        bool inSuccessZone = qte.IsInSuccessZone;

        // Evaluate success/fail
        if (inSuccessZone && correctButton)
        {
            Debug.Log($"<color=green>{playerName} QTE SUCCESS!</color>");
            
            if (isPlayer1)
                onPlayer1Success?.Invoke();
            else
                onPlayer2Success?.Invoke();
        }
        else
        {
            string reason = !correctButton 
                ? $"Wrong button (Expected: {qte.CurrentExpectedInput}, Pressed: {pressedButton})" 
                : "Not in success zone";
            
            Debug.Log($"<color=red>{playerName} QTE FAILED! Reason: {reason}</color>");
            
            if (isPlayer1)
                onPlayer1Fail?.Invoke();
            else
                onPlayer2Fail?.Invoke();
        }

        // Disable this QTE
        qte.DisableQTE();

        // Check if both completed
        if (!radialQTE1.IsQTEActive && !radialQTE2.IsQTEActive)
        {
            Debug.Log("<color=cyan>Both players completed QTE!</color>");
            onBothPlayersComplete?.Invoke();
        }
    }
}
