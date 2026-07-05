using UnityEngine;

public class GP_Key : MonoBehaviour
{
    public string keyName;
    public bool isCollected = false;

    public void OnPickup()
    {
        if (isCollected) return;
        isCollected = true;
        gameObject.SetActive(false);
    }

    public void OnReset()
    {
        isCollected = false;
        gameObject.SetActive(true);
    }
}
