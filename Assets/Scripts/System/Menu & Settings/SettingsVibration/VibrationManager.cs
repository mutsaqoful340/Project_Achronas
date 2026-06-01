using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using System.Collections;

public class VibrationManager : MonoBehaviour
{
    public static VibrationManager Instance { get; private set; }

    private bool vibrationEnabled = true;
    private Gamepad currentGamepad;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load saved setting
        vibrationEnabled = PlayerPrefs.GetInt("Vibration", 1) == 1;
    }

    void Update()
    {
        // Always get current connected gamepad
        currentGamepad = Gamepad.current;
    }

    public void SetVibration(bool enabled)
    {
        vibrationEnabled = enabled;

        // Stop vibration immediately if disabled
        if (!enabled)
            StopVibration();

        PlayerPrefs.SetInt("Vibration", enabled ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"[VibrationManager] Vibration: {enabled}");
    }

    public void Vibrate(float lowFrequency, float highFrequency, float duration)
    {
        if (!vibrationEnabled) return;
        if (currentGamepad == null) return;

        StartCoroutine(VibrationCoroutine(lowFrequency, highFrequency, duration));
    }

    // Preset: ringan (UI navigasi)
    public void VibrateLight()
    {
        Vibrate(0.1f, 0.1f, 0.1f);
    }

    // Preset: sedang (confirm/select)
    public void VibrateMedium()
    {
        Vibrate(0.3f, 0.3f, 0.2f);
    }

    // Preset: kuat (hit/damage)
    public void VibrateHeavy()
    {
        Vibrate(0.7f, 0.7f, 0.3f);
    }

    private IEnumerator VibrationCoroutine(float low, float high, float duration)
    {
        currentGamepad?.SetMotorSpeeds(low, high);
        yield return new WaitForSeconds(duration);
        StopVibration();
    }

    public void StopVibration()
    {
        currentGamepad?.SetMotorSpeeds(0, 0);
    }

    public bool IsVibrationEnabled()
    {
        return vibrationEnabled;
    }

    void OnDisable()
    {
        StopVibration();
    }

    void OnApplicationQuit()
    {
        StopVibration();
    }
}
