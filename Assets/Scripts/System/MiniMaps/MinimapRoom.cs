using UnityEngine;

public class MinimapRoom : MonoBehaviour
{
    public GameObject coverHidden;
    public GameObject coverBlur;
    public GameObject questionMark;

    private bool everVisited = false;

    void Start()
    {
        coverHidden.SetActive(true);
        coverBlur.SetActive(false);
        questionMark.SetActive(true);
    }

    public void SetNearby(bool isNear)
    {
        if (everVisited) return;

        coverHidden.SetActive(!isNear);
        coverBlur.SetActive(isNear);
        questionMark.SetActive(!isNear);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        everVisited = true;
        coverHidden.SetActive(false);
        coverBlur.SetActive(false);
        questionMark.SetActive(false);
    }
}