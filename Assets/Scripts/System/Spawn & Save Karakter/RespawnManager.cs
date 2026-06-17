using UnityEngine;
using System.Collections;

public class RespawnManager : MonoBehaviour
{
    [Header("Players")]
    public Player_Components player1;
    public Player_Components player2;

    [Header("Respawn Settings")]
    public float respawnDelay = 3f;

    [Header("Default Spawn (jika belum ada save)")]
    public Transform defaultSpawnP1;
    public Transform defaultSpawnP2;

    private bool isRespawning = false;

    private void Update()
    {
        if (isRespawning) return;
        if (player1.IsDead || player2.IsDead)
        {
            isRespawning = true;
            StartCoroutine(RespawnAll());
        }
    }

    private IEnumerator RespawnAll()
    {
        yield return new WaitForSeconds(respawnDelay);

        Vector3 spawnP1, spawnP2;

        // Kalau belum ada save, pakai default spawn point
        if (!PlayerPrefs.HasKey("spawnP1") || !PlayerPrefs.HasKey("spawnP2"))
        {
            if (defaultSpawnP1 == null || defaultSpawnP2 == null)
            {
                Debug.LogWarning("[RESPAWN] Tidak ada data spawn point dan default spawn belum di-set!");
                isRespawning = false;
                yield break;
            }

            Debug.Log("[RESPAWN] Pakai default spawn point.");
            spawnP1 = defaultSpawnP1.position;
            spawnP2 = defaultSpawnP2.position;
        }
        else
        {
            spawnP1 = JsonUtility.FromJson<Vector3>(PlayerPrefs.GetString("spawnP1"));
            spawnP2 = JsonUtility.FromJson<Vector3>(PlayerPrefs.GetString("spawnP2"));
        }

        WarpPlayer(player1, spawnP1);
        WarpPlayer(player2, spawnP2);

        player1.IsDead = false;
        player2.IsDead = false;
        isRespawning = false;

        Debug.Log("[RESPAWN] Kedua player respawn!");
    }

    private void WarpPlayer(Player_Components player, Vector3 spawnPos)
    {
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            player.transform.position = spawnPos;
            cc.enabled = true;
        }
        else
        {
            player.transform.position = spawnPos;
        }
    }
}