using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class VibrationManager : MonoBehaviour
{
    public static VibrationManager Instance { get; private set; }

    private bool vibrationEnabled = true;
    private Gamepad currentGamepad;
    private Coroutine vibrationCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        vibrationEnabled = PlayerPrefs.GetInt("Vibration", 1) == 1;
    }

    void Update()
    {
        currentGamepad = Gamepad.current;

        // TEST SEMENTARA - hapus setelah test
        if (currentGamepad != null && currentGamepad.buttonSouth.wasPressedThisFrame)
            VibrateHeavy();

        // TEST SEMENTARA
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            VibrateHeavy();
    }

    public void SetVibration(bool enabled)
    {
        vibrationEnabled = enabled;

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

        // Stop existing coroutine dulu
        if (vibrationCoroutine != null)
            StopCoroutine(vibrationCoroutine);

        vibrationCoroutine = StartCoroutine(VibrationCoroutine(lowFrequency, highFrequency, duration));
    }

    public void VibrateLight() => Vibrate(0.1f, 0.1f, 0.1f);
    public void VibrateMedium() => Vibrate(0.3f, 0.3f, 0.2f);
    public void VibrateHeavy() => Vibrate(0.7f, 0.7f, 0.3f);

    private IEnumerator VibrationCoroutine(float low, float high, float duration)
    {
        currentGamepad?.SetMotorSpeeds(low, high);
        yield return new WaitForSecondsRealtime(duration);
        StopVibration();
        vibrationCoroutine = null;
    }

    public void StopVibration()
    {
        currentGamepad?.SetMotorSpeeds(0, 0);
    }

    public bool IsVibrationEnabled() => vibrationEnabled;

    void OnDisable() => StopVibration();
    void OnApplicationQuit() => StopVibration();
}