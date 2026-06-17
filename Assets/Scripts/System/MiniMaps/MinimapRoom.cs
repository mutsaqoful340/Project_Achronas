using UnityEngine;

public class MinimapRoom : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    // ROOM IDENTITY
    // ═══════════════════════════════════════════════════════════
    [Header("Room Identity")]
    [Tooltip("ID unik ruangan, harus sama dengan RoomSaveZone.roomID")]
    public string roomID;

    // ═══════════════════════════════════════════════════════════
    // REFERENCES
    // ═══════════════════════════════════════════════════════════
    [Header("Room Covers")]
    public GameObject[] coverHiddens;
    public GameObject[] coverBlurs;

    [Header("UI")]
    public GameObject questionMark;

    // ═══════════════════════════════════════════════════════════
    // STATE
    // ═══════════════════════════════════════════════════════════
    private bool everVisited = false;

    // ═══════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════
    void Start()
    {
        LoadVisitedState();
    }

    // ═══════════════════════════════════════════════════════════
    // PUBLIC API
    // ═══════════════════════════════════════════════════════════

    /// <summary>Load status visited dari PlayerPrefs (dipanggil saat Start atau setelah Load game).</summary>
    public void LoadVisitedState()
    {
        everVisited = PlayerPrefs.GetInt("MinimapRoom_" + roomID, 0) == 1;

        if (everVisited)
        {
            SetAllCoversActive(false, false, false);
        }
        else
        {
            SetAllCoversActive(true, false, true);
        }
    }

    /// <summary>Dipanggil dari RoomSaveZone saat player masuk trigger door.</summary>
    public void OnTriggerEnterExternal()
    {
        if (everVisited) return;
        MarkVisited();
    }

    /// <summary>Dipanggil dari RoomProximity saat player mendekat/menjauh.</summary>
    public void SetNearby(bool isNear)
    {
        if (everVisited) return;
        SetAllCoversActive(!isNear, isNear, !isNear);
    }

    // ═══════════════════════════════════════════════════════════
    // TRIGGER
    // ═══════════════════════════════════════════════════════════
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (everVisited) return;
        MarkVisited();
    }

    // ═══════════════════════════════════════════════════════════
    // PRIVATE
    // ═══════════════════════════════════════════════════════════
    private void MarkVisited()
    {
        everVisited = true;
        SetAllCoversActive(false, false, false);

        // Simpan ke PlayerPrefs
        PlayerPrefs.SetInt("MinimapRoom_" + roomID, 1);
        PlayerPrefs.Save();
    }

    private void SetAllCoversActive(bool hidden, bool blur, bool question)
    {
        foreach (GameObject cover in coverHiddens)
            if (cover != null) cover.SetActive(hidden);

        foreach (GameObject b in coverBlurs)
            if (b != null) b.SetActive(blur);

        if (questionMark != null)
            questionMark.SetActive(question);
    }
}