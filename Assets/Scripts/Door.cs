using UnityEngine;

/// <summary>
/// Дверь на вертикальной петле. Триггер обнаружения игрока висит на отдельном
/// НЕвращающемся дочернем объекте (TriggerZone), поэтому зона детекции не
/// смещается при открытии — дверь не дёргается.
/// Компонент меняет ТОЛЬКО поворот корневого объекта. Позиция не трогается.
/// </summary>
public class Door : MonoBehaviour
{
    [SerializeField] float openAngle = 95f;
    [SerializeField] float openSpeed = 320f;
    [SerializeField] bool isOpen;
    [SerializeField] Vector3 rotationAxis = Vector3.up; // Ось петли в мировых координатах (по умолчанию Y = вверх)

    [Header("Trigger")]
    [SerializeField] string[] triggerTags = { "Player", "Neighbor" };
    [SerializeField] Collider triggerZone; // ссылка на НЕвращающийся дочерний коллайдер

    Quaternion closedWorldRotation;
    Quaternion triggerZoneOriginalWorldRotation;
    bool hasTriggerZone;
    float angle;
    bool playerInside;
    bool isEntranceLocked; // true, если дверь требует разблокировки (тег DoorEntrance)

    void Awake()
    {
        // Дверь с тегом DoorEntrance заблокирована до вызова Unlock()
        isEntranceLocked = CompareTag("DoorEntrance");

        closedWorldRotation = transform.rotation;
        angle = isOpen ? openAngle : 0f;

        if (triggerZone != null)
        {
            triggerZoneOriginalWorldRotation = triggerZone.transform.rotation;
            hasTriggerZone = true;
        }

        ApplyAngle();

        if (triggerZone == null)
        {
            // Автопоиск дочернего объекта с Collider (isTrigger не обязателен для заблокированных дверей)
            foreach (var c in GetComponentsInChildren<Collider>())
            {
                if (c.transform != transform)
                {
                    triggerZone = c;
                    break;
                }
            }
        }

        if (triggerZone == null)
        {
            Debug.LogError(
                $"Door '{name}': укажите в инспекторе triggerZone — невращающийся дочерний коллайдер.\n" +
                $"Создайте пустой дочерний объект (напр. 'TriggerZone'), добавьте BoxCollider, " +
                $"разместите его в районе петли. Он НЕ должен вращаться вместе с дверью.",
                this);
        }
        else
        {
            // Заблокированные двери (DoorEntrance): isTrigger = false — физический барьер
            // Обычные/разблокированные двери: isTrigger = true — свободный проход
            triggerZone.isTrigger = !isEntranceLocked;
        }
    }

    // События ловятся на triggerZone через родительский MonoBehaviour —
    // Unity вызывает их на объекте с Rigidbody или на любом компоненте родителя.
    // Если triggerZone — дочерний, события всплывают к Door (при наличии Rigidbody на корне).
    // Поэтому используем ручную проверку в Update через bounds.

    void Update()
    {
        if (triggerZone != null)
            playerInside = CheckPlayerInTrigger();

        // Дверь с тегом DoorEntrance не открывается автоматически, пока не разблокирована
        if (!isEntranceLocked)
            SetOpen(playerInside);

        var target = isOpen ? openAngle : 0f;
        if (Mathf.Approximately(angle, target))
            return;

        angle = Mathf.MoveTowards(angle, target, openSpeed * Time.deltaTime);
        ApplyAngle();
    }

    bool CheckPlayerInTrigger()
    {
        var bounds = triggerZone.bounds;
        // Простой поиск сущностей по тегам в пределах bounds триггера
        foreach (var tag in triggerTags)
        {
            var entities = GameObject.FindGameObjectsWithTag(tag);
            foreach (var entity in entities)
            {
                var entityCol = entity.GetComponent<Collider>();
                if (entityCol != null && bounds.Intersects(entityCol.bounds))
                    return true;
                if (bounds.Contains(entity.transform.position))
                    return true;
            }
        }
        return false;
    }

    void ApplyAngle()
    {
        // Вращаем дверь в МИРОВОМ пространстве вокруг заданной оси (по умолчанию Vector3.up)
        Quaternion hingeRotation = Quaternion.AngleAxis(angle, rotationAxis);
        transform.rotation = hingeRotation * closedWorldRotation;

        // Триггер-зона остаётся в исходном мировом положении (не вращается вместе с дверью)
        if (hasTriggerZone && triggerZone != null)
            triggerZone.transform.rotation = triggerZoneOriginalWorldRotation;
    }

    /// <summary>Разблокирует входную дверь (тег DoorEntrance) — включает isTrigger и дверь начинает открываться автоматически.</summary>
    public void Unlock()
    {
        isEntranceLocked = false;
        if (triggerZone != null)
            triggerZone.isTrigger = true;
    }

    public bool IsEntranceLocked => isEntranceLocked;

    public void Toggle() => isOpen = !isOpen;

    public void SetOpen(bool value) => isOpen = value;

    public bool IsOpen => isOpen;
}