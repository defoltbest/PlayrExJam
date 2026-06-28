using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Обрабатывает клик левой кнопкой мыши (новый Input System): пускает луч из камеры
/// и переключает дверь (Door), если попал по её полотну.
/// </summary>
[RequireComponent(typeof(Camera))]
public class DoorClickController : MonoBehaviour
{
    [SerializeField] float maxDistance = 500f;

    Camera cam;

    void Awake() => cam = GetComponent<Camera>();

    void Update()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        var mouse = Mouse.current;
        if (mouse == null || cam == null)
            return;

        if (!mouse.leftButton.wasPressedThisFrame)
            return;

        var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out var hit, maxDistance))
        {
            var door = hit.collider.GetComponentInParent<Door>();
            if (door != null)
                door.Toggle();
        }
    }
}
