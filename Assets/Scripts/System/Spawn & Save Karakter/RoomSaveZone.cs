using UnityEngine;

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

        // Auto-save ke slot kosong pertama via SaveManager
        if (SaveManager.Instance != null)
        {
            string slot = SaveManager.Instance.GetFirstEmptySlot();
            if (slot != null)
            {
                playerSave.SaveToSlot(slot);
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
}