using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.Events;
public class GP_CtsChoice : MonoBehaviour
{
    [Tooltip("The first choice button.")]
    public GP_CtsChoice_Btn choice1;
    [Tooltip("The second choice button.")]
    public GP_CtsChoice_Btn choice2;
    [Tooltip("The target object for the first choice.")]
    public GameObject choice1Target;
    [Tooltip("The target object for the second choice.")]
    public GameObject choice2Target;
    [Tooltip("The cutscene director(s) to control.")]
    public PlayableDirector[] cutsceneDirector;

    [Tooltip("The Cinemachine virtual camera.")]
    public CinemachineVirtualCameraBase vcam;

    [Header("Events")]
    [Tooltip("Event triggered when choice 1 is selected.")]
    public UnityEvent onChoice1Selected;
    [Tooltip("Event triggered when choice 2 is selected.")]
    public UnityEvent onChoice2Selected;

    [Header("Debug")]
    public GameObject currentTarget;

    public void OnChoiceSelected(GP_CtsChoice_Btn selectedChoice)
    {
        if (selectedChoice == choice1)
        {
            Debug.Log("<color=green>Choice 1 selected!</color>");
            if (choice1Target != null)
            {
                vcam.LookAt = choice1Target.transform;
                currentTarget = choice1Target;
                onChoice1Selected?.Invoke();
            }
        }
        else if (selectedChoice == choice2)
        {
            Debug.Log("<color=yellow>Choice 2 selected!</color>");
            if (choice2Target != null)
            {
                vcam.LookAt = choice2Target.transform;
                currentTarget = choice2Target;
                onChoice2Selected?.Invoke();
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

    public void OnChoiceClicked(GP_CtsChoice_Btn selectedChoice)
    {
        if (selectedChoice == choice1)
        {
            Debug.Log("<color=green>Choice 1 clicked!</color>");
            // Play the cutscene for choice 1
            if (cutsceneDirector.Length > 0 && cutsceneDirector[0] != null)
            {
                cutsceneDirector[0].Play();
            }
        }
        else if (selectedChoice == choice2)
        {
            Debug.Log("<color=yellow>Choice 2 clicked!</color>");
            // Play the cutscene for choice 2
            if (cutsceneDirector.Length > 1 && cutsceneDirector[1] != null)
            {
                cutsceneDirector[1].Play();
            }
        }
    }
}