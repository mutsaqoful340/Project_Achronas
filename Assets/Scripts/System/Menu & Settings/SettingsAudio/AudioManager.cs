using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("AUDIO MIXER")]
    public AudioMixer audioMixer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load saved volumes
        LoadVolumes();
    }

    void LoadVolumes()
    {
        SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", 1f));
        SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 1f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 1f));
        SetDialogueVolume(PlayerPrefs.GetFloat("DialogueVolume", 1f));
    }

    // Convert 0-1 slider value to dB (-80 to 0)
    float ToDecibel(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        return Mathf.Log10(value) * 20f;
    }

    public void SetMasterVolume(float value)
    {
        audioMixer.SetFloat("MasterVolume", ToDecibel(value));
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float value)
    {
        audioMixer.SetFloat("MusicVolume", ToDecibel(value));
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        audioMixer.SetFloat("SFXVolume", ToDecibel(value));
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }

    public void SetDialogueVolume(float value)
    {
        audioMixer.SetFloat("DialogueVolume", ToDecibel(value));
        PlayerPrefs.SetFloat("DialogueVolume", value);
        PlayerPrefs.Save();
    }

    public void ResetToDefault()
    {
        SetMasterVolume(1f);
        SetMusicVolume(1f);
        SetSFXVolume(1f);
        SetDialogueVolume(1f);
    }

    public float GetMasterVolume() => PlayerPrefs.GetFloat("MasterVolume", 1f);
    public float GetMusicVolume() => PlayerPrefs.GetFloat("MusicVolume", 1f);
    public float GetSFXVolume() => PlayerPrefs.GetFloat("SFXVolume", 1f);
    public float GetDialogueVolume() => PlayerPrefs.GetFloat("DialogueVolume", 1f);
}