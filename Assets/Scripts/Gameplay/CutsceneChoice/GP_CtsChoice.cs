using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public class GP_CtsChoice : MonoBehaviour
{
    public Button choice1;
    public Button choice2;
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
        }
        else if (selectedButton == choice2)
        {
            Debug.Log("<color=blue>Choice 2 selected!</color>");
        }

        // Switch camera
        if (vcam != null)
        {
            _Sys_VCamPriorityController vcamController = FindObjectOfType<_Sys_VCamPriorityController>();
            if (vcamController != null)
            {
                vcamController.SetCameraActive(vcam);
            }
        }
    }
}
