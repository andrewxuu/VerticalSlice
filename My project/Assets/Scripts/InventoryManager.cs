using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("UI Slots")]
    public InventorySlotUI[] slots;

    [Header("Item Registry")]
    public ItemData[] allItems;

    private Dictionary<ItemData, int> inventory = new Dictionary<ItemData, int>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public int AddItem(ItemData item, int amount = 1)
    {
        if (item == null) { Debug.LogWarning("[Inventory] AddItem called with null ItemData."); return 0; }

        int current = GetItemCount(item);
        int canAdd  = Mathf.Clamp(amount, 0, item.maxStackSize - current);

        if (canAdd <= 0)
        {
            Debug.Log($"[Inventory] {item.itemName} is full (max {item.maxStackSize}).");
            return 0;
        }

        inventory[item] = current + canAdd;
        Debug.Log($"[Inventory] +{canAdd} {item.itemName}  (Total: {inventory[item]}/{item.maxStackSize})");

        RefreshUI();
        return canAdd;
    }

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null) return false;

        int current = GetItemCount(item);
        if (current < amount)
        {
            Debug.Log($"[Inventory] Not enough {item.itemName} to remove {amount} (have {current}).");
            return false;
        }

        inventory[item] = current - amount;
        if (inventory[item] == 0)
            inventory.Remove(item);

        Debug.Log($"[Inventory] -{amount} {item.itemName}");

        RefreshUI();
        return true;
    }

    public int GetItemCount(ItemData item)
    {
        return (item != null && inventory.ContainsKey(item)) ? inventory[item] : 0;
    }

    public bool HasItem(ItemData item, int amount = 1)
    {
        return GetItemCount(item) >= amount;
    }

    void RefreshUI()
    {
        if (slots == null) return;

        foreach (var slot in slots)
            if (slot != null) slot.Clear();

        int i = 0;
        foreach (var kvp in inventory)
        {
            if (i >= slots.Length) break;
            if (slots[i] != null) slots[i].SetItem(kvp.Key, kvp.Value);
            i++;
        }
    }
}