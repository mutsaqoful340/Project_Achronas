using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using UnityEngine.EventSystems;

public class GP_CtsChoice_Btn : MonoBehaviour, ISelectHandler, ISubmitHandler
{
    public Button choiceButton;
    public GP_CtsChoice cutsceneChoice;
        
    public void OnSelect(BaseEventData eventData)
    {
        if (cutsceneChoice != null)
        {
            cutsceneChoice.OnChoiceSelected(this);
        }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (cutsceneChoice != null)
        {
            cutsceneChoice.OnChoiceClicked(this);
        }
    }
}
