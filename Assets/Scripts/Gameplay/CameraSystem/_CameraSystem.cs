using UnityEngine;
using Unity.Cinemachine;
using TMPro;

public class _CameraSystem : MonoBehaviour
{
    [Header("Cinemachine Target Group Reference")]
    public CinemachineTargetGroup targetGroup;
    public TextMeshProUGUI _targetGroupStretch;

    private void Update()
    {
        if (targetGroup != null)
        {
            // Get the bounding sphere radius to show the stretch
            _targetGroupStretch.text = "CTG Stretch: " + targetGroup.Sphere.radius.ToString();
        }
    }
}
