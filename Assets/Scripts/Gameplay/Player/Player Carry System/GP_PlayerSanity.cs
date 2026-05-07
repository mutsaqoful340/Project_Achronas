using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// This script will handle the player's sanity level and its effects on the game.
/// PUT IN A MANAGER GAMEOBJECT, NOT IN THE PLAYER.
/// </summary>
public class GP_PlayerSanity : MonoBehaviour
{
    public Player_Components Rinda;
    public Volume renderVol;
    public float sanityLevel = 100f;
    public float sanityDecreaseRate = 5f; // Sanity decrease per second when conditions are not met
    public float sanityIncreaseRate = 10f; // Sanity increase per second when conditions are met
    public bool IsSanityRecovered;

    public void HandleSanity()
    {
        OnSanityDepleted();
        // if (renderVol.profile.TryGet(out Vignette vignette))
        // {
        //     // Adjust vignette based on sanity level
        //     float vignetteIntensity = Mathf.Lerp(0f, 1f, 1f - (sanityLevel / 100f));
        //     vignette.intensity.value = vignetteIntensity;
        // }
    }

    private void OnSanityDepleted()
    {
        Rinda.currentActionState = ActionState.Depressed;
        Rinda.HandleDepressed();
        Debug.Log("Sanity depleted! Rinda is now depressed.");
    }
}
