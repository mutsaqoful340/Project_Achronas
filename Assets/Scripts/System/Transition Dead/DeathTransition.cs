using System.Collections;
using UnityEngine;

public class DeathTransition : MonoBehaviour
{
    [Header("Fade UI")]
    [Tooltip("CanvasGroup dengan Image hitam full-screen")]
    [SerializeField] private CanvasGroup fadeCanvas;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float holdDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.6f;

    private void Awake()
    {
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0f;
            fadeCanvas.blocksRaycasts = false;
        }
    }

    public IEnumerator PlayDeathTransition()
    {
        fadeCanvas.blocksRaycasts = true;
        yield return Fade(0f, 1f, fadeInDuration);
        yield return new WaitForSecondsRealtime(holdDuration);
    }

    public IEnumerator FadeBackIn()
    {
        yield return Fade(1f, 0f, fadeOutDuration);
        fadeCanvas.blocksRaycasts = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fadeCanvas.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        fadeCanvas.alpha = to;
    }
}