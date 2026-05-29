using UnityEngine;
using DG.Tweening;

public class TabletAnimator : MonoBehaviour
{
    [Header("Posisi")]
    public Transform tabletShowPos;   // TabletShowPos empty
    public Transform tabletHidePos;   // TabletHidePos empty

    [Header("Animasi")]
    public float animDuration = 0.4f;
    public Ease showEase = Ease.OutBack;
    public Ease hideEase = Ease.InBack;

    [Header("Referensi")]
    public PauseMenuSelector pauseMenu;
    public GameObject pausePanelUI;   // Panel-PauseMenu UI

    bool isShowing = false;
    Tween currentTween;

    void Start()
    {
        // Mulai di posisi hide
        transform.localPosition = tabletHidePos.localPosition;
        if (pausePanelUI != null) pausePanelUI.SetActive(false);
    }

    // ── Dipanggil dari PauseMenuSelector ─────────────────────

    public void ShowTablet()
    {
        if (isShowing) return;
        isShowing = true;

        currentTween?.Kill();
        gameObject.SetActive(true);

        transform.localPosition = tabletHidePos.localPosition;

        currentTween = transform.DOLocalMove(tabletShowPos.localPosition, animDuration)
            .SetEase(showEase)
            .SetUpdate(true) // jalan walau Time.timeScale = 0
            .OnComplete(() =>
            {
                if (pausePanelUI != null) pausePanelUI.SetActive(true);
            });
    }

    public void HideTablet()
    {
        if (!isShowing) return;
        isShowing = false;

        if (pausePanelUI != null) pausePanelUI.SetActive(false);

        currentTween?.Kill();
        currentTween = transform.DOLocalMove(tabletHidePos.localPosition, animDuration)
            .SetEase(hideEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
    }

    public bool IsShowing => isShowing;
}
