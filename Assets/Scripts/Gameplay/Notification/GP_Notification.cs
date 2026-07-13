using UnityEngine;
using TMPro;

public class GP_Notification : MonoBehaviour
{
    public TextMeshProUGUI notificationText;
    public Animator animator;

    public void OnShowNotification(string message)
    {
        notificationText.text = message;
        animator.SetTrigger("Show");
    }
}