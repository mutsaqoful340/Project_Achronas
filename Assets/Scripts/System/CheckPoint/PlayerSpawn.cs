using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    void Start()
    {
        if (GameManager.Instance.HasSave())
        {
            transform.position = GameManager.Instance.GetSpawnPosition();
        }
    }
}