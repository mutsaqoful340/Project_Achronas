using UnityEngine;

public class RoomProximity : MonoBehaviour
{
    public MinimapRoom parentRoom;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        parentRoom.SetNearby(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        parentRoom.SetNearby(false);
    }
}