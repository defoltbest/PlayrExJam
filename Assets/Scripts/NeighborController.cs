using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

    private NavMeshAgent _agent;
    private int _currentWaypointIndex = 0;
    private bool _isWaiting = false;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        
        // Тег Neighbor нужен, чтобы скрипт дверей открывал перед ним двери
        if (!CompareTag("Neighbor"))
        {
            Debug.LogWarning($"На соседе {gameObject.name} не установлен тег 'Neighbor'. Пожалуйста, создайте этот тег и назначьте его, иначе двери не будут открываться!");
        }
    }

    private void Start()
    {
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
}
