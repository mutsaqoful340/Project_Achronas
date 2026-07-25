using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

public class BrightnessController : MonoBehaviour
{
    [Header("Volume")]
    public Volume brightnessVolume;

    [Header("UI")]
    public TMP_Text brightnessText;

    [Header("Settings")]
    [Range(0, 100)]
    public int brightness = 50;

    private ColorAdjustments colorAdjustments;

    private void Start()
    {
        if (brightnessVolume.profile.TryGet(out colorAdjustments))
        {
            brightness = PlayerPrefs.GetInt("Brightness", 50);
            UpdateBrightness();
        }
        else
        {
            Debug.LogError("Color Adjustments tidak ditemukan pada Volume Profile!");
        }
    }

    public void Increase()
    {
        brightness = Mathf.Clamp(brightness + 5, 0, 100);
        UpdateBrightness();
    }

    public void Decrease()
    {
        brightness = Mathf.Clamp(brightness - 5, 0, 100);
        UpdateBrightness();
    }

    public void ResetBrightness()
    {
        brightness = 50;
        UpdateBrightness();
    }

    private void UpdateBrightness()
    {
        // Update angka di UI
        if (brightnessText != null)
            brightnessText.text = brightness.ToString();

        // Ubah brightness game
        float exposure = Mathf.Lerp(-2f, 2f, brightness / 100f);

        if (colorAdjustments != null)
            colorAdjustments.postExposure.value = exposure;

        // Simpan otomatis
        PlayerPrefs.SetInt("Brightness", brightness);
        PlayerPrefs.Save();
    }
}