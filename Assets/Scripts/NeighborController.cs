using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[Serializable]
public struct NeighborWaypoint
{
    [Tooltip("Точка на сцене, куда пойдет сосед")]
    public Transform point;
    
    [Tooltip("Время ожидания в этой точке (в секундах)")]
    public float waitTime;
}

[RequireComponent(typeof(NavMeshAgent))]
public class NeighborController : MonoBehaviour
{
    [Header("Настройки маршрута")]
    [Tooltip("Список точек, по которым будет ходить сосед. Он будет ходить по ним по порядку, а затем начнет с начала.")]
    public List<NeighborWaypoint> waypoints = new List<NeighborWaypoint>();

    [Header("Обзор")]
    [Tooltip("Дальность обзора соседа")]
    [SerializeField] private float visionRange = 10f;
    [Tooltip("Угол обзора в градусах (полный, т.е. 90 = по 45° в каждую сторону)")]
    [SerializeField, Range(0f, 180f)] private float visionAngle = 90f;
    [Tooltip("Время (в секундах), которое игрок должен провести в зоне видимости для проигрыша")]
    [SerializeField] private float detectionDelay = 0.7f;
    [Tooltip("Маска препятствий для лучей обзора")]
    [SerializeField] private LayerMask visionObstacleMask = -1;

    private NavMeshAgent _agent;
    private int _currentWaypointIndex = 0;
    private bool _isWaiting = false;

