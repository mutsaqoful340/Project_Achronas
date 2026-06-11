using UnityEngine;

public class RoomSaveZone : MonoBehaviour
{
    [Header("Room Identity")]
    public string roomID = "Room_A";

    [Header("Spawn Points")]
    public Transform spawnP1;
    public Transform spawnP2;

    private bool hasSaved = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasSaved && other.CompareTag("Player"))
        {
            Save();
            hasSaved = true;
        }
    }

    private void Save()
    {
        PlayerPrefs.SetString("lastRoomID", roomID);
        PlayerPrefs.SetString("spawnP1", JsonUtility.ToJson(spawnP1.position));
        PlayerPrefs.SetString("spawnP2", JsonUtility.ToJson(spawnP2.position));
        PlayerPrefs.Save();

        Debug.Log($"[SAVE] {roomID}");
    }
}