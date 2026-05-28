using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// This script will funtion as an object that will be followed by the Cinemashine Target.
/// It will contain player objects and calculates stuff.
/// This will NOT be attached to the Cinemachine object, instead it will be attached to an object that will be followed by the Cinemachine Target.
/// The Cinemachine Target will then follow this object, which will allow us to have more control over the camera's behavior and settings.
/// </summary>

public class Sys_PlayerConstraint : MonoBehaviour
{
    [Header("References")]
    public Transform playerRinda;
    public Transform playerNaya;
    public GP_PlayerSanity playerSanity;

    [Header("Camera Target Group Settings")]
    public float maxPlayerDistance = 10f; // The maximum distance between the players before invoke events or change camera behavior.

    [Header("Weight Smoothing")]
    [Tooltip("How fast the weight transitions between players (0-1, higher = faster)")]
    [SerializeField] private float weightTransitionSpeed = 0.1f;
    private float currentPlayer1Weight = 1f;
    private float currentPlayer2Weight = 1f;

    [Header("Distance Check Optimization")]
    [Tooltip("How often to check player distance in seconds")]
    [SerializeField] private float distanceCheckInterval = 0.2f;
    private bool hasDistanceEventTriggered = false;
    private bool arePlayersSeparated = false;

    [Header("Movement Detection")]
    [Tooltip("Minimum velocity threshold to consider a player as moving")]
    [SerializeField] private float movementThreshold = 0.01f;

    [Header("Events")]
    public UnityEvent OnPlayerDistanceExceeded;

    // Tracking variables
    private Vector3 player1PreviousPosition;
    private Vector3 player2PreviousPosition;

    private void Start()
    {
        if (playerRinda != null)
            player1PreviousPosition = playerRinda.position;
        if (playerNaya != null)
            player2PreviousPosition = playerNaya.position;
    }

    private void Update()
    {
        if (playerRinda == null || playerNaya == null)
            return;

        // Behaviour 1: Calculate the average position of the players and set it as the position of this object.
        CalculateAveragePosition();

        // Behaviour 2: Calculate which player is moving and adjust weights dynamically.
        CalculateMovementWeights();

        // Check distance between players (optimized - only when players are moving and at intervals)
        CheckPlayerDistance();
    }

    /// <summary>
    /// Calculates the average position of both players and sets this object's position to that point.
    /// </summary>
    private void CalculateAveragePosition()
    {
        Vector3 midpoint = (playerRinda.position + playerNaya.position) / 2f;
        transform.position = midpoint;
    }

    /// <summary>
    /// Calculates which player is moving and adjusts their respective weights smoothly.
    /// If both players are moving equally, weights become equal.
    /// If one player is moving more, their weight increases over time.
    /// </summary>
    private void CalculateMovementWeights()
    {
        // Calculate velocities based on position change
        Vector3 player1Velocity = playerRinda.position - player1PreviousPosition;
        Vector3 player2Velocity = playerNaya.position - player2PreviousPosition;

        float player1Speed = player1Velocity.magnitude;
        float player2Speed = player2Velocity.magnitude;

        // Determine target weights based on movement
        float targetPlayer1Weight = 1f;
        float targetPlayer2Weight = 1f;

        // If both are moving, keep weights equal
        if (player1Speed > movementThreshold && player2Speed > movementThreshold)
        {
            targetPlayer1Weight = 1f;
            targetPlayer2Weight = 1f;
        }
        // If only player1 is moving, increase their weight
        else if (player1Speed > movementThreshold)
        {
            targetPlayer1Weight = 1.5f;
            targetPlayer2Weight = 1f;
        }
        // If only player2 is moving, increase their weight
        else if (player2Speed > movementThreshold)
        {
            targetPlayer1Weight = 1f;
            targetPlayer2Weight = 1.5f;
        }
        // If neither is moving, keep weights equal
        else
        {
            targetPlayer1Weight = 1f;
            targetPlayer2Weight = 1f;
        }

        // Smoothly interpolate to target weights
        currentPlayer1Weight = Mathf.Lerp(currentPlayer1Weight, targetPlayer1Weight, weightTransitionSpeed * Time.deltaTime);
        currentPlayer2Weight = Mathf.Lerp(currentPlayer2Weight, targetPlayer2Weight, weightTransitionSpeed * Time.deltaTime);

        // Update previous positions for next frame
        player1PreviousPosition = playerRinda.position;
        player2PreviousPosition = playerNaya.position;
    }

    /// <summary>
    /// Checks the distance between players every frame.
    /// Separated > Depletes
    /// Together & Rinda not depressed & not carrying > Recovers
    /// Together & Rinda depressed & not carrying > Stays at 0
    /// Together & Rinda depressed & carrying > Recovers
    /// </summary>
    private void CheckPlayerDistance()
    {
        float distance = Vector3.Distance(playerRinda.position, playerNaya.position);

        if (distance > maxPlayerDistance)
        {
            // SEPARATED: Depletes sanity
            if (!arePlayersSeparated)
            {
                arePlayersSeparated = true;
                // Debug.Log("Players separated! Sanity depletion started.");
                OnPlayerDistanceExceeded.Invoke();
                hasDistanceEventTriggered = true;
            }

            if (playerSanity != null)
            {
                playerSanity.DepleteSanity();
            }
        }
        else
        {
            // TOGETHER: Handle different conditions
            if (arePlayersSeparated)
            {
                arePlayersSeparated = false;
                hasDistanceEventTriggered = false;
                // Debug.Log("Players reunited! Sanity depletion stopped.");
            }

            // Determine if sanity should recover (only if below 100)
            if (playerSanity != null && playerRinda != null && playerSanity.sanityLevel < 100f)
            {
                var rindaCC = playerRinda.GetComponent<Player_Components>();
                bool rindaDepressed = rindaCC.currentActionState == ActionState.Depressed;
                bool rindaBeingCarried = playerSanity.IsCarried;

                if (!rindaDepressed)
                {
                    // Together & Not depressed > Recover
                    playerSanity.RecoverSanity();
                    // Debug.Log("Rinda not depressed - recovering sanity");
                }
                else if (rindaDepressed && rindaBeingCarried)
                {
                    // Together & Depressed & Carrying > Recover
                    playerSanity.RecoverSanity();
                    // Debug.Log("Rinda being carried - recovering sanity");
                }
                else if (rindaDepressed && !rindaBeingCarried)
                {
                    // Together & Depressed & NOT carrying > Stay at 0
                    // Debug.Log("Rinda depressed and not being carried - sanity stays at 0");
                }
            }
        }
    }

    /// <summary>
    /// Draws gizmos for debugging player distance.
    /// Green line when players are within maxPlayerDistance, red when they exceed it.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Only draw if both players are assigned
        if (playerRinda == null || playerNaya == null)
            return;

        // Calculate distance between players
        float distance = Vector3.Distance(playerRinda.position, playerNaya.position);

        // Set color based on whether distance exceeds maxPlayerDistance
        Gizmos.color = distance > maxPlayerDistance ? Color.red : Color.green;

        // Draw line between players
        Gizmos.DrawLine(playerRinda.position, playerNaya.position);

        // Draw spheres at player positions for clarity
        Gizmos.DrawWireSphere(playerRinda.position, 0.2f);
        Gizmos.DrawWireSphere(playerNaya.position, 0.2f);
    }
}
