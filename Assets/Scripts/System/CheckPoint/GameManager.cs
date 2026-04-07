using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public List<SaveData> allCheckpoints = new List<SaveData>();
    public Vector3 defaultSpawnPoint;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 🔥 RESET
    public void ResetSave()
    {
        SaveSystem.DeleteSave();
        allCheckpoints.Clear();

        Debug.Log("Save di-reset!");
    }

    // 🔥 SIMPAN CHECKPOINT
    public void SetCheckpoint(string id, Vector3 pos)
    {
        SaveData data = new SaveData
        {
            checkpointID = id,
            posX = pos.x,
            posY = pos.y,
            posZ = pos.z
        };

        // 🔥 biar nggak dobel
        SaveData existing = allCheckpoints.Find(c => c.checkpointID == id);

        if (existing != null)
        {
            // 🔥 UPDATE posisi
            existing.posX = pos.x;
            existing.posY = pos.y;
            existing.posZ = pos.z;

            Debug.Log("Checkpoint di-update: " + id);
        }
        else
        {
            // 🔥 TAMBAH baru
            allCheckpoints.Add(data);

            Debug.Log("Checkpoint baru: " + id);
        }
    }

    // 🔥 AMBIL SPAWN TERAKHIR
    public Vector3 GetSpawnPosition()
    {
        if (allCheckpoints.Count == 0)
        {
            Debug.Log("Tidak ada checkpoint, pakai default");
            return defaultSpawnPoint;
        }

        SaveData last = allCheckpoints[allCheckpoints.Count - 1];

        Vector3 pos = new Vector3(last.posX, last.posY, last.posZ);

        Debug.Log("Spawn dari: " + last.checkpointID);
        Debug.Log("Posisi: " + pos);

        return pos;
    }

    public bool HasSave()
    {
        return allCheckpoints.Count > 0;
    }

    // 🔥 LOAD DARI JSON
    public void LoadGame()
    {
        allCheckpoints = SaveSystem.LoadAllCheckpoints();

        if (allCheckpoints == null)
        {
            allCheckpoints = new List<SaveData>();
        }

        Debug.Log("Jumlah checkpoint: " + allCheckpoints.Count);
    }
}