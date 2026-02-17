using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// This script will funtion as an object that will be followed by the Cinemashine Target.
/// It will contain player objects and calculates stuff.
/// This will NOT be attached to the Cinemachine object, instead it will be attached to an object that will be followed by the Cinemachine Target.
/// The Cinemachine Target will then follow this object, which will allow us to have more control over the camera's behavior and settings.
/// </summary>

public class _Sys_VCamBehaviourController : MonoBehaviour
{
    [Header("Player Objects")]
    public Transform player1;
    public Transform player2;

    [Header("Camera Target Group Settings")]
    public float radius = 0.5f;
    public float maxPlayerDistance = 10f; // The maximum distance between the players before invoke events or change camera behavior.

    [Header("Weight Smoothing")]
    [Tooltip("How fast the weight transitions between players (0-1, higher = faster)")]
    [SerializeField] private float weightTransitionSpeed = 0.1f;
    private float currentPlayer1Weight = 1f;
    private float currentPlayer2Weight = 1f;

    [Header("Distance Check Optimization")]
    [Tooltip("How often to check player distance in seconds")]
    [SerializeField] private float distanceCheckInterval = 0.2f;
    private float nextDistanceCheckTime = 0f;
    private bool hasDistanceEventTriggered = false;

    [Header("Movement Detection")]
    [Tooltip("Minimum velocity threshold to consider a player as moving")]
    [SerializeField] private float movementThreshold = 0.01f;

    [Header("Events")]
    public UnityEvent onPlayerDistanceExceeded;

    // Tracking variables
    private Vector3 player1PreviousPosition;
    private Vector3 player2PreviousPosition;

    private void Start()
    {
        if (player1 != null)
            player1PreviousPosition = player1.position;
        if (player2 != null)
            player2PreviousPosition = player2.position;
    }

    private void Update()
    {
        if (player1 == null || player2 == null)
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
        Vector3 midpoint = (player1.position + player2.position) / 2f;
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
        Vector3 player1Velocity = player1.position - player1PreviousPosition;
        Vector3 player2Velocity = player2.position - player2PreviousPosition;

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
        player1PreviousPosition = player1.position;
        player2PreviousPosition = player2.position;
    }

    /// <summary>
    /// Checks the distance between players at intervals and only when they're moving.
    /// If distance exceeds maxPlayerDistance, invokes the onPlayerDistanceExceeded event.
    /// This is optimized to avoid checking every frame.
    /// </summary>
    private void CheckPlayerDistance()
    {
        // Only check if players are moving
        float player1Speed = (player1.position - player1PreviousPosition).magnitude;
        float player2Speed = (player2.position - player2PreviousPosition).magnitude;

        bool playersMoving = player1Speed > movementThreshold || player2Speed > movementThreshold;

        // OPTIMIZATION: Check distance at intervals only when players are moving
        if (playersMoving && Time.time >= nextDistanceCheckTime)
        {
            nextDistanceCheckTime = Time.time + distanceCheckInterval;

            float distance = Vector3.Distance(player1.position, player2.position);

            if (distance > maxPlayerDistance)
            {
                if (!hasDistanceEventTriggered)
                {
                    onPlayerDistanceExceeded?.Invoke();
                    hasDistanceEventTriggered = true;
                }
            }
            else
            {
                hasDistanceEventTriggered = false;
            }
        }
    }
}
