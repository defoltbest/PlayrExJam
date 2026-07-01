using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Универсальное взаимодействие: в зоне триггера нажать F — спрятаться / выйти.
/// Вешается на любой объект, за которым можно спрятаться (шкаф, коробка, стена и т.д.).
/// </summary>
public class HideInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider interactionZone;   // триггер-зона (isTrigger = true)
    [SerializeField] private Transform hidePosition;      // точка, куда телепортируется игрок

    [Header("UI")]
    [SerializeField] private GameObject tooltip;          // подсказка (опционально)
    [SerializeField] private TMP_Text tooltipText;        // текст внутри тултипа

    [Header("Tooltip Text")]
    [SerializeField] private string hidePrompt = "Нажмите F, чтобы спрятаться";
    [SerializeField] private string exitPrompt = "Нажмите F, чтобы выйти";

    private Transform _player;
    private PlayerController _playerController;
    private MeshRenderer _playerRenderer;
    private Collider _playerCollider;

    private Vector3 _returnPosition;
    private bool _isPlayerInZone;
    private bool _isHidden;

    /// <summary>
    /// Статический флаг: спрятан ли игрок прямо сейчас (в любом укрытии).
    /// Используется NeighborController для определения, был ли игрок замечен до укрытия.
    /// </summary>
    public static bool IsPlayerHidden { get; private set; }

    private Camera _mainCamera;
    private CameraFollow _cameraFollow;

    private void OnDestroy()
    {
        // Сбрасываем статический флаг при выгрузке сцены (рестарт игры),
        // иначе после SceneManager.LoadScene он останется true и сломает логику соседа.
        IsPlayerHidden = false;
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        if (_mainCamera != null)
            _cameraFollow = _mainCamera.GetComponent<CameraFollow>();

        if (interactionZone == null)
        {
            Debug.LogError("[HideInteraction] interactionZone не назначен!", this);
            return;
        }

        // Подписываемся на события триггера
        var trigger = interactionZone.gameObject.AddComponent<TriggerForwarder>();
        trigger.OnTriggerEntered += HandlePlayerEnter;
        trigger.OnTriggerExited += HandlePlayerExit;

        // Настройка тултипа: World Space Canvas для билборда (как в CollectibleBubble)
        if (tooltip != null)
        {
            tooltip.SetActive(false);

            var canvas = tooltip.GetComponent<Canvas>();
            if (canvas == null)
                canvas = tooltip.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = _mainCamera;
        }
    }

    private void Update()
    {
        if (!_isPlayerInZone) return;

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (_isHidden)
                Exit();
            else
                Hide();
        }
    }

    private void LateUpdate()
    {
        // Билборд: тултип всегда лицом к камере (правильный LookAt, как в CollectibleBubble)
        if (tooltip != null && tooltip.activeSelf)
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                return;
            }

            var lookPos = tooltip.transform.position + _mainCamera.transform.rotation * Vector3.forward;
            tooltip.transform.LookAt(lookPos, _mainCamera.transform.rotation * Vector3.up);
        }
    }

    private void Hide()
    {
        if (_player == null) return;

        _returnPosition = _player.position;

        // Отключаем ВСЕ коллайдеры и NavMeshAgent ПЕРЕД телепортацией,
        // чтобы физика не блокировала перемещение модели внутрь объекта.
        DisableAllPlayerColliders();
        var agent = _player.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        _player.position = hidePosition != null ? hidePosition.position : transform.position;

        _playerController.enabled = false;
        if (_playerRenderer != null) _playerRenderer.enabled = false;
        if (_cameraFollow != null) _cameraFollow.enabled = false;

        _isHidden = true;
        IsPlayerHidden = true;

        UpdateTooltipText();
        if (tooltip != null)
            tooltip.SetActive(true);
    }

    private void Exit()
    {
        if (_player == null) return;

        _player.position = _returnPosition;

        _playerController.enabled = true;
        if (_playerRenderer != null) _playerRenderer.enabled = true;
        if (_cameraFollow != null) _cameraFollow.enabled = true;

        // Возвращаем коллайдеры и NavMeshAgent ПОСЛЕ телепортации наружу.
        EnableAllPlayerColliders();
        var agent = _player.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = true;

        _isHidden = false;
        IsPlayerHidden = false;

        UpdateTooltipText();
        if (tooltip != null)
            tooltip.SetActive(true);
    }

    private void HandlePlayerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _player = other.transform;
        _playerController = _player.GetComponent<PlayerController>();
        _playerRenderer = _player.GetComponent<MeshRenderer>();
        _playerCollider = other; // тот же коллайдер, что вошёл в триггер

        _isPlayerInZone = true;

        UpdateTooltipText();
        if (tooltip != null)
            tooltip.SetActive(true);
    }

    private void HandlePlayerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _isPlayerInZone = false;

        if (tooltip != null)
            tooltip.SetActive(false);

        // Если игрок вышел из зоны пока был спрятан — автоматически выходим
        if (_isHidden)
            Exit();
    }

    private void UpdateTooltipText()
    {
        if (tooltipText != null)
        {
            tooltipText.text = _isHidden ? exitPrompt : hidePrompt;
        }
    }

    private void DisableAllPlayerColliders()
    {
        var colliders = _player.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
            col.enabled = false;
    }

    private void EnableAllPlayerColliders()
    {
        var colliders = _player.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
            col.enabled = true;
    }
}