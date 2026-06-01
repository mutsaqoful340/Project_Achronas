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

        // Subscribe ke event connect/disconnect gamepad
        InputSystem.onDeviceChange += OnDeviceChange;

        // Cek gamepad yang sudah terconnect
        currentGamepad = Gamepad.current;
    }

    void Update()
    {
        if (currentGamepad == null)
            currentGamepad = Gamepad.current;


    }

    void OnDestroy()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is Gamepad gamepad)
        {
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
            {
                currentGamepad = gamepad;
                Debug.Log($"[VibrationManager] Gamepad connected: {gamepad.name}");
            }
            else if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
            {
                currentGamepad = Gamepad.current;
                Debug.Log("[VibrationManager] Gamepad disconnected");
            }
        }
    }

    public void SetVibration(bool enabled)
    {
        vibrationEnabled = enabled;

        if (!enabled)
            StopVibration();
        else
        {
            Debug.Log($"[VibrationManager] SetMotorSpeeds called - gamepad: {currentGamepad?.name ?? "NULL"}");
            currentGamepad?.SetMotorSpeeds(0.5f, 0.5f);
            Invoke(nameof(StopVibration), 0.3f);
        }

        PlayerPrefs.SetInt("Vibration", enabled ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"[VibrationManager] Vibration: {enabled}");
    }

    public void Vibrate(float lowFrequency, float highFrequency, float duration)
    {
        Debug.Log($"[VibrationManager] Vibrate called - enabled:{vibrationEnabled} gamepad:{currentGamepad?.name ?? "null"}");
        if (!vibrationEnabled) return;
        if (currentGamepad == null) return;

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