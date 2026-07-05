using UnityEngine;
using UnityEngine.Events;

public class Sys_ObjResetManager : MonoBehaviour
{
    public UnityEvent onReset;

    public void OnResetObjects()
    {
        onReset?.Invoke();
    }
}
