using UnityEngine;

/// <summary>
/// Плавное следование камеры за целью с фиксированным смещением.
/// Камера двигается с использованием SmoothDamp для мягкого перемещения.
/// Поворот камеры выставляется ОДИН раз при старте (на цель) и больше не меняется.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Offset")]
    [SerializeField] private Vector3 offset;

    [Header("Smooth Settings")]
    [SerializeField] private float smoothTime;
    [SerializeField] private float maxSpeed;

    [Header("Look At")]
    [SerializeField] private bool lookAtTarget;
    [SerializeField] private Vector3 lookAtOffset;

    private Vector3 _velocity = Vector3.zero;
    private Camera _camera;
    [SerializeField] private bool snapOnStart;

    private void Awake()
    {
        _camera = GetComponent<Camera>();

        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
    }

    private void Start()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Плавное движение позиции
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _velocity,
            smoothTime,
            maxSpeed
        );

        // Постоянно смотрим на игрока
        if (lookAtTarget)
        {
            Vector3 lookTarget = target.position + lookAtOffset;
            transform.LookAt(lookTarget);
        }
    }

    /// <summary>
    /// Установить цель для следования.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// Мгновенно переместить камеру к цели (без сглаживания).
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null)
            return;

        transform.position = target.position + offset;
        _velocity = Vector3.zero;

        if (lookAtTarget)
        {
            Vector3 lookTarget = target.position + lookAtOffset;
            transform.LookAt(lookTarget);
        }
    }
}