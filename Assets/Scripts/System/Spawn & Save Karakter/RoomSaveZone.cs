using UnityEngine;
using System.IO;

public class RoomSaveZone : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    // REFERENCES
    // ═══════════════════════════════════════════════════════════
    [Header("Room Identity")]
    public string roomID = "Room_A";

    [Header("Spawn Points")]
    public Transform spawnP1;
    public Transform spawnP2;

    [Header("Players")]
    public Transform player1;
    public Transform player2;

    [Header("Player Save Controller")]
    public PlayerSaveController playerSave;

    // ═══════════════════════════════════════════════════════════
    // PRIVATE
    // ═══════════════════════════════════════════════════════════
    private bool hasSaved = false;

    // ═══════════════════════════════════════════════════════════
    // TRIGGER
    // ═══════════════════════════════════════════════════════════
    private void OnTriggerEnter(Collider other)
    {
        if (!hasSaved && other.CompareTag("Player"))
        {
            Save();
            hasSaved = true;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // SAVE
    // ═══════════════════════════════════════════════════════════
    private void Save()
    {
        // Simpan spawn point ke PlayerPrefs biar RespawnManager bisa baca
        PlayerPrefs.SetString("lastRoomID", roomID);
        PlayerPrefs.SetString("spawnP1", JsonUtility.ToJson(spawnP1.position));
        PlayerPrefs.SetString("spawnP2", JsonUtility.ToJson(spawnP2.position));
        PlayerPrefs.Save();

        if (SaveManager.Instance != null)
        {
            // Cek apakah ruangan ini sudah pernah disimpan di slot tertentu
            string slot = SaveManager.Instance.FindSlotByRoomID(roomID);

            // Kalau belum ada, cari slot kosong pertama
            if (slot == null)
                slot = SaveManager.Instance.GetFirstEmptySlot();

            if (slot != null)
            {
                playerSave.SaveToSlot(slot);
                SaveRoomThumbnail(slot);
                Debug.Log($"[SAVE] {roomID} → {slot}");
            }
            else
            {
                Debug.LogWarning("[SAVE] Semua slot penuh!");
            }
        }
        else
        {
            Debug.LogWarning("[SAVE] SaveManager tidak ditemukan!");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // THUMBNAIL
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Copy gambar static ruangan dari Resources/RoomImages/{roomID}.png
    /// menjadi thumbnail untuk slot terkait.
    /// </summary>
    private void SaveRoomThumbnail(string slot)
    {
        Texture2D roomImage = Resources.Load<Texture2D>($"RoomImages/{roomID}");

        if (roomImage == null)
        {
            Debug.LogWarning($"[THUMBNAIL] Gambar 'RoomImages/{roomID}' tidak ditemukan di Resources!");
            return;
        }

        byte[] bytes = roomImage.EncodeToPNG();
        string path = Path.Combine(Application.persistentDataPath, "saves", slot + "_thumb.png");

        File.WriteAllBytes(path, bytes);
        Debug.Log($"[THUMBNAIL] {roomID} → {path}");
    }
}