using System.Collections;
using UnityEngine;

public class RoomAmbientAudio : MonoBehaviour
{
    [Header("References")]
    public AudioSource roomAudio;

    [Header("Fade Settings")]
    public float targetVolume = 1f;
    public float fadeDuration = 1.5f;

    private Coroutine fadeCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTo(targetVolume));
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTo(0f));
    }

    private IEnumerator FadeTo(float target)
    {
        float start = roomAudio.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            roomAudio.volume = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }

        roomAudio.volume = target;
    }
}