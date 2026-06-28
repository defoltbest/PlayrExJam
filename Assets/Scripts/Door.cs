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

    Quaternion closedRotation;
    float angle;

    void Awake()
    {
        closedRotation = transform.localRotation;
        angle = isOpen ? openAngle : 0f;
        ApplyAngle();
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
