using UnityEngine;

public class PlayerSaveController : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    // REFERENCES
    // ═══════════════════════════════════════════════════════════
    [Header("Referensi")]
    [SerializeField] private Transform player1Transform;
    [SerializeField] private Transform player2Transform;

    // ═══════════════════════════════════════════════════════════
    // STATE
    // ═══════════════════════════════════════════════════════════
    private int _playTimeSeconds;
    private float _playTimeAccum;

    // ═══════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════
    private void Awake()
    {
        if (player1Transform == null)
            player1Transform = transform;
    }

    private void Update()
    {
        TrackPlayTime();
    }

    // ═══════════════════════════════════════════════════════════
    // PLAY TIME TRACKER
    // ═══════════════════════════════════════════════════════════
    private void TrackPlayTime()
    {
        _playTimeAccum += Time.deltaTime;
        if (_playTimeAccum >= 1f)
        {
            _playTimeSeconds++;
            _playTimeAccum -= 1f;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // PUBLIC API
    // ═══════════════════════════════════════════════════════════

    /// <summary>Simpan ke slot tertentu.</summary>
    public void SaveToSlot(string slot)
    {
        SaveVisitedRoomsToPrefs(); // kumpulkan visited rooms dulu
        SaveManager.Instance?.Save(slot, player1Transform, player2Transform, _playTimeSeconds);
    }

    /// <summary>Load dari slot tertentu.</summary>
    public void LoadFromSlot(string slot)
    {
        // visited rooms di-restore di dalam SaveManager.Load()
        SaveManager.Instance?.Load(slot, player1Transform, player2Transform);
    }

    public Transform GetPlayer1Transform() => player1Transform;
    public Transform GetPlayer2Transform() => player2Transform;

    // ═══════════════════════════════════════════════════════════
    // MINIMAP VISITED ROOMS
    // ═══════════════════════════════════════════════════════════

    /// <summary>Kumpulkan semua ruangan yang sudah visited ke PlayerPrefs.</summary>
    private void SaveVisitedRoomsToPrefs()
    {
        MinimapRoom[] allRooms = FindObjectsOfType<MinimapRoom>();
        System.Collections.Generic.List<string> visited = new();

        foreach (var room in allRooms)
            if (!string.IsNullOrEmpty(room.roomID))
                if (PlayerPrefs.GetInt("MinimapRoom_" + room.roomID, 0) == 1)
                    visited.Add(room.roomID);

        PlayerPrefs.SetString("VisitedRooms", string.Join(",", visited));
        PlayerPrefs.Save();
    }
}