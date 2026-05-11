using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// SaveManager — Singleton yang menangani seluruh operasi save/load.
///
/// Cara pakai:
///   SaveManager.Instance.Save("slot1");
///   SaveManager.Instance.Load("slot1");
///   SaveManager.Instance.DeleteSave("slot1");
///
/// Data disimpan di: Application.persistentDataPath/saves/<slot>.sav
/// Format: JSON (bisa diaktifkan enkripsi Base64 XOR sederhana)
/// </summary>
public class SaveManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────
    public static SaveManager Instance { get; private set; }

    // ── Konfigurasi (edit via Inspector) ──────────────────────
    [Header("Pengaturan Save")]
    [Tooltip("Aktifkan enkripsi sederhana pada file save")]
    [SerializeField] private bool useEncryption = false;

    [Tooltip("Kunci enkripsi (ubah menjadi string unik)")]
    [SerializeField] private string encryptionKey = "kunci-rahasia-123";

    [Tooltip("Subfolder di dalam persistentDataPath")]
    [SerializeField] private string saveFolder = "saves";

    [Tooltip("Ekstensi file save")]
    [SerializeField] private string fileExtension = ".sav";

    // ── Events ────────────────────────────────────────────────
    public event Action<string> OnSaveSuccess;
    public event Action<string> OnLoadSuccess;
    public event Action<string> OnSaveError;

    // ── Path helpers ──────────────────────────────────────────
    private string SaveDirectory => Path.Combine(Application.persistentDataPath, saveFolder);

    private string SlotPath(string slot) =>
        Path.Combine(SaveDirectory, slot + fileExtension);

    // ── Lifecycle ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Buat direktori save jika belum ada
        Directory.CreateDirectory(SaveDirectory);
    }

    // ═══════════════════════════════════════════════════════════
    // SAVE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Simpan data player ke slot tertentu.
    /// </summary>
    /// <param name="slot">Nama slot, contoh: "slot1", "autosave"</param>
    /// <param name="player">Transform GameObject player</param>
    /// <param name="playTimeSeconds">Total waktu bermain (detik)</param>
    public bool Save(string slot, Transform player, int playTimeSeconds = 0)
    {
        try
        {
            var data = new SaveData
            {
                saveSlot        = slot,
                savedAt         = DateTime.Now.ToString("o"),
                playTimeSeconds = playTimeSeconds
            };

            data.SetPosition(player.position);
            data.SetRotation(player.rotation);

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

    /// <summary>
    /// Muat data dari slot dan terapkan ke player.
    /// </summary>
    /// <returns>SaveData jika berhasil, null jika gagal</returns>
    public SaveData Load(string slot, Transform player)
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
            string json    = useEncryption ? Decrypt(content) : content;

            var data = JsonUtility.FromJson<SaveData>(json);

            // Terapkan ke player — nonaktifkan CharacterController/Rigidbody sementara
            ApplyToPlayer(player, data);

            Debug.Log($"[SaveManager] Dimuat dari slot '{slot}'");
            OnLoadSuccess?.Invoke(slot);
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Gagal memuat slot '{slot}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Muat hanya datanya tanpa menerapkan ke player (untuk preview UI).
    /// </summary>
    public SaveData LoadRaw(string slot)
    {
        string path = SlotPath(slot);
        if (!File.Exists(path)) return null;

        try
        {
            string content = File.ReadAllText(path, Encoding.UTF8);
            string json    = useEncryption ? Decrypt(content) : content;
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

    /// <summary>Hapus file save untuk slot tertentu.</summary>
    public bool DeleteSave(string slot)
    {
        string path = SlotPath(slot);
        if (!File.Exists(path)) return false;

        File.Delete(path);
        Debug.Log($"[SaveManager] Slot '{slot}' dihapus.");
        return true;
    }

    /// <summary>Cek apakah slot sudah memiliki data.</summary>
    public bool SlotExists(string slot) => File.Exists(SlotPath(slot));

    /// <summary>Ambil semua slot yang tersedia.</summary>
    public string[] GetAllSlots()
    {
        var files = Directory.GetFiles(SaveDirectory, "*" + fileExtension);
        var slots = new string[files.Length];
        for (int i = 0; i < files.Length; i++)
            slots[i] = Path.GetFileNameWithoutExtension(files[i]);
        return slots;
    }

    // ── Apply data ke player ──────────────────────────────────

    private void ApplyToPlayer(Transform player, SaveData data)
    {
        // Matikan CharacterController agar position bisa diset langsung
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.position = data.GetPosition();
        player.rotation = data.GetRotation();

        if (cc != null) cc.enabled = true;
    }

    // ── Enkripsi XOR sederhana ────────────────────────────────

    private string Encrypt(string plain)
    {
        byte[] data = Encoding.UTF8.GetBytes(plain);
        byte[] key  = Encoding.UTF8.GetBytes(encryptionKey);
        for (int i = 0; i < data.Length; i++)
            data[i] ^= key[i % key.Length];
        return Convert.ToBase64String(data);
    }

    private string Decrypt(string cipher)
    {
        byte[] data = Convert.FromBase64String(cipher);
        byte[] key  = Encoding.UTF8.GetBytes(encryptionKey);
        for (int i = 0; i < data.Length; i++)
            data[i] ^= key[i % key.Length];
        return Encoding.UTF8.GetString(data);
    }
}
