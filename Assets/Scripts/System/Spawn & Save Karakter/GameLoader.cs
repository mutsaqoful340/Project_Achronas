using UnityEngine;

public class GameLoader : MonoBehaviour
{
    [Header("Players")]
    public Transform player1;
    public Transform player2;

    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey("spawnP1") || !PlayerPrefs.HasKey("spawnP2")) return;

        Vector3 p1 = JsonUtility.FromJson<Vector3>(PlayerPrefs.GetString("spawnP1"));
        Vector3 p2 = JsonUtility.FromJson<Vector3>(PlayerPrefs.GetString("spawnP2"));

        CharacterController cc1 = player1.GetComponent<CharacterController>();
        CharacterController cc2 = player2.GetComponent<CharacterController>();

        cc1.enabled = false;
        player1.position = p1;
        cc1.enabled = true;

        cc2.enabled = false;
        player2.position = p2;
        cc2.enabled = true;

        Debug.Log($"[LOAD] {PlayerPrefs.GetString("lastRoomID")}");
    }
}