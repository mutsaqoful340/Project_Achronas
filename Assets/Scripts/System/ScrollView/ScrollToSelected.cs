using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ScrollToSelected : MonoBehaviour
{
    public ScrollRect scrollRect;

    private GameObject lastSelected;

    void Update()
    {
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || selected == lastSelected) return;
        lastSelected = selected;

        if (!selected.transform.IsChildOf(scrollRect.content)) return;

        Canvas.ForceUpdateCanvases();

        RectTransform selectedRect = selected.GetComponent<RectTransform>();
        RectTransform contentRect = scrollRect.content;
        RectTransform viewportRect = scrollRect.viewport;

        float slotTop = -selectedRect.anchoredPosition.y - selectedRect.rect.height / 2;
        float slotBottom = -selectedRect.anchoredPosition.y + selectedRect.rect.height / 2;

        float viewportHeight = viewportRect.rect.height;
        float contentHeight = contentRect.rect.height;

        if (contentHeight <= viewportHeight) return;

        float currentOffset = (1 - scrollRect.verticalNormalizedPosition) * (contentHeight - viewportHeight);

        if (slotBottom > currentOffset + viewportHeight)
        {
            // Snap ke baris bawah — align bawah slot ke bawah viewport
            float newOffset = slotBottom - viewportHeight;
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(1 - (newOffset / (contentHeight - viewportHeight)));
        }
        else if (slotTop < currentOffset)
        {
            // Snap ke baris atas — align atas slot ke atas viewport
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(1 - (slotTop / (contentHeight - viewportHeight)));
        }
    }
}