using UnityEngine;

public class PlayerSaveController : MonoBehaviour
{
    [Header("Referensi")]
    [SerializeField] private Transform player1Transform;
    [SerializeField] private Transform player2Transform;

    // ── State ─────────────────────────────────────────────────
    private int _playTimeSeconds;
    private float _playTimeAccum;

    // ── Lifecycle ─────────────────────────────────────────────
    private void Awake()
    {
        if (player1Transform == null)
            player1Transform = transform;
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

    // ── Public API ────────────────────────────────────────────
    /// <summary>Simpan ke slot tertentu.</summary>
    public void SaveToSlot(string slot)
    {
        SaveManager.Instance?.Save(slot, player1Transform, player2Transform, _playTimeSeconds);
    }

    /// <summary>Load dari slot tertentu.</summary>
    public void LoadFromSlot(string slot)
    {
        SaveManager.Instance?.Load(slot, player1Transform, player2Transform);
    }

    public Transform GetPlayer1Transform() => player1Transform;
    public Transform GetPlayer2Transform() => player2Transform;
}