using UnityEngine;

public class AudioListenerFollow : MonoBehaviour
{
    public Transform player1;
    public Transform player2;

    void Update()
    {
        if (player1 == null || player2 == null) return;
        transform.position = (player1.position + player2.position) / 2f;
    }
}