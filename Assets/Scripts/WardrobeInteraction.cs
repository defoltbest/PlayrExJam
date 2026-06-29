using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Взаимодействие со шкафом: в зоне триггера нажать F — спрятаться / выйти.
/// Вешается на объект Wardrobe.
/// </summary>
public class WardrobeInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider interactionZone;   // WardrobeCollaider (isTrigger = true)
    [SerializeField] private Transform hidePosition;      // точка внутри шкафа, куда телепортируется игрок

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

    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;

        if (interactionZone == null)
        {
            Debug.LogError("[WardrobeInteraction] interactionZone не назначен!", this);
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
        _player.position = hidePosition != null ? hidePosition.position : transform.position;

        _playerController.enabled = false;
        if (_playerRenderer != null) _playerRenderer.enabled = false;
        if (_playerCollider != null) _playerCollider.enabled = false;

        _isHidden = true;

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
        if (_playerCollider != null) _playerCollider.enabled = true;

        _isHidden = false;

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
}
