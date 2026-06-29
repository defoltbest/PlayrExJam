using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Простой инвентарь игрока — хранит имена собранных предметов.
/// Добавить на объект Player.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private List<string> items = new();

    public void AddItem(string itemName)
    {
        items.Add(itemName);
        Debug.Log($"Picked up: {itemName} | Inventory count: {items.Count}");
    }

    public bool HasItem(string itemName) => items.Contains(itemName);
    public IReadOnlyList<string> Items => items;
}