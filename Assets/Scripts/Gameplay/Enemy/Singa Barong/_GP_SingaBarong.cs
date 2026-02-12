using UnityEngine;
using TMPro;
using UnityEngine.Events;
using NUnit.Framework.Internal;
using Unity.Collections;
using UnityEngine.UI;

public class _GP_SingaBarong : MonoBehaviour
{
    [Header("Light Reaction")]
    public UnityEvent onLitByPlayerLight;

    /// <summary>
    /// Called by PlayerLight when this enemy is detected by the player's light
    /// </summary>
    public void OnLitByPlayerLight()
    {
        // React to being lit - implement specific behavior here
        Debug.Log($"{gameObject.name}: I've been spotted by the light!");
        
        // Invoke event for Inspector-assigned reactions
        onLitByPlayerLight?.Invoke();
        
        // Add your behavior here:
        // - Flee from player
        // - Freeze in place
        // - Turn aggressive
        // - Play animation/sound
    }
}
