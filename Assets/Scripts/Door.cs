using UnityEngine;

/// <summary>
/// Дверь на вертикальной петле. Компонент висит на корневом объекте двери,
/// начало координат которого находится у края проёма (петля), а полотно — дочерний
/// объект со смещением. Door меняет ТОЛЬКО поворот (позицию не трогает вовсе),
/// поэтому дверь не может «провалиться» или «крутиться по центру»:
/// она всегда распахивается вокруг своей петли. Перемещайте корневой объект — петля едет с ним.
/// </summary>
public class Door : MonoBehaviour
{
    [SerializeField] float openAngle = 95f;
    [SerializeField] float openSpeed = 320f;
    [SerializeField] bool isOpen;

    [Header("Auto‑open")]
    [SerializeField] string playerTag = "Player";

    Quaternion closedRotation;
    float angle;

    void Awake()
    {
        closedRotation = transform.localRotation;
        angle = isOpen ? openAngle : 0f;
        ApplyAngle();
    }

    void Start()
    {
        var col = GetComponent<Collider>();
        if (col == null || !col.isTrigger)
            Debug.LogWarning($"Door on {name} needs a Trigger Collider to auto‑open.", this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            SetOpen(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
            SetOpen(false);
    }

    public void Toggle() => isOpen = !isOpen;

    public void SetOpen(bool value) => isOpen = value;

    public bool IsOpen => isOpen;

    void Update()
    {
        var target = isOpen ? openAngle : 0f;
        if (Mathf.Approximately(angle, target))
            return;

        angle = Mathf.MoveTowards(angle, target, openSpeed * Time.deltaTime);
        ApplyAngle();
    }

    void ApplyAngle()
    {
        transform.localRotation = closedRotation * Quaternion.Euler(0f, angle, 0f);
    }
}
