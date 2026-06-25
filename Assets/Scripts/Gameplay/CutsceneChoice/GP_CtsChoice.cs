using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GP_CtsChoice : MonoBehaviour, ISelectHandler
{
    [Tooltip("The first choice button.")]
    public Button choice0;
    public Button choice1;
    public Button choice2;

    void Start()
    {
        if (choice0 != null)
        {
            EventSystem.current.SetSelectedGameObject(choice0.gameObject);
        }
    }

    public void OnRandomChoiceSelected()
    {
        int randomChoice = Random.Range(0, 2);
        if (randomChoice == 0 && choice1 != null)
        {
            EventSystem.current.SetSelectedGameObject(choice1.gameObject);
            InvokeChoiceOnClick(choice1);
        }
        else if (randomChoice == 1 && choice2 != null)
        {
            EventSystem.current.SetSelectedGameObject(choice2.gameObject);
            InvokeChoiceOnClick(choice2);
        }
    }

    private void InvokeChoiceOnClick(Button choiceButton)
    {
        if (choiceButton != null)
        {
            choiceButton.onClick.Invoke();
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        GameObject selectedObject = eventData != null ? eventData.selectedObject : null;
        if (selectedObject == null)
        {
            return;
        }

        if ((choice1 != null && selectedObject == choice1.gameObject) ||
            (choice2 != null && selectedObject == choice2.gameObject))
        {
            Debug.Log($"<color=green>Choice selected: {selectedObject.name}</color>");
            DisableChoice0();
        }
    }

    public void DisableChoice0()
    {
        if (choice0 != null)
        {
            choice0.interactable = false;
        }
    }

    public void OnPlayTimeline(PlayableDirector director)
    {
        if (director != null)
        {
            director.Play();
        }
    }
}

#region Backup
// using UnityEngine;
// using UnityEngine.UI;
// using Unity.Cinemachine;
// using UnityEngine.EventSystems;
// using UnityEngine.Playables;
// using UnityEngine.Events;
// public class GP_CtsChoice : MonoBehaviour
// {
//     [Tooltip("The first choice button.")]
//     public GP_CtsChoice_Btn choice1;
//     [Tooltip("The second choice button.")]
//     public GP_CtsChoice_Btn choice2;
//     [Tooltip("The target object for the first choice.")]
//     public GameObject choice1Target;
//     [Tooltip("The target object for the second choice.")]
//     public GameObject choice2Target;
//     [Tooltip("The cutscene director(s) to control.")]
//     public PlayableDirector[] cutsceneDirector;

//     [Tooltip("The Cinemachine virtual camera.")]
//     public CinemachineVirtualCameraBase vcam;

//     [Header("Events")]
//     [Tooltip("Event triggered when choice 1 is selected.")]
//     public UnityEvent onChoice1Selected;
//     [Tooltip("Event triggered when choice 2 is selected.")]
//     public UnityEvent onChoice2Selected;

//     [Header("Debug")]
//     public GameObject currentTarget;

//     public void OnChoiceSelected(GP_CtsChoice_Btn selectedChoice)
//     {
//         if (selectedChoice == choice1)
//         {
//             Debug.Log("<color=green>Choice 1 selected!</color>");
//             if (choice1Target != null)
//             {
//                 vcam.LookAt = choice1Target.transform;
//                 currentTarget = choice1Target;
//                 onChoice1Selected?.Invoke();
//             }
//         }
//         else if (selectedChoice == choice2)
//         {
//             Debug.Log("<color=yellow>Choice 2 selected!</color>");
//             if (choice2Target != null)
//             {
//                 vcam.LookAt = choice2Target.transform;
//                 currentTarget = choice2Target;
//                 onChoice2Selected?.Invoke();
//             }
//         }

//         // Switch camera
//         if (vcam != null)
//         {
//             _Sys_VCamPriorityController vcamController = FindAnyObjectByType<_Sys_VCamPriorityController>();
//             if (vcamController != null)
//             {
//                 vcamController.SetCameraActive(vcam);
//             }
//         }
//     }

//     public void OnChoiceClicked(GP_CtsChoice_Btn selectedChoice)
//     {
//         if (selectedChoice == choice1)
//         {
//             Debug.Log("<color=green>Choice 1 clicked!</color>");
//             // Play the cutscene for choice 1
//             if (cutsceneDirector.Length > 0 && cutsceneDirector[0] != null)
//             {
//                 cutsceneDirector[0].Play();
//             }
//         }
//         else if (selectedChoice == choice2)
//         {
//             Debug.Log("<color=yellow>Choice 2 clicked!</color>");
//             // Play the cutscene for choice 2
//             if (cutsceneDirector.Length > 1 && cutsceneDirector[1] != null)
//             {
//                 cutsceneDirector[1].Play();
//             }
//         }
//     }
// }
#endregion