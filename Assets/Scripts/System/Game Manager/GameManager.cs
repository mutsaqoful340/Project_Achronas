using UnityEngine;

public class GameManager : MonoBehaviour
{
    public void Awake(){
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 45;
    }
}