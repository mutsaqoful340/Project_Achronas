using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    public Light lamp;
    public float minIntensity = 0.2f;
    public float maxIntensity = 1.2f;
    public float flickerSpeed = 5f;

    void Start()
    {
        if (lamp == null)
            lamp = GetComponent<Light>();
    }

    void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0.0f);
        lamp.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}