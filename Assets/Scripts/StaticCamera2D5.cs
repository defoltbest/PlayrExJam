using UnityEngine;

/// <summary>
/// Locks the camera to a fixed side-view position (dollhouse cross-section).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class StaticCamera2D5 : MonoBehaviour
{
    Vector3 fixedPosition;
    float fixedOrthographicSize;
    bool configured;

    public void ConfigureSideView(Vector3 position, float orthographicSize)
    {
        fixedPosition = position;
        fixedOrthographicSize = orthographicSize;
        configured = true;
        Apply();
    }

    void LateUpdate()
    {
        if (!Application.isPlaying || !configured)
            return;

        Apply();
    }

    void Apply()
    {
        var camera = GetComponent<Camera>();
        transform.SetPositionAndRotation(fixedPosition, Quaternion.identity);
        camera.orthographic = true;
        camera.orthographicSize = fixedOrthographicSize;
    }
}
