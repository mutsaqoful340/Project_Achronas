using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class UniversalTriggerCollider : MonoBehaviour
{
    public UnityEvent onTriggerEnterEvent;
    public UnityEvent onTriggerExitEvent;

    private const int PlayerSlotCount = 2;
    [SerializeField] private Player_Components[] playerInside = new Player_Components[PlayerSlotCount];
    [SerializeField] private bool isTriggerActive = false;
    [SerializeField] private bool twoPlayerMode = true;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Player_Components playerComponent = other.GetComponent<Player_Components>();
        if (playerComponent == null)
        {
            return;
        }

        if (IsPlayerAlreadyInside(playerComponent))
        {
            return;
        }

        AddPlayer(playerComponent);

        if (!twoPlayerMode)
        {
            if (!isTriggerActive)
            {
                isTriggerActive = true;
                onTriggerEnterEvent?.Invoke();
            }

            return;
        }

        if (GetPlayerCount() == PlayerSlotCount && !isTriggerActive)
        {
            isTriggerActive = true;
            onTriggerEnterEvent?.Invoke();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Player_Components playerComponent = other.GetComponent<Player_Components>();
        if (playerComponent == null)
        {
            return;
        }

        RemovePlayer(playerComponent);

        if (!twoPlayerMode || GetPlayerCount() < PlayerSlotCount)
        {
            isTriggerActive = false;
        }

        if (!twoPlayerMode || GetPlayerCount() < PlayerSlotCount)
        {
            onTriggerExitEvent?.Invoke();
        }
    }

    private bool IsPlayerAlreadyInside(Player_Components playerComponent)
    {
        for (int i = 0; i < playerInside.Length; i++)
        {
            if (playerInside[i] == playerComponent)
            {
                return true;
            }
        }

        return false;
    }

    private int GetPlayerCount()
    {
        int count = 0;

        for (int i = 0; i < playerInside.Length; i++)
        {
            if (playerInside[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private void AddPlayer(Player_Components playerComponent)
    {
        for (int i = 0; i < playerInside.Length; i++)
        {
            if (playerInside[i] == null)
            {
                playerInside[i] = playerComponent;
                return;
            }
        }
    }

    private void RemovePlayer(Player_Components playerComponent)
    {
        for (int i = 0; i < playerInside.Length; i++)
        {
            if (playerInside[i] == playerComponent)
            {
                playerInside[i] = null;
                return;
            }
        }
    }
}
