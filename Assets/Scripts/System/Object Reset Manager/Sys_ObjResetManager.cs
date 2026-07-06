using UnityEngine;
using UnityEngine.Events;

public class Sys_ObjResetManager : MonoBehaviour
{
    public GameObject[] objectsToReset; // reference to the objects you want to reset (position, rotation, scale)
    public UnityEvent onReset;

    private Vector3[] initialPositions;
    private Quaternion[] initialRotations;
    private Vector3[] initialScales;

    void Awake()
    {
        if (objectsToReset == null)
        {
            return;
        }

        initialPositions = new Vector3[objectsToReset.Length];
        initialRotations = new Quaternion[objectsToReset.Length];
        initialScales = new Vector3[objectsToReset.Length];

        for (int i = 0; i < objectsToReset.Length; i++)
        {
            GameObject obj = objectsToReset[i];
            if (obj == null)
            {
                continue;
            }

            Transform targetTransform = obj.transform;
            initialPositions[i] = targetTransform.position;
            initialRotations[i] = targetTransform.rotation;
            initialScales[i] = targetTransform.localScale;
        }
    }
    
    public void OnResetObjects()
    {
        onReset?.Invoke();
        ResetObjects();
    }

    private void ResetObjects()
    {
        if (objectsToReset == null || initialPositions == null || initialRotations == null || initialScales == null)
        {
            return;
        }

        for (int i = 0; i < objectsToReset.Length; i++)
        {
            GameObject obj = objectsToReset[i];
            if (obj == null)
            {
                continue;
            }

            Transform targetTransform = obj.transform;
            targetTransform.position = initialPositions[i];
            targetTransform.rotation = initialRotations[i];
            targetTransform.localScale = initialScales[i];
        }
    }
}
