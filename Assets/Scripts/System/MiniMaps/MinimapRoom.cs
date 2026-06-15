using UnityEngine;

public class MinimapRoom : MonoBehaviour
{
    [Header("Room Covers")]
    public GameObject[] coverHiddens;
    public GameObject[] coverBlurs;

    [Header("UI")]
    public GameObject questionMark;

    private bool everVisited = false;

    void Start()
    {
        // Awal game: ruangan tertutup
        foreach (GameObject cover in coverHiddens)
        {
            if (cover != null)
                cover.SetActive(true);
        }

        foreach (GameObject blur in coverBlurs)
        {
            if (blur != null)
                blur.SetActive(false);
        }

        if (questionMark != null)
            questionMark.SetActive(true);
    }

    public void SetNearby(bool isNear)
    {
        if (everVisited) return;

        // Cover hidden
        foreach (GameObject cover in coverHiddens)
        {
            if (cover != null)
                cover.SetActive(!isNear);
        }

        // Blur
        foreach (GameObject blur in coverBlurs)
        {
            if (blur != null)
                blur.SetActive(isNear);
        }

        // Tanda tanya
        if (questionMark != null)
            questionMark.SetActive(!isNear);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        everVisited = true;

        // Hilangkan semua cover
        foreach (GameObject cover in coverHiddens)
        {
            if (cover != null)
                cover.SetActive(false);
        }

        // Hilangkan semua blur
        foreach (GameObject blur in coverBlurs)
        {
            if (blur != null)
                blur.SetActive(false);
        }

        // Hilangkan tanda tanya
        if (questionMark != null)
            questionMark.SetActive(false);
    }
}