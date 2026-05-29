using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public GameObject currentPanel;
    public GameObject targetPanel;

    public void SwitchPanel()
    {
        if (currentPanel != null)
            currentPanel.SetActive(false);

        if (targetPanel != null)
            targetPanel.SetActive(true);
    }
}