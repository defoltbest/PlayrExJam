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

    [Tooltip("Время ожидания в этой точке, пока проигрывается анимация")]
    public float waitTime;

    [Tooltip("Trigger-параметр в Animator Controller, который включится в этой точке. Например: Drink, WatchTV, ScratchHead")]
    public string animationTrigger;
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
    private Animator _animator;
    private int _currentWaypointIndex = 0;
    private bool _isWaiting = false;
    private Coroutine _waitCoroutine;
private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
private static readonly int IsPursuingHash = Animator.StringToHash("IsPursuing");

    // --- Обнаружение игрока ---
    private string _currentRoomTag;
    private float _detectionTimer;
    private Transform _playerTransform;
    private bool _playerDetected;
    private bool _playerSeenBeforeHiding;
    private bool _isGameOver = false;

    // --- Преследование ---
    [Header("Преследование")]
    [Tooltip("Скорость движения при преследовании игрока")]
    [SerializeField] private float pursuitSpeed = 4f;
    [Tooltip("Скорость поворота при преследовании игрока")]
    [SerializeField] private float pursuitRotationSpeed = 720f;
    [Tooltip("Время ожидания на месте после потери игрока из вида, перед возвратом на маршрут")]
    [SerializeField] private float waitTimeAfterLosingPlayer = 2f;

    private bool _isPursuing = false;
    private bool _isWaitingAfterLosingPlayer = false;
    private float _patrolSpeed;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TimerController timerController;

    [Header("Предметы")]
    // Чтобы брать кружку в руку
    [SerializeField] private Transform cup;
    [SerializeField] private Transform cupHoldPoint;
    [SerializeField] private Transform cupTablePoint;


    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false; // Берем управление поворотом на себя

        _animator = GetComponentInChildren<Animator>();
        
        if (_animator != null)
{
    _animator.applyRootMotion = false;
}
        
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
        // Сохраняем патрульную скорость агента
        _patrolSpeed = _agent.speed;

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

        // --- Преследование / возврат к патрулированию ---
        HandlePursuit();

        // --- Управление анимацией: IsWalking = true когда агент движется ---
       UpdateAnimation();

        if (waypoints.Count == 0 || _isWaiting || _isPursuing)
            return;

        // --- Поворот в сторону движения (угол обзора = forward) ---
        RotateTowardsMovement();

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
            _playerSeenBeforeHiding = false; // игрок ушёл в другую комнату — сосед забывает
            return;
        }

        // Вычисляем направление и дистанцию до игрока (нужно и для укрытия, и для обычного обзора)
        Vector3 directionToPlayer = _playerTransform.position - transform.position;
        directionToPlayer.y = 0f;
        float distance = directionToPlayer.magnitude;
        float angle = Vector3.Angle(transform.forward, directionToPlayer.normalized);
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        // --- ЛОГИКА УКРЫТИЯ ---
        // Игрок спрятан — проверяем, находится ли он сейчас в угле обзора соседа
        if (HideInteraction.IsPlayerHidden)
        {
            // Если игрок вне радиуса или вне угла обзора — сосед теряет его из виду
            // (даже если видел до укрытия). Луч препятствий НЕ проверяем,
            // потому что укрытие по определению находится за препятствием (мебелью).
            bool currentlyInSight = distance <= visionRange && angle <= visionAngle * 0.5f;

            if (!currentlyInSight)
            {
                // Игрок ушёл из угла обзора — сосед забывает, что видел его
                _playerSeenBeforeHiding = false;
            }

            if (!_playerSeenBeforeHiding)
            {
                // Сосед не видел игрока до укрытия, или игрок ушёл из обзора → безопасно
                _detectionTimer = 0f;
                _playerDetected = false;
                return;
            }
            else
            {
                // Сосед видел игрока, и игрок всё ещё в угле обзора → продолжает отсчёт
                _playerDetected = true;
                _detectionTimer += Time.deltaTime;
                if (_detectionTimer >= detectionDelay)
                    GameOver();
                return;
            }
        }

        // --- ОБЫЧНОЕ ОБНАРУЖЕНИЕ (игрок НЕ в укрытии) ---

        // --- УСЛОВИЕ 2: Игрок в радиусе обзора? ---
        if (distance > visionRange)
        {
            _detectionTimer = 0f;
            _playerDetected = false;
            _playerSeenBeforeHiding = false; // игрок ушёл из зоны видимости — забываем
            return;
        }

        // --- УСЛОВИЕ 3: Игрок в угле обзора? (visionAngle / 2 в каждую сторону от forward) ---
        if (angle > visionAngle * 0.5f)
        {
            _detectionTimer = 0f;
            _playerDetected = false;
            _playerSeenBeforeHiding = false; // игрок ушёл из угла обзора — забываем
            return;
        }

        // --- УСЛОВИЕ 4: Нет препятствий между соседом и игроком? ---
        if (Physics.Raycast(rayOrigin, directionToPlayer.normalized, distance, visionObstacleMask))
        {
            _detectionTimer = 0f;
            _playerDetected = false;
            _playerSeenBeforeHiding = false; // игрок скрылся за препятствием — забываем
            return;
        }

        // Все условия выполнены — игрок видим
        _playerDetected = true;
        _detectionTimer += Time.deltaTime;

        if (_detectionTimer >= detectionDelay)
        {
            GameOver();
        }
    }

    /// <summary>
    /// Управляет преследованием: если игрок обнаружен — начинает преследовать его;
    /// если игрок вышел из зоны видимости — возвращается к патрулированию.
    /// </summary>
    private void HandlePursuit()
    {
        if (_playerDetected && !_isPursuing)
{
    PutCupBack(); //кружку вернули при погоне
    // Если сосед стоял на точке и проигрывал анимацию — прерываем ожидание
    if (_waitCoroutine != null)
{
    StopCoroutine(_waitCoroutine);
    _waitCoroutine = null;

    // Считаем, что действие в этой точке уже было прервано,
    // поэтому после погони сосед пойдёт к следующей точке, а не вернётся пить чай заново.
    AdvanceWaypointIndex();
}

    _isWaiting = false;
    _agent.isStopped = false;

    // --- НАЧАТЬ ПРЕСЛЕДОВАНИЕ ---
    _isPursuing = true;
    _playerSeenBeforeHiding = true;
    _agent.speed = pursuitSpeed;
    _agent.autoBraking = false;
}
        else if (!_playerDetected && _isPursuing && !_isWaitingAfterLosingPlayer)
        {
            // --- ИГРОК ПОТЕРЯН: СТОЯТЬ НА МЕСТЕ И ЖДАТЬ ---
            _isPursuing = false;
            _playerSeenBeforeHiding = false; // сосед потерял игрока — забывает, что видел
            _agent.isStopped = true;
            _agent.autoBraking = true;
            StartCoroutine(WaitBeforeReturnToPatrol());
        }

        if (_isPursuing)
        {
            // Постоянно обновляем destination на позицию игрока
            _agent.SetDestination(_playerTransform.position);

            // Плавно поворачиваемся лицом к игроку
            Vector3 dirToPlayer = (_playerTransform.position - transform.position);
            dirToPlayer.y = 0f;
            if (dirToPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    pursuitRotationSpeed * Time.deltaTime
                );
            }
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
        if (tag == "Kitchen" || tag == "Hallway" || tag == "Livingroom" || tag == "Bathroom" || tag == "Entrance") 
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
            _playerSeenBeforeHiding = false;
        }
    }

    private IEnumerator WaitAndProceed()
{
    _isWaiting = true;

    _agent.isStopped = true;
    _agent.ResetPath();

    if (_animator != null)
    {
        _animator.SetBool(IsWalkingHash, false);
    }

    NeighborWaypoint currentPoint = waypoints[_currentWaypointIndex];

    // Включаем анимацию для конкретной точки
    if (_animator != null && !string.IsNullOrWhiteSpace(currentPoint.animationTrigger))
    {
        //Пьем из кружки
        if (currentPoint.animationTrigger == "Drink")
        {
          TakeCup();
        }

        _animator.SetTrigger(currentPoint.animationTrigger);
    }

    // Пауза в точке
    if (currentPoint.waitTime > 0)
    {
        yield return new WaitForSeconds(currentPoint.waitTime);
    }

  //Возвращаем кружку на базу
    if (currentPoint.animationTrigger == "Drink")
    {
        PutCupBack();
    }

    _waitCoroutine = null;
    _isWaiting = false;

    // Если за время ожидания началось преследование, не возвращаемся к маршруту
    if (_isPursuing)
        yield break;

    _agent.isStopped = false;

    AdvanceWaypointIndex();

    GoToNextWaypoint();
}

    /// <summary>
    /// Поворачивает соседа в сторону движения (forward = направление взгляда = угол обзора).
    /// </summary>
    private void RotateTowardsMovement()
    {
        // steeringTarget — точка, к которой агент реально движется прямо сейчас
        Vector3 targetPoint = _agent.steeringTarget;
        Vector3 direction = (targetPoint - transform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                540f * Time.deltaTime // Быстрый, но плавный поворот
            );
        }
    }

    private IEnumerator WaitBeforeReturnToPatrol()
    {
        _isWaitingAfterLosingPlayer = true;

        if (waitTimeAfterLosingPlayer > 0)
        {
            yield return new WaitForSeconds(waitTimeAfterLosingPlayer);
        }

        _isWaitingAfterLosingPlayer = false;
        _agent.isStopped = false;
        _agent.speed = _patrolSpeed;
        GoToNextWaypoint();
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
            StartWaitingAtWaypoint();
        }
    }

    private void UpdateAnimation()
{
    if (_animator == null) return;

    bool isMoving =
        !_agent.isStopped &&
        !_isWaiting &&
        _agent.velocity.sqrMagnitude > 0.01f;

    _animator.SetBool(IsWalkingHash, isMoving);
    _animator.SetBool(IsPursuingHash, _isPursuing);
}

