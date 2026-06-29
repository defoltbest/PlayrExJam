using UnityEngine;
using System;

/// <summary>
/// Вспомогательный компонент для проброса OnTriggerEnter/Exit через события.
/// Добавляется автоматически скриптом WardrobeInteraction на объект-триггер.
/// </summary>
public class TriggerForwarder : MonoBehaviour
{
    public event Action<Collider> OnTriggerEntered;
    public event Action<Collider> OnTriggerExited;

    private void OnTriggerEnter(Collider other) => OnTriggerEntered?.Invoke(other);
    private void OnTriggerExit(Collider other) => OnTriggerExited?.Invoke(other);

    /// <summary>
    /// Удаляет всех подписчиков, чтобы не было дублирования.
    /// </summary>
    public void ClearSubscribers()
    {
        OnTriggerEntered = null;
        OnTriggerExited = null;
    }
}