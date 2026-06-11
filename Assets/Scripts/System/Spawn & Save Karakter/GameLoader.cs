using UnityEngine;

public class GameLoader : MonoBehaviour
{
    [Header("Players")]
    public Transform player1;
    public Transform player2;

    private void Start()
    {
        if (!PlayerPrefs.HasKey("lastRoomID")) return;

        Vector3 p1 = JsonUtility.FromJson<Vector3>(PlayerPrefs.GetString("spawnP1"));
        Vector3 p2 = JsonUtility.FromJson<Vector3>(PlayerPrefs.GetString("spawnP2"));

        player1.position = p1;
        player2.position = p2;

        Debug.Log($"[LOAD] {PlayerPrefs.GetString("lastRoomID")}");
    }
}