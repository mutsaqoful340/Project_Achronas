using UnityEngine;

public enum SurfaceType
{
    Concrete,
    Wood,
    Marble
}

public class Surface : MonoBehaviour
{
    public SurfaceType surfaceType;
}