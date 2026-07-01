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

    [Header("Minimap")]
    public MinimapRoom minimapRoom;

    // ═══════════════════════════════════════════════════════════
    // TRIGGER
    // ═══════════════════════════════════════════════════════════
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            Save();
    }

    // ═══════════════════════════════════════════════════════════
    // SAVE
    // ═══════════════════════════════════════════════════════════
    private void Save()
    {
        // Tandai ruangan ini sebagai visited di minimap
        if (minimapRoom != null)
            minimapRoom.OnTriggerEnterExternal();

        // Simpan spawn point ke PlayerPrefs
        PlayerPrefs.SetString("lastRoomID", roomID);
        
        // Ensure spawn points are above minimum Y (0.5) to prevent floor clipping on respawn
        Vector3 safeSpawnP1 = spawnP1.position;
        Vector3 safeSpawnP2 = spawnP2.position;
        if (safeSpawnP1.y < 0.5f) safeSpawnP1.y = 0.5f;
        if (safeSpawnP2.y < 0.5f) safeSpawnP2.y = 0.5f;
        
        PlayerPrefs.SetString("spawnP1", JsonUtility.ToJson(safeSpawnP1));
        PlayerPrefs.SetString("spawnP2", JsonUtility.ToJson(safeSpawnP2));
        Debug.Log($"[ROOMSAVE] Saved spawn points - P1: {safeSpawnP1}, P2: {safeSpawnP2}");
        PlayerPrefs.Save();

        if (SaveManager.Instance != null)
        {
            string slot = SaveManager.Instance.FindSlotByRoomID(roomID);

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