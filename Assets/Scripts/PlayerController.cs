using UnityEngine;

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
        HandleInput();
        MoveTowardsTarget();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, groundLayer))
            {
                _targetPosition = new Vector3(hit.point.x, transform.position.y, hit.point.z);
                _hasTarget = true;
            }
        }
    }

    private void MoveTowardsTarget()
    {
        if (!_hasTarget) return;

        Vector3 direction = _targetPosition - transform.position;
        float distance = direction.magnitude;

        if (distance < 0.1f)
        {
            transform.position = _targetPosition;
            _hasTarget = false;
            return;
        }

        Vector3 moveStep = direction.normalized * moveSpeed * Time.deltaTime;
        if (moveStep.magnitude > distance)
            moveStep = direction;

        transform.position += moveStep;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}