private void StartWaitingAtWaypoint()
{
    if (_waitCoroutine != null)
        return;

    _waitCoroutine = StartCoroutine(WaitAndProceed());
}

//Мы не идем в ту же точку, если погнались за игроком (на самом деле это не работает)
private void AdvanceWaypointIndex()
{
    _currentWaypointIndex++;

    if (_currentWaypointIndex >= waypoints.Count)
    {
        _currentWaypointIndex = 0;
    }
}

// Берем кружку
private void TakeCup()
{
    Debug.Log("TakeCup вызван");

    if (cup == null)
    {
        Debug.LogError("Cup не назначена в инспекторе!");
        return;
    }

    if (cupHoldPoint == null)
    {
        Debug.LogError("CupHoldPoint не назначен в инспекторе!");
        return;
    }

    Debug.Log("Берём кружку: " + cup.name + " -> " + cupHoldPoint.name);

    cup.SetParent(cupHoldPoint);

    cup.localPosition = Vector3.zero;
    cup.localRotation = Quaternion.identity;
}
// Возвращаем кружку
private void PutCupBack()
{
    if (cup == null || cupTablePoint == null)
        return;

    cup.SetParent(cupTablePoint);

    cup.localPosition = Vector3.zero;
    cup.localRotation = Quaternion.identity;
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