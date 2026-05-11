using UnityEngine;

public class PlayerSaveController : MonoBehaviour
{
    [Header("Referensi")]
    [Tooltip("Kosongkan jika script ini sudah ada di Player itu sendiri")]
    [SerializeField] private Transform playerTransform;

    // ── State ─────────────────────────────────────────────────
    private int _playTimeSeconds;
    private float _playTimeAccum;

    // ── Lifecycle ─────────────────────────────────────────────
    private void Awake()
    {
        if (playerTransform == null)
            playerTransform = transform;
    }

    private void Update()
    {
        TrackPlayTime();
    }

    // ── Play time tracker ─────────────────────────────────────
    private void TrackPlayTime()
    {
        _playTimeAccum += Time.deltaTime;
        if (_playTimeAccum >= 1f)
        {
            _playTimeSeconds++;
            _playTimeAccum -= 1f;
        }
    }

    // ── Public API (dipanggil dari SaveSlotSelector) ──────────

    /// <summary>Simpan ke slot tertentu.</summary>
    public void SaveToSlot(string slot)
    {
        SaveManager.Instance?.Save(slot, playerTransform, _playTimeSeconds);
    }

    /// <summary>Load dari slot tertentu.</summary>
    public void LoadFromSlot(string slot)
    {
        SaveManager.Instance?.Load(slot, playerTransform);
    }

    public Transform GetPlayerTransform() => playerTransform;
}
