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

    [Header("Trigger")]
    [SerializeField] string[] triggerTags = { "Player", "Neighbor" };
    [SerializeField] Collider triggerZone; // ссылка на НЕвращающийся дочерний коллайдер

    Quaternion closedRotation;
    float angle;
    bool playerInside;

    void Awake()
    {
        closedRotation = transform.localRotation;
        angle = isOpen ? openAngle : 0f;
        ApplyAngle();

        if (triggerZone == null)
        {
            // Автопоиск дочернего объекта с Collider.isTrigger
            foreach (var c in GetComponentsInChildren<Collider>())
            {
                if (c.isTrigger && c.transform != transform)
                {
                    triggerZone = c;
                    break;
                }
            }
        }

        if (triggerZone == null)
        {
            Debug.LogError(
                $"Door '{name}': укажите в инспекторе triggerZone — невращающийся дочерний коллайдер с isTrigger=true.\n" +
                $"Создайте пустой дочерний объект (напр. 'TriggerZone'), добавьте BoxCollider (isTrigger), " +
                $"разместите его в районе петли. Он НЕ должен вращаться вместе с дверью.",
                this);
        }
        else if (!triggerZone.isTrigger)
        {
            Debug.LogWarning($"Door '{name}': triggerZone должен быть isTrigger=true.", this);
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
        transform.localRotation = closedRotation * Quaternion.Euler(0f, angle, 0f);

        // Компенсируем вращение родителя для triggerZone — он всегда смотрит в исходном направлении
        if (triggerZone != null)
            triggerZone.transform.localRotation = Quaternion.Inverse(Quaternion.Euler(0f, angle, 0f));
    }

    public void Toggle() => isOpen = !isOpen;

    public void SetOpen(bool value) => isOpen = value;

    public bool IsOpen => isOpen;
}