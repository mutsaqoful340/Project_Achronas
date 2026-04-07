using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using TMPro;

public class CheckpointUI : MonoBehaviour
{
    [Header("UI")]
    public List<Button> slots;
    public GameObject uiRoot;
    public GameObject panelChoose;
    public GameObject panelGame;

    [Header("Players")]
    public List<GameObject> players;

    [Header("Spawn Settings")]
    public float playerSpacing = 2f; // jarak antar player biar gak numpuk

    void OnEnable()
    {
        Debug.Log("CheckpointUI AKTIF");

        

        UpdateSlots();
    }

    void UpdateSlots()
    {
        List<SaveData> dataList = SaveSystem.LoadAllCheckpoints();
        Debug.Log("Jumlah save: " + dataList.Count);

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
            {
                Debug.LogError("Slot index " + i + " NULL!");
                continue;
            }

            Button btn = slots[i];
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();

            if (txt == null)
            {
                Debug.LogError("TMP_Text tidak ditemukan di: " + btn.name);
                continue;
            }

            btn.onClick.RemoveAllListeners();

            if (i < dataList.Count)
            {
                SaveData data = dataList[i];
                txt.text = data.checkpointID;

                string id = data.checkpointID;

                btn.onClick.AddListener(() =>
                {
                    Debug.Log("Klik slot: " + id);
                    LoadCheckpoint(id);
                });
            }
            else
            {
                txt.text = "Empty";

                btn.onClick.AddListener(() =>
                {
                    Debug.Log("Klik EMPTY");
                    StartNewGame();
                });
            }
        }

        // default select (buat gamepad)
        if (slots.Count > 0 && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(slots[0].gameObject);
        }
    }

    void LoadCheckpoint(string id)
    {
        List<SaveData> dataList = SaveSystem.LoadAllCheckpoints();
        SaveData data = dataList.Find(c => c.checkpointID == id);

        if (data != null)
        {
            Vector3 basePos = new Vector3(data.posX, data.posY, data.posZ);

            // 🔥 LOCK arah ke dunia (biar gak ngikut rotasi player)
            Vector3 rightDir = Vector3.right;

            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p != null)
                {
                    Vector3 offset = rightDir * (i * playerSpacing);

                    CharacterController cc = p.GetComponent<CharacterController>();

                    if (cc != null)
                    {
                        cc.enabled = false;
                        p.transform.position = basePos + offset;
                        cc.enabled = true;
                    }
                    else
                    {
                        p.transform.position = basePos + offset;
                    }

                    
                }
            }

            Debug.Log("Load: " + id);
        }
        else
        {
            Debug.Log("Data tidak ditemukan!");
        }

        panelChoose.SetActive(false);
        panelGame.SetActive(true);
    }

    void StartNewGame()
    {
        Debug.Log("Start New Game KE PANGGIL!");

        Vector3 basePos = GameManager.Instance.defaultSpawnPoint;

        // 🔥 FIX: kasih offset biar gak numpuk
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (p != null)
            {
                Vector3 offset = new Vector3(i * playerSpacing, 0, 0);
                p.transform.position = basePos + offset;
                p.SetActive(true);
            }
        }

        uiRoot.SetActive(false);
        panelGame.SetActive(true);
    }

    // ❌ DIHAPUS: Input lama biar gak error Input System
    // Kalau mau debug, pakai Input System baru nanti
}