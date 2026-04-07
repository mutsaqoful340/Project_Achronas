using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        Debug.Log("Health awal: " + currentHealth);
    }

    void Update()
    {
        // tekan tombol A (gamepad)
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            TakeDamage(10);
        }
    }

    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;

        Debug.Log("Health sekarang: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Player mati!");
        Invoke(nameof(Respawn), 2f); // delay biar keliatan
    }

    void Respawn()
    {
        currentHealth = maxHealth;
        Debug.Log("Respawn! Health reset: " + currentHealth);

        if (GameManager.Instance.HasSave())
        {
            Vector3 spawnPos = GameManager.Instance.GetSpawnPosition();

            CharacterController cc = GetComponent<CharacterController>();

            if (cc != null)
            {
                cc.enabled = false; // 🔥 wajib
                transform.position = spawnPos;
                cc.enabled = true;
            }
            else
            {
                transform.position = spawnPos;
            }

            Debug.Log("Respawn ke: " + spawnPos);
        }
        else
        {
            Debug.Log("Tidak ada checkpoint!");
        }
    }
}