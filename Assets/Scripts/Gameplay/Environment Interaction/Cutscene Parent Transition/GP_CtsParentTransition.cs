using UnityEngine;
using UnityEngine.AI;

public class GP_CtsParentTransition : MonoBehaviour
{
    private Vector3 positionVelocity = Vector3.zero;
    private GameObject oldParent;
    [SerializeField]
    private float smoothTime = 0.1f;
    private float transitionThreshold = 0.001f;

    private Transform currentChild;
    private bool isTransitioning = false;

    // Cache movement components so we can restore their previous enabled state.
    private CharacterController cachedCharacterController;
    private NavMeshAgent cachedNavMeshAgent;
    private bool wasCharacterControllerEnabled;
    private bool wasNavMeshAgentEnabled;

    public void OnTransition(GameObject child)
    {
        currentChild = child.transform;
        oldParent = currentChild.parent != null ? currentChild.parent.gameObject : null;
        currentChild.SetParent(transform);
        isTransitioning = true;
        positionVelocity = Vector3.zero;

        // Disable movement components while transitioning (supports player and boss).
        cachedCharacterController = currentChild.GetComponent<CharacterController>();
        if (cachedCharacterController != null)
        {
            wasCharacterControllerEnabled = cachedCharacterController.enabled;
            cachedCharacterController.enabled = false;
        }

        cachedNavMeshAgent = currentChild.GetComponent<NavMeshAgent>();
        if (cachedNavMeshAgent != null)
        {
            wasNavMeshAgentEnabled = cachedNavMeshAgent.enabled;
            cachedNavMeshAgent.enabled = false;
        }

        Player_Components playerComponents = currentChild.GetComponent<Player_Components>();
        if (playerComponents != null)
        {
            playerComponents.HandleInCutscene(true);
        }
    }

    private void Update()
    {
        if (!isTransitioning || currentChild == null) return;

        currentChild.localPosition = Vector3.SmoothDamp(currentChild.localPosition, Vector3.zero, ref positionVelocity, smoothTime);
        currentChild.localRotation = Quaternion.Slerp(currentChild.localRotation, Quaternion.identity, Time.deltaTime / Mathf.Max(0.0001f, smoothTime));

        // Stop transitioning when close enough to target
        // if (currentChild.localPosition.magnitude < transitionThreshold && 
        //     Quaternion.Angle(currentChild.localRotation, Quaternion.identity) < transitionThreshold)
        // {
        //     currentChild.localPosition = Vector3.zero;
        //     currentChild.localRotation = Quaternion.identity;
            
        //     ReparentToOldParent();
        //     isTransitioning = false;
        // }
    }

    public void ReparentToOldParent()
    {
        OnUnparent();
    }

    public void OnUnparent()
    {
        if (currentChild == null) return;

        isTransitioning = false;
        positionVelocity = Vector3.zero;

        // Preserve the current world pose while moving the child back.
        Vector3 worldPos = currentChild.position;
        Quaternion worldRot = currentChild.rotation;

        if (oldParent != null)
        {
            currentChild.SetParent(oldParent.transform);
        }
        else
        {
            currentChild.SetParent(null);
        }

        currentChild.position = worldPos;
        currentChild.rotation = worldRot;

        // Restore movement component states to what they were before transition.
        if (cachedCharacterController != null)
        {
            cachedCharacterController.enabled = wasCharacterControllerEnabled;
        }

        if (cachedNavMeshAgent != null)
        {
            cachedNavMeshAgent.enabled = wasNavMeshAgentEnabled;
        }
        
        Player_Components playerComponents = currentChild.GetComponent<Player_Components>();
        if (playerComponents != null)
        {
            playerComponents.HandleInCutscene(false);
        }

        cachedCharacterController = null;
        cachedNavMeshAgent = null;
        wasCharacterControllerEnabled = false;
        wasNavMeshAgentEnabled = false;

        currentChild = null;
        oldParent = null;
    }
}
