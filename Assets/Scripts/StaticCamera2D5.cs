using UnityEngine;

/// <summary>
/// Locks the camera to its scene settings during Play Mode.
/// Supports both perspective (volume) and orthographic modes.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class StaticCamera2D5 : MonoBehaviour
{
    Vector3 fixedPosition;
    Quaternion fixedRotation;
    bool useOrthographic;
    float fixedOrthographicSize;
    float fixedFieldOfView;
    bool configured;

    void Awake()
    {
        SyncFromTransform();
    }

    /// <summary>
    /// Captures the current transform and camera settings as the locked play-mode state.
    /// </summary>
    public void SyncFromTransform()
    {
        var camera = GetComponent<Camera>();
        fixedPosition = transform.position;
        fixedRotation = transform.rotation;
        useOrthographic = camera.orthographic;
        fixedOrthographicSize = camera.orthographicSize;
        fixedFieldOfView = camera.fieldOfView;
        configured = true;
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
        transform.SetPositionAndRotation(fixedPosition, fixedRotation);
        camera.orthographic = useOrthographic;

        if (useOrthographic)
            camera.orthographicSize = fixedOrthographicSize;
        else
            camera.fieldOfView = fixedFieldOfView;
    }
}
