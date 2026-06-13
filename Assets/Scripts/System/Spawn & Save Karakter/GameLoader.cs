using UnityEngine;

public class GameLoader : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    // REFERENCES
    // ═══════════════════════════════════════════════════════════
    [Header("Players")]
    public Transform player1;
    public Transform player2;

    // ═══════════════════════════════════════════════════════════
    // PUBLIC
    // ═══════════════════════════════════════════════════════════
    public void LoadGame(string slot)
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[GAMELOADER] SaveManager tidak ditemukan!");
            return;
        }

        if (!SaveManager.Instance.SlotExists(slot))
        {
            Debug.LogWarning($"[GAMELOADER] Slot '{slot}' tidak ada!");
            return;
        }

        SaveManager.Instance.Load(slot, player1, player2);
        Debug.Log($"[GAMELOADER] Load dari slot '{slot}'");
    }
}