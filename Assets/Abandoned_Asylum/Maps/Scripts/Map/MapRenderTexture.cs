using UnityEngine;
using UnityEngine.UI;

public class MapRenderTexture : MonoBehaviour
{
    [Header("References")]
    public Camera mapCamera;
    public RawImage mapRawImage;

    [Header("Quality")]
    [Range(0.5f, 1f)]
    public float resolutionScale = 1f;

    private RenderTexture _rt;
    private int _lastW, _lastH;

    void Awake() => CreateOrUpdateTexture();
    void OnEnable() => CreateOrUpdateTexture();

    void Update()
    {
        int w = Mathf.RoundToInt(Screen.width * resolutionScale);
        int h = Mathf.RoundToInt(Screen.height * resolutionScale);
        if (w != _lastW || h != _lastH) CreateOrUpdateTexture();
    }

    void CreateOrUpdateTexture()
    {
        int w = Mathf.RoundToInt(Screen.width * resolutionScale);
        int h = Mathf.RoundToInt(Screen.height * resolutionScale);
        if (w == _lastW && h == _lastH && _rt != null) return;

        if (_rt != null)
        {
            if (mapCamera) mapCamera.targetTexture = null;
            _rt.Release();
            Destroy(_rt);
        }

        _rt = new RenderTexture(w, h, 24, RenderTextureFormat.Default);
        _rt.antiAliasing = 2;
        _rt.filterMode = FilterMode.Bilinear;
        _rt.Create();

        if (mapCamera) mapCamera.targetTexture = _rt;
        if (mapRawImage) mapRawImage.texture = _rt;

        _lastW = w; _lastH = h;
    }

    void OnDestroy()
    {
        if (_rt != null)
        {
            if (mapCamera) mapCamera.targetTexture = null;
            _rt.Release();
            Destroy(_rt);
        }
    }
}