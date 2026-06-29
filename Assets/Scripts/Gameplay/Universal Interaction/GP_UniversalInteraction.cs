using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GP_UniversalInteraction : MonoBehaviour
{
    public UnityEvent onInteract;

    private readonly Dictionary<Player_Components, UnityAction<ActionState>> _subscribedPlayers
        = new Dictionary<Player_Components, UnityAction<ActionState>>();

    public void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Player_Components player = other.GetComponent<Player_Components>();
        if (player == null || player.moduleInputPlay == null) return;
        if (_subscribedPlayers.ContainsKey(player)) return;

        UnityAction<ActionState> handler = (state) => OnPlayerAction(state);
        player.moduleInputPlay.OnAction += handler;
        _subscribedPlayers.Add(player, handler);
    }

    public void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Player_Components player = other.GetComponent<Player_Components>();
        if (player == null) return;

        Unsubscribe(player);
    }

    private void OnPlayerAction(ActionState state)
    {
        if (state == ActionState.Interact)
        {
            Debug.Log("Interacted with " + gameObject.name);
            onInteract?.Invoke();
        }
    }

    private void Unsubscribe(Player_Components player)
    {
        if (_subscribedPlayers.TryGetValue(player, out var handler))
        {
            if (player.moduleInputPlay != null)
                player.moduleInputPlay.OnAction -= handler;
            _subscribedPlayers.Remove(player);
        }
    }

    private void OnDestroy()
    {
        foreach (var pair in _subscribedPlayers)
        {
            if (pair.Key != null && pair.Key.moduleInputPlay != null)
                pair.Key.moduleInputPlay.OnAction -= pair.Value;
        }
        _subscribedPlayers.Clear();
    }
}
