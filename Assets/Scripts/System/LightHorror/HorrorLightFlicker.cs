using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class HorrorLightFlicker : MonoBehaviour
{
    [Header("Light")]
    public Light targetLight;

    [Header("Intensity")]
    public float normalIntensity = 4f;
    public float minIntensity = 0f;
    public float maxIntensity = 4f;

    [Header("Timing")]
    public float minStableTime = 2f;
    public float maxStableTime = 6f;

    public float minFlickerTime = 0.03f;
    public float maxFlickerTime = 0.12f;

    [Header("Flicker")]
    public int minFlickers = 2;
    public int maxFlickers = 6;

    [Header("Random Off")]
    public bool randomBlackout = true;

    public float blackoutChance = 0.25f;
    public float blackoutDuration = 0.6f;

    void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();
    }

    void Start()
    {
        StartCoroutine(FlickerRoutine());
    }

    IEnumerator FlickerRoutine()
    {
        while (true)
        {
            targetLight.intensity = normalIntensity;

            yield return new WaitForSeconds(
                Random.Range(minStableTime, maxStableTime));

            int flickers = Random.Range(
                minFlickers,
                maxFlickers + 1);

            for (int i = 0; i < flickers; i++)
            {
                targetLight.intensity =
                    Random.Range(minIntensity, maxIntensity);

                yield return new WaitForSeconds(
                    Random.Range(minFlickerTime, maxFlickerTime));
            }

            targetLight.intensity = normalIntensity;

            if (randomBlackout &&
                Random.value < blackoutChance)
            {
                targetLight.enabled = false;

                yield return new WaitForSeconds(
                    blackoutDuration);

                targetLight.enabled = true;
            }
        }
    }
}