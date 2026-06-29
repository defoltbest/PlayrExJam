using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Инвентарь игрока — хранит имена собранных предметов и отображает их в UI.
/// Добавить на объект Player.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Data")]
    [SerializeField] private List<string> items = new();

    [Header("UI References")]
    [SerializeField] private Transform inventoryPanel;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Sprite[] itemSprites;

    public void AddItem(string itemName)
    {
        items.Add(itemName);
        Debug.Log($"[PlayerInventory] Picked up: '{itemName}' | Inventory count: {items.Count}");

        // Создаём UI-элемент в панели инвентаря
        if (inventoryPanel == null || itemPrefab == null)
        {
            Debug.LogWarning("[PlayerInventory] inventoryPanel или itemPrefab не назначены — UI не обновлён.");
            return;
        }

        var itemInstance = Instantiate(itemPrefab, inventoryPanel);
        itemInstance.name = itemName;

        // Ищем Image в дочернем объекте ImageItem
        var image = itemInstance.GetComponentInChildren<Image>();
        if (image == null)
        {
            Debug.LogWarning("[PlayerInventory] Компонент Image не найден в дочерних объектах префаба.");
            return;
        }

        // Способ 1: ищем спрайт в itemSprites (по точному совпадению имени)
        Sprite matchedSprite = FindSpriteInArray(itemName);

        // Способ 2: загружаем из Resources/IconItem/ (если не нашли в массиве)
        if (matchedSprite == null)
            matchedSprite = Resources.Load<Sprite>($"IconItem/{itemName}");

        if (matchedSprite != null)
        {
            image.sprite = matchedSprite;
            Debug.Log($"[PlayerInventory] Спрайт '{matchedSprite.name}' назначен для '{itemName}'.");
        }
        else
        {
            Debug.LogWarning($"[PlayerInventory] Спрайт для '{itemName}' не найден ни в itemSprites, ни в Resources/IconItem/. Используется дефолтный спрайт.");
        }
    }

    private Sprite FindSpriteInArray(string itemName)
    {
        if (itemSprites == null || itemSprites.Length == 0)
        {
            Debug.LogWarning($"[PlayerInventory] Массив itemSprites пуст (null или length=0).");
            return null;
        }

        Debug.Log($"[PlayerInventory] Поиск '{itemName}' среди {itemSprites.Length} спрайтов:");
        foreach (var sprite in itemSprites)
        {
            if (sprite == null) continue;
            Debug.Log($"  - sprite.name='{sprite.name}' (совпадение: {sprite.name == itemName})");
            if (sprite.name == itemName)
                return sprite;
        }

        return null;
    }

    public bool HasItem(string itemName) => items.Contains(itemName);
    public IReadOnlyList<string> Items => items;
}
