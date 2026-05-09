using System;
using System.Numerics;
using Unity.AppUI.Editor;
using UnityEngine;
using UnityEngine.UIElements;

public class _GP_Sit : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Transform tempat player akan duduk.")]
    public Transform sitPosition;

    [Header("Debug - Auto-assigned at runtime, do not modify!")]
    [Tooltip("GameObject yang merepresentasikan player.")]
    [SerializeField] private GameObject player;
    [Tooltip("Status apakah player sedang duduk atau tidak.")]
    [SerializeField] private bool isPlayerSitting;
    [Tooltip("Module input untuk menangani input dari player.")]
    [SerializeField] private _ModuleInputPlay input;
    [Tooltip("Player components reference.")]
    private Player_Components playerComponents;
    private bool isPlayerInTrigger;

    // Subscribe to input events when player enters trigger, and unsubscribe when they exit or stand up
    void OnEnable()
    {
        
    }

    // Unsubscribe from input events when object is disabled to prevent memory leaks
    void OnDisable()
    {
        if (input != null)
        {
            input.OnAction -= HandleInputAction;
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.gameObject;
            playerComponents = player.GetComponent<Player_Components>();
            
            if (playerComponents != null)
            {
                input = playerComponents.moduleInputPlay;
            }
            
            isPlayerInTrigger = true;
            
            // Subscribe to input events
            if (input != null)
            {
                input.OnAction += HandleInputAction;
            }
        }
    }

    public void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (isPlayerSitting)
            {
                OnParent();
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.gameObject == player)
        {
            isPlayerInTrigger = false;
            
            // DON'T stand up automatically - let the player use Back button to stand up
            // The sit position might be outside the trigger, so OnTriggerExit will fire
            // but we want the player to remain sitting until they press Back
            
            // Only unsubscribe and cleanup if player is NOT sitting
            if (!isPlayerSitting)
            {
                // Unsubscribe from input events
                if (input != null)
                {
                    input.OnAction -= HandleInputAction;
                }
                
                player = null;
                playerComponents = null;
                input = null;
            }
            // If player IS sitting, keep the input subscription active so they can press Back to stand
        }
    }

    private void HandleInputAction(ActionState actionState)
    {
        
        // Check if player is in trigger and pressed Interact
        if (isPlayerInTrigger && actionState == ActionState.Interact)
        {
            OnSit();
        }

        // Allow Cancel button to work even if player is outside trigger (since sit position might be outside trigger bounds)
        if (actionState == ActionState.Cancel && isPlayerSitting)
        {
            OnStandUp();
            
            // Cleanup after standing up
            if (input != null)
            {
                input.OnAction -= HandleInputAction;
            }
            player = null;
            playerComponents = null;
            input = null;
        }
    }
    
    private void OnSit()
    {
        if (!isPlayerSitting)
        {
            isPlayerSitting = true;

            if (player != null && playerComponents != null)
            {
                // Disable CharacterController if present (prevents it from interfering with transform)
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.enabled = false;
                }
                
                // Disable player movement
                playerComponents.enabled = false;
                
                OnParent();
                Debug.Log($"{player.name} is now sitting.");
            }
        }
    }

    private void OnParent()
    {
        if (player != null && sitPosition != null)
        {
            if (player.transform.parent != sitPosition)
            {
                player.transform.SetParent(sitPosition);
                Debug.Log($"<color=blue>Parenting {player.name} to sit position {sitPosition.name}</color>");
            }

            // Always reset position and rotation to ensure proper sitting
            player.transform.localPosition = UnityEngine.Vector3.zero;
            player.transform.localRotation = UnityEngine.Quaternion.identity;
        }
    }
    
    private void OnStandUp()
    {
        if (isPlayerSitting)
        {
            isPlayerSitting = false;

            if (player != null && playerComponents != null)
            {
                // Unparent player
                player.transform.SetParent(null);
                
                // Re-enable player movement
                playerComponents.enabled = true;
                
                // Re-enable CharacterController if present
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.enabled = true;
                }
                
                Debug.Log($"{player.name} stood up.");
            }
        }
    }
}