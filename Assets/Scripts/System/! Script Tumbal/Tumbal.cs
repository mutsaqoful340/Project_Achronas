using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.Video;

public class Tumbal : MonoBehaviour
{
    [SerializeField] private GameObject player;

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (player != null)
            {
                player = null; // Clear player reference when they exit the trigger
            }
        }
    }

    public GameObject parentObj;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false; // Disable CharacterController to allow physics to take over
            }
        }
    }

    public void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Call the parenting method while the player is within the trigger
            OnParent();
        }
    }

    void Update()
    {
        // Condition for the parenting method to be called.
        OnParent();
    }

    private void OnParent()
    {
        if (parentObj != null)
        {
            if (transform.parent != parentObj.transform)
            {
                transform.SetParent(parentObj.transform);
                Debug.Log($"<color=blue>Parenting {gameObject.name} to {parentObj.name}</color>");
            }

            // Always reset position and rotation to ensure proper parenting
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }
}
