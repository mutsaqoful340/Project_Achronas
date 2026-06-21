using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public class GP_CtsChoice : MonoBehaviour
{
    [Tooltip("The first choice button.")]
    public Button choice1;
    [Tooltip("The second choice button.")]
    public Button choice2;
    [Tooltip("The target object for the first choice.")]
    public GameObject choice1Target;
    [Tooltip("The target object for the second choice.")]
    public GameObject choice2Target;

    [Tooltip("The Cinemachine virtual camera to switch to.")]
    public CinemachineVirtualCameraBase vcam;

    private void Start()
    {
        // Set up listeners ONCE during initialization
        if (choice1 != null)
        {
            choice1.onClick.AddListener(() => OnChoiceSelected(choice1));
        }
        if (choice2 != null)
        {
            choice2.onClick.AddListener(() => OnChoiceSelected(choice2));
        }
    }

    private void OnChoiceSelected(Button selectedButton)
    {
        if (selectedButton == choice1)
        {
            Debug.Log("<color=green>Choice 1 selected!</color>");
            if (choice1Target != null)
            {
                vcam.LookAt = choice1Target.transform;
            }
        }
        else if (selectedButton == choice2)
        {
            Debug.Log("<color=blue>Choice 2 selected!</color>");
            if (choice2Target != null)
            {
                vcam.LookAt = choice2Target.transform;
            }
        }

        // Switch camera
        if (vcam != null)
        {
            _Sys_VCamPriorityController vcamController = FindAnyObjectByType<_Sys_VCamPriorityController>();
            if (vcamController != null)
            {
                vcamController.SetCameraActive(vcam);
            }
        }
    }
}
