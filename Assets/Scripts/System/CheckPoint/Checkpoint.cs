using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public string checkpointID;
    public Transform spawnPoint;

    private bool activated = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !activated)
        {
            activated = true;

            // 🔥 UPDATE: kirim spawnPoint (bukan cuma posisi)
            GameManager.Instance.SetCheckpoint(
                checkpointID,
                spawnPoint.position,
                spawnPoint
            );

            // 🔥 SAVE
            SaveSystem.SaveGame(GameManager.Instance.allCheckpoints);

            Debug.Log("🔥 SAVE DIPANGGIL DARI CHECKPOINT!");

            // efek visual
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.green;
            }

            Debug.Log("Checkpoint: " + checkpointID);
        }
    }
}