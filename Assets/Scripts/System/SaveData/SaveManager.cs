using System;
using System.IO;
using System.Text;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    // SINGLETON
    // ═══════════════════════════════════════════════════════════
    public static SaveManager Instance { get; private set; }

    // ═══════════════════════════════════════════════════════════
    // KONFIGURASI
    // ═══════════════════════════════════════════════════════════
    [Header("Pengaturan Save")]
    [Tooltip("Aktifkan enkripsi sederhana pada file save")]
    [SerializeField] private bool useEncryption = false;

    [Tooltip("Kunci enkripsi (ubah menjadi string unik)")]
    [SerializeField] private string encryptionKey = "kunci-rahasia-123";

    [Tooltip("Subfolder di dalam persistentDataPath")]
    [SerializeField] private string saveFolder = "saves";

    [Tooltip("Ekstensi file save")]
    [SerializeField] private string fileExtension = ".sav";

    // ═══════════════════════════════════════════════════════════
    // EVENTS
    // ═══════════════════════════════════════════════════════════
    public event Action<string> OnSaveSuccess;
    public event Action<string> OnLoadSuccess;
    public event Action<string> OnSaveError;

    // ═══════════════════════════════════════════════════════════
    // PATH HELPERS
    // ═══════════════════════════════════════════════════════════
    private string SaveDirectory => Path.Combine(Application.persistentDataPath, saveFolder);
    private string SlotPath(string slot) => Path.Combine(SaveDirectory, slot + fileExtension);

    // ═══════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Directory.CreateDirectory(SaveDirectory);
        Debug.Log(Application.persistentDataPath);
    }

    // ═══════════════════════════════════════════════════════════
    // SAVE
    // ═══════════════════════════════════════════════════════════
    public bool Save(string slot, Transform player1, Transform player2, int playTimeSeconds = 0)
    {
        try
        {
            var data = new SaveData
            {
                saveSlot = slot,
                savedAt = DateTime.Now.ToString("o"),
                playTimeSeconds = playTimeSeconds,
                lastRoomID = PlayerPrefs.GetString("lastRoomID", ""),

                // Minimap visited rooms
                visitedRooms = PlayerPrefs.GetString("VisitedRooms", "")
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            };

            // Player 1
            data.SetPosition(player1.position);
            data.SetRotation(player1.rotation);

            // Player 2
            data.SetPosition2(player2.position);
            data.SetRotation2(player2.rotation);

            // Spawn Points
            if (PlayerPrefs.HasKey("spawnP1"))
                data.SetSpawnP1(JsonUtility.FromJson<Vector3>(PlayerPrefs.GetString("spawnP1")));

            if (PlayerPrefs.HasKey("spawnP2"))
                data.SetSpawnP2(JsonUtility.FromJson<Vector3>(PlayerPrefs.GetString("spawnP2")));

            string json = JsonUtility.ToJson(data, prettyPrint: true);
            string content = useEncryption ? Encrypt(json) : json;

            File.WriteAllText(SlotPath(slot), content, Encoding.UTF8);

            Debug.Log($"[SaveManager] Tersimpan ke slot '{slot}': {SlotPath(slot)}");
            OnSaveSuccess?.Invoke(slot);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Gagal menyimpan slot '{slot}': {ex.Message}");
            OnSaveError?.Invoke(slot);
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // LOAD
    // ═══════════════════════════════════════════════════════════
    public SaveData Load(string slot, Transform player1, Transform player2)
    {
        string path = SlotPath(slot);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveManager] File save tidak ditemukan: {path}");
            return null;
        }

        try
        {
            string content = File.ReadAllText(path, Encoding.UTF8);
            string json = useEncryption ? Decrypt(content) : content;
            var data = JsonUtility.FromJson<SaveData>(json);

            // Spawn point & room
            PlayerPrefs.SetString("lastRoomID", data.lastRoomID);
            PlayerPrefs.SetString("spawnP1", JsonUtility.ToJson(data.GetSpawnP1()));
            PlayerPrefs.SetString("spawnP2", JsonUtility.ToJson(data.GetSpawnP2()));

            // Restore visited rooms ke PlayerPrefs
            if (data.visitedRooms != null)
            {
                foreach (string id in data.visitedRooms)
                    if (!string.IsNullOrEmpty(id))
                        PlayerPrefs.SetInt("MinimapRoom_" + id, 1);

                PlayerPrefs.SetString("VisitedRooms", string.Join(",", data.visitedRooms));
            }

            PlayerPrefs.Save();

            // Terapkan ke player
            ApplyToPlayer(player1, data.GetSpawnP1(), data.GetRotation());
            ApplyToPlayer(player2, data.GetSpawnP2(), data.GetRotation2());

            // Refresh semua MinimapRoom di scene
            foreach (var room in FindObjectsOfType<MinimapRoom>())
                room.LoadVisitedState();

            Debug.Log($"[SaveManager] Dimuat dari slot '{slot}' — Room: {data.lastRoomID}");
            OnLoadSuccess?.Invoke(slot);
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Gagal memuat slot '{slot}': {ex.Message}");
            return null;
        }
    }

    public SaveData LoadRaw(string slot)
    {
        string path = SlotPath(slot);
        if (!File.Exists(path)) return null;

        try
        {
            string content = File.ReadAllText(path, Encoding.UTF8);
            string json = useEncryption ? Decrypt(content) : content;
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch
        {
            return null;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // UTILITAS
    // ═══════════════════════════════════════════════════════════
    public bool DeleteSave(string slot)
    {
        string path = SlotPath(slot);
        if (!File.Exists(path)) return false;

        File.Delete(path);
        Debug.Log($"[SaveManager] Slot '{slot}' dihapus.");
        return true;
    }

    public bool SlotExists(string slot) => File.Exists(SlotPath(slot));

    public string[] GetAllSlots()
    {
        var files = Directory.GetFiles(SaveDirectory, "*" + fileExtension);
        var slots = new string[files.Length];
        for (int i = 0; i < files.Length; i++)
            slots[i] = Path.GetFileNameWithoutExtension(files[i]);
        return slots;
    }

    public string GetFirstEmptySlot()
    {
        string[] slotNames = { "slot1", "slot2", "slot3", "slot4", "slot5", "slot6" };
        foreach (string slot in slotNames)
            if (!SlotExists(slot)) return slot;
        return null;
    }

    // ═══════════════════════════════════════════════════════════
    // APPLY TO PLAYER
    // ═══════════════════════════════════════════════════════════
    private void ApplyToPlayer(Transform player, Vector3 position, Quaternion rotation)
    {
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.position = position;
        player.rotation = rotation;

        if (cc != null) cc.enabled = true;
    }

    // ═══════════════════════════════════════════════════════════
    // ENKRIPSI XOR
    // ═══════════════════════════════════════════════════════════
    private string Encrypt(string plain)
    {
        byte[] data = Encoding.UTF8.GetBytes(plain);
        byte[] key = Encoding.UTF8.GetBytes(encryptionKey);
        for (int i = 0; i < data.Length; i++)
            data[i] ^= key[i % key.Length];
        return Convert.ToBase64String(data);
    }

    private string Decrypt(string cipher)
    {
        byte[] data = Convert.FromBase64String(cipher);
        byte[] key = Encoding.UTF8.GetBytes(encryptionKey);
        for (int i = 0; i < data.Length; i++)
            data[i] ^= key[i % key.Length];
        return Encoding.UTF8.GetString(data);
    }

    public string FindSlotByRoomID(string roomID)
    {
        string[] slotNames = { "slot1", "slot2", "slot3", "slot4", "slot5", "slot6" };
        foreach (string slot in slotNames)
        {
            if (SlotExists(slot))
            {
                SaveData data = LoadRaw(slot);
                if (data != null && data.lastRoomID == roomID)
                    return slot;
            }
        }
        return null;
    }

}