    // --- Обнаружение игрока ---
    private string _currentRoomTag;
    private float _detectionTimer;
    private Transform _playerTransform;
    private bool _playerDetected;
    private bool _isGameOver = false;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TimerController timerController;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        
        // Rigidbody для корректной работы OnTriggerEnter
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }
        
        // Тег Neighbor нужен, чтобы скрипт дверей открывал перед ним двери
        if (!CompareTag("Neighbor"))
        {
            Debug.LogWarning($"На соседе {gameObject.name} не установлен тег 'Neighbor'. Пожалуйста, создайте этот тег и назначьте его, иначе двери не будут открываться!");
        }
    }

    private void Start()
    {
        // Подписываемся на кнопку Retry в меню проигрыша
        if (gameOverMenu != null)
        {
            var retryButton = gameOverMenu.GetComponentInChildren<UnityEngine.UI.Button>();
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(RestartGame);
            }
        }

        if (waypoints.Count > 0)
        {
            GoToNextWaypoint();
        }
        else
        {
            Debug.LogWarning("У соседа нет ни одной точки в маршруте! Добавьте точки в массив Waypoints.");
        }
    }

    private void Update()
    {
        // Всегда проверяем обнаружение игрока (даже когда ждём на точке)
        DetectPlayer();

        if (waypoints.Count == 0 || _isWaiting)
            return;

        // Проверяем, дошел ли агент до цели
        // Используем небольшую погрешность (remainingDistance < 0.1), так как агент может остановиться чуть-чуть не дойдя
        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.1f)
        {
            if (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f)
            {
                StartCoroutine(WaitAndProceed());
            }
        }
    }

    /// <summary>
    /// Проверяет, находится ли игрок в зоне видимости соседа.
    /// Условия: одна комната, в радиусе, в угле обзора 90°, нет препятствий.
    /// Если игрок остаётся в зоне видимости detectionDelay секунд — игра окончена.
    /// </summary>
    private void DetectPlayer()
    {
        // Лениво находим игрока
        if (_playerTransform == null)
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
                _playerTransform = playerGo.transform;
            else
            {
                _detectionTimer = 0f;
                return;
            }
        }

        // --- УСЛОВИЕ 1: Находятся ли персонажи в одной комнате? ---
        string playerRoom = PlayerController.CurrentRoomTag;
        if (string.IsNullOrEmpty(_currentRoomTag) || string.IsNullOrEmpty(playerRoom) || _currentRoomTag != playerRoom)
        {
            _detectionTimer = 0f;
            _playerDetected = false;
            return;
        }

        // --- УСЛОВИЕ 2: Игрок в радиусе обзора? ---
        Vector3 directionToPlayer = _playerTransform.position - transform.position;
        directionToPlayer.y = 0f;
        float distance = directionToPlayer.magnitude;

        if (distance > visionRange)
        {
            _detectionTimer = 0f;
            _playerDetected = false;
            return;
        }

        // --- УСЛОВИЕ 3: Игрок в угле обзора? (visionAngle / 2 в каждую сторону от forward) ---
        float angle = Vector3.Angle(transform.forward, directionToPlayer.normalized);
        if (angle > visionAngle * 0.5f)
        {
            _detectionTimer = 0f;
            _playerDetected = false;
            return;
        }

        // --- УСЛОВИЕ 4: Нет препятствий между соседом и игроком? ---
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(rayOrigin, directionToPlayer.normalized, distance, visionObstacleMask))
        {
            _detectionTimer = 0f;
            _playerDetected = false;
            return;
        }

        // Все условия выполнены — накапливаем таймер
        _playerDetected = true;
        _detectionTimer += Time.deltaTime;

        if (_detectionTimer >= detectionDelay)
        {
            GameOver();
        }
    }

    /// <summary>
    /// Показывает меню проигрыша (игрок попался соседу).
    /// </summary>
    private void GameOver()
    {
        if (_isGameOver) return;
        _isGameOver = true;

        Debug.Log("ИГРА ОКОНЧЕНА! Игрок замечен соседом.");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (gameOverMenu != null)
        {
            gameOverMenu.SetActive(true);
        }

        if (playerController != null)
        {
            playerController.enabled = false;
        }

        if (timerController != null)
        {
            timerController.Pause();
        }

        this.enabled = false;
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnTriggerEnter(Collider other)
    {
        string tag = other.tag;
        if (tag == "Kitchen" || tag == "Hallway" || tag == "Livingroom" || tag == "Bathroom")
        {
            _currentRoomTag = tag;
        }

        if (tag == "Player")
        {
            // Проверяем реальную дистанцию, чтобы GameOver не срабатывал
            // просто от нахождения в одной комнате (триггер комнаты большой)
            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance < 1.5f)
            {
                GameOver();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == _currentRoomTag)
        {
            _currentRoomTag = null;
            _detectionTimer = 0f;
            _playerDetected = false;
        }
    }

    private IEnumerator WaitAndProceed()
    {
        _isWaiting = true;

        var currentPoint = waypoints[_currentWaypointIndex];
        
        // Ждем указанное время
        if (currentPoint.waitTime > 0)
        {
            yield return new WaitForSeconds(currentPoint.waitTime);
        }

        // Переходим к следующей точке
        _currentWaypointIndex++;
        if (_currentWaypointIndex >= waypoints.Count)
        {
            _currentWaypointIndex = 0; // Зацикливаем маршрут
        }

        GoToNextWaypoint();
        _isWaiting = false;
    }

    private void GoToNextWaypoint()
    {
        if (waypoints[_currentWaypointIndex].point != null)
        {
            _agent.SetDestination(waypoints[_currentWaypointIndex].point.position);
        }
        else
        {
            Debug.LogError($"У соседа пропущена (null) точка под индексом {_currentWaypointIndex}!");
            // Попробуем пропустить пустую точку, чтобы он не застрял намертво
            StartCoroutine(WaitAndProceed());
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Визуализация угла обзора
        Gizmos.color = _playerDetected ? Color.red : Color.yellow;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        float halfAngleRad = visionAngle * 0.5f * Mathf.Deg2Rad;

        Vector3 forward = transform.forward * visionRange;
        Vector3 right = transform.right * Mathf.Sin(halfAngleRad) * visionRange;

        Vector3 leftBoundary = transform.forward * Mathf.Cos(halfAngleRad) * visionRange - right;
        Vector3 rightBoundary = transform.forward * Mathf.Cos(halfAngleRad) * visionRange + right;

        Gizmos.DrawLine(origin, origin + leftBoundary);
        Gizmos.DrawLine(origin, origin + rightBoundary);
        Gizmos.DrawLine(origin + leftBoundary, origin + rightBoundary);

        // Дуга обзора
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.3f);
        UnityEditor.Handles.DrawWireArc(origin, Vector3.up, leftBoundary.normalized,
                                        visionAngle, visionRange);
    }
#endif
}