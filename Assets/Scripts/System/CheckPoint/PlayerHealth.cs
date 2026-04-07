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
        return;
    }
}