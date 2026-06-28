using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Управление персонажем через новый Input System:
///  - WASD / стрелки: движение относительно камеры по плоскости пола;
///  - левый клик по полу/предметам: перемещение в точку.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Click/Tap Settings")]
    [SerializeField] private LayerMask groundLayer = -1;
    [SerializeField] private float raycastDistance = 100f;

    private Vector3 _targetPosition;
    private bool _hasTarget;
    private Camera _mainCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _targetPosition = transform.position;
    }

    private void Update()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        // Клавиатура имеет приоритет: при ручном управлении отменяем клик-цель.
        if (HandleKeyboard())
            return;

        HandleClick();
        MoveTowardsTarget();
    }

    private bool HandleKeyboard()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        var input = Vector2.zero;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;

        if (input.sqrMagnitude < 0.01f)
            return false;

        var forward = PlanarAxis(_mainCamera != null ? _mainCamera.transform.forward : Vector3.forward);
        var right = PlanarAxis(_mainCamera != null ? _mainCamera.transform.right : Vector3.right);
        var direction = (forward * input.y + right * input.x).normalized;

        transform.position += direction * (moveSpeed * Time.deltaTime);
        FaceDirection(direction);
        _hasTarget = false;
        return true;
    }

    private void HandleClick()
    {
        var mouse = Mouse.current;
        if (mouse == null || _mainCamera == null)
            return;

        if (!mouse.leftButton.wasPressedThisFrame)
            return;

        var ray = _mainCamera.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out var hit, raycastDistance, groundLayer))
            return;

        _targetPosition = new Vector3(hit.point.x, transform.position.y, hit.point.z);
        _hasTarget = true;
    }

    private void MoveTowardsTarget()
    {
        if (!_hasTarget) return;

        var direction = _targetPosition - transform.position;
        var distance = direction.magnitude;

        if (distance < 0.05f)
        {
            transform.position = _targetPosition;
            _hasTarget = false;
            return;
        }

        var moveStep = direction.normalized * (moveSpeed * Time.deltaTime);
        if (moveStep.magnitude > distance)
            moveStep = direction;

        transform.position += moveStep;
        FaceDirection(direction);
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        var targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    // Ось камеры, спроецированная на горизонтальную плоскость (для движения по полу).
    private static Vector3 PlanarAxis(Vector3 axis)
    {
        axis.y = 0f;
        return axis.sqrMagnitude < 0.0001f ? Vector3.forward : axis.normalized;
    }
}
