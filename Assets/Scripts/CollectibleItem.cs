using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Предмет, который игрок может подобрать по клавише E.
/// В префабе должен быть дочерний объект с текстом «Нажмите E» — ссылку указать в tooltipText.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CollectibleItem : MonoBehaviour
{
    [Header("Item Info")]
    [SerializeField] private string itemName = "Item";

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipText;

    private bool _playerInRange;
    private PlayerInventory _playerInventory;

    void Awake()
    {
        if (string.IsNullOrWhiteSpace(itemName))
            itemName = name;

        // Триггер-коллайдер
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // Rigidbody обязателен для срабатывания OnTriggerEnter (хотя бы на одном из двух объектов)
        // Player уже имеет kinematic Rigidbody, но для надёжности добавим и сюда.
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        // Текст изначально скрыт + всегда поверх всех объектов
        if (tooltipText != null)
        {
            tooltipText.SetActive(false);

            var canvas = tooltipText.GetComponent<Canvas>();
            if (canvas == null)
                canvas = tooltipText.AddComponent<Canvas>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = 32767;
        }

        Debug.Log($"[CollectibleItem] Awake: '{itemName}' готов. Collider isTrigger={col.isTrigger}, Rigidbody exists={rb != null}");
    }

    void Update()
    {
        // Биллборд: текст всегда лицом к камере
        if (tooltipText != null && tooltipText.activeSelf)
        {
            var cam = Camera.main;
            if (cam != null)
                tooltipText.transform.forward = cam.transform.forward;
        }

        if (!_playerInRange)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            PickUp();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[CollectibleItem] OnTriggerEnter с '{other.name}' (tag: {other.tag})");

        if (!other.CompareTag("Player"))
            return;

        Debug.Log($"[CollectibleItem] Игрок вошёл в зону предмета '{itemName}'");

        _playerInRange = true;
        _playerInventory = other.GetComponent<PlayerInventory>();
        if (_playerInventory == null)
            _playerInventory = other.GetComponentInParent<PlayerInventory>();

        if (tooltipText != null)
            tooltipText.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"[CollectibleItem] OnTriggerExit с '{other.name}'");

        if (!other.CompareTag("Player"))
            return;

        Debug.Log($"[CollectibleItem] Игрок вышел из зоны предмета '{itemName}'");

        _playerInRange = false;
        _playerInventory = null;

        if (tooltipText != null)
            tooltipText.SetActive(false);
    }

    void PickUp()
    {
        Debug.Log($"[CollectibleItem] Подбор предмета '{itemName}'");

        if (_playerInventory != null)
            _playerInventory.AddItem(itemName);

        Destroy(gameObject);
    }
}