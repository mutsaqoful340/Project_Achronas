using UnityEngine;
using UnityEngine.SceneManagement;
public class Sys_SceneChanger : MonoBehaviour
{
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
