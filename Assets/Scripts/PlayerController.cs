using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

/// <summary>
/// Управление персонажем через новый Input System:
///  - WASD / стрелки: движение относительно камеры по плоскости пола;
///  - левый клик по полу/предметам: перемещение в точку.
/// </summary>
public class PlayerController : MonoBehaviour
{
    /// <summary>
    /// Тег комнаты, в которой сейчас находится игрок.
    /// Используется соседом для проверки видимости в одной комнате.
    /// </summary>
    public static string CurrentRoomTag { get; private set; }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Click/Tap Settings")]
    [SerializeField] private LayerMask groundLayer = -1;
    [SerializeField] private float raycastDistance = 100f;

    [Header("Animation")]
    [SerializeField] private Animator _animator;

    private NavMeshAgent _agent;
    private Camera _mainCamera;
    private Rigidbody _rb;

    private void Awake()
    {
        // Сбрасываем статические поля при перезапуске сцены,
        // иначе после SceneManager.LoadScene они сохраняют старые значения.
        CurrentRoomTag = null;
        HideInteraction.ResetHiddenState();

        _mainCamera = Camera.main;

        // Rigidbody для корректной работы OnTriggerEnter (требуется физическим движком)
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody>();
            _rb.isKinematic = true;
        }

        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
        {
            _agent = gameObject.AddComponent<NavMeshAgent>();
        }
        
        _agent.speed = moveSpeed;
        _agent.updateRotation = false; // Мы вращаем сами в FaceDirection

        // Поиск Animator на дочернем объекте Character
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        // Тег Player для распознавания дверьми
        if (!CompareTag("Player"))
            gameObject.tag = "Player";
    }

    private void Update()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        bool isMoving = false;

        // Клавиатура имеет приоритет: при ручном управлении отменяем путь.
        if (HandleKeyboard())
        {
            if (_agent.hasPath)
                _agent.ResetPath();
            isMoving = true;
        }
        else
        {
            HandleClick();

            if (_agent.hasPath && _agent.velocity.sqrMagnitude > 0.01f)
            {
                FaceDirection(_agent.velocity.normalized);
                isMoving = true;
            }
        }

        UpdateAnimator(isMoving);
    }

    private void UpdateAnimator(bool isMoving)
    {
        if (_animator != null)
            _animator.SetBool("IsWalking", isMoving);
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

        _agent.Move(direction * (moveSpeed * Time.deltaTime));
        FaceDirection(direction);
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

        _agent.SetDestination(hit.point);
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

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, является ли триггер комнатой
        string tag = other.tag;
        if (tag == "Kitchen" || tag == "Hallway" || tag == "Livingroom" || tag == "Bathroom" || tag == "Entrance")
        {
            CurrentRoomTag = tag;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // При выходе из комнаты сбрасываем, если это была текущая комната
        if (other.tag == CurrentRoomTag)
        {
            CurrentRoomTag = null;
        }
    }
}
