using UnityEngine;
using System.Collections;

public class PlayerSpawn : MonoBehaviour
{
    public int playerIndex; // set di inspector (0, 1, dll)
    public float spacing = 2f;

    void Start()
    {
        if (GameManager.Instance.HasSave())
        {
            StartCoroutine(SpawnDelay());
        }
    }

    IEnumerator SpawnDelay()
    {
        yield return null; // 🔥 tunggu 1 frame biar gak dioverride script lain

        Vector3 spawn = GameManager.Instance.GetSpawnPosition();
        Vector3 rightDir = GameManager.Instance.GetSpawnRightDirection();

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 🔥 pakai arah dari checkpoint (INI YANG FIX UTAMA)
        transform.position = spawn + rightDir * (playerIndex * spacing);

        if (cc != null) cc.enabled = true;
    }
}