using UnityEngine;

public class GP_CtsParentTransition : MonoBehaviour
{
    private Vector3 positionVelocity = Vector3.zero;
    private GameObject oldParent;
    [SerializeField]
    private float smoothTime = 0.1f;
    private float transitionThreshold = 0.001f;

    private Transform currentChild;
    private bool isTransitioning = false;

    public void OnTransition(GameObject child)
    {
        currentChild = child.transform;
        oldParent = currentChild.parent.gameObject;
        currentChild.SetParent(transform);
        isTransitioning = true;
        positionVelocity = Vector3.zero;
        
        // Disable player controller
        CharacterController characterController = currentChild.GetComponent<CharacterController>();
        if (characterController != null)
            characterController.enabled = false;
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
        if (currentChild == null || oldParent == null) return;

        // Store world position/rotation before reparenting
        Vector3 worldPos = currentChild.position;
        Quaternion worldRot = currentChild.rotation;
        
        // Reparent to old parent
        currentChild.SetParent(oldParent.transform);
        
        // Restore world position/rotation
        currentChild.position = worldPos;
        currentChild.rotation = worldRot;
    }
}
