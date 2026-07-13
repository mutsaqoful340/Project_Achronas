using System.Collections;
using UnityEngine;

/// <summary>
/// Attach ke GameObject yang punya ParticleSystem + Renderer pakai BloodSplatter shader.
/// Otomatis dissolve-out setelah splatter muncul.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class BloodSplatterFX : MonoBehaviour
{
    [Header("Shader Property")]
    [SerializeField] private float dissolveDelay    = 0.3f;  // detik sebelum dissolve mulai
    [SerializeField] private float dissolveDuration = 0.8f;  // durasi dissolve
    [SerializeField] private float poolingTime      = 1.5f;  // total lifetime sebelum destroy

    [Header("Optional Wetness Pulse")]
    [SerializeField] private bool  doWetPulse       = true;
    [SerializeField] private float wetPulseSpeed    = 2f;

    private ParticleSystemRenderer _psRenderer;
    private MaterialPropertyBlock  _mpb;
    private static readonly int    _DissolveID = Shader.PropertyToID("_Dissolve");
    private static readonly int    _WetnessID  = Shader.PropertyToID("_Wetness");

    void Awake()
    {
        _psRenderer = GetComponent<ParticleSystemRenderer>();
        _mpb        = new MaterialPropertyBlock();
    }

    /// <summary>
    /// Panggil ini dari luar untuk trigger splatter di posisi tertentu.
    /// </summary>
    public static BloodSplatterFX Spawn(BloodSplatterFX prefab, Vector3 worldPos, Quaternion rotation)
    {
        var instance = Instantiate(prefab, worldPos, rotation);
        instance.GetComponent<ParticleSystem>().Play();
        instance.StartCoroutine(instance.RunLifecycle());
        return instance;
    }

    // Kalau sudah ada di scene (bukan prefab spawn), pakai ini
    void OnEnable()
    {
        StartCoroutine(RunLifecycle());
    }

    private IEnumerator RunLifecycle()
    {
        // Phase 1 – fresh splatter, dissolve = 0
        SetDissolve(0f);
        float t = 0f;

        // Wetness pulse selama delay
        while (t < dissolveDelay)
        {
            t += Time.deltaTime;
            if (doWetPulse)
            {
                float wet = 0.5f + 0.5f * Mathf.Sin(t * wetPulseSpeed * Mathf.PI);
                SetWetness(wet);
            }
            yield return null;
        }

        // Phase 2 – dissolve out
        t = 0f;
        while (t < dissolveDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, t / dissolveDuration);
            SetDissolve(progress);
            yield return null;
        }

        SetDissolve(1f);

        // Phase 3 – tunggu sisa lifetime lalu destroy
        yield return new WaitForSeconds(Mathf.Max(0f, poolingTime - dissolveDelay - dissolveDuration));
        Destroy(gameObject);
    }

    private void SetDissolve(float value)
    {
        _psRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(_DissolveID, value);
        _psRenderer.SetPropertyBlock(_mpb);
    }

    private void SetWetness(float value)
    {
        _psRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(_WetnessID, value);
        _psRenderer.SetPropertyBlock(_mpb);
    }
}
