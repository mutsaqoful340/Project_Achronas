using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using TMPro; // 🔥 WAJIB untuk TextMeshPro

public class CheckpointUI : MonoBehaviour
{
    [Header("UI")]
    public List<Button> slots;
    public GameObject uiRoot;
    public GameObject panelChoose;
    public GameObject panelGame;

    [Header("Players")]
    public List<GameObject> players;

    void OnEnable()
    {
        Debug.Log("CheckpointUI AKTIF");

        // matikan semua player dulu
        foreach (var p in players)
        {
            if (p != null)
                p.SetActive(false);
        }

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

            // 🔥 SUPPORT TMP (INI YANG FIX MASALAH KAMU)
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

        // 🔥 SET SELECT DEFAULT (WAJIB BUAT GAMEPAD)
        if (slots.Count > 0 && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(slots[0].gameObject);
        }
    }

    void Update()
    {
        // debug input
        if (Input.anyKeyDown)
        {
            Debug.Log("Input terdeteksi");
        }

        // 🔥 TEST MANUAL
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("TEST KLIK MANUAL");
            if (slots.Count > 0)
                slots[0].onClick.Invoke();
        }

        // 🔥 FORCE REFRESH
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("FORCE UPDATE SLOTS");
            UpdateSlots();
        }
    }

    void LoadCheckpoint(string id)
    {
        List<SaveData> dataList = SaveSystem.LoadAllCheckpoints();
        SaveData data = dataList.Find(c => c.checkpointID == id);

        if (data != null)
        {
            Vector3 pos = new Vector3(data.posX, data.posY, data.posZ);

            foreach (var p in players)
            {
                if (p != null)
                {
                    p.transform.position = pos;
                    p.SetActive(true);
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

        Vector3 startPos = GameManager.Instance.defaultSpawnPoint;

        foreach (var p in players)
        {
            if (p != null)
            {
                p.transform.position = startPos;
                p.SetActive(true);
            }
        }

        uiRoot.SetActive(false);
        panelGame.SetActive(true);
    }
}