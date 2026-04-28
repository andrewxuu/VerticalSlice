using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI inventoryDisplayText;

    private Dictionary<string, int> inventory = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void AddItem(string itemName, int amount = 1)
    {
        if (inventory.ContainsKey(itemName))
            inventory[itemName] += amount;
        else
            inventory[itemName] = amount;

        Debug.Log($"[Inventory] +{amount} {itemName}  (Total: {inventory[itemName]})");
        RefreshUI();
    }

    public int GetItemCount(string itemName)
    {
        return inventory.ContainsKey(itemName) ? inventory[itemName] : 0;
    }

    void RefreshUI()
    {
        if (inventoryDisplayText == null) return;
        var sb = new System.Text.StringBuilder();
        foreach (var kvp in inventory)
            sb.AppendLine($"{kvp.Key}  x{kvp.Value}");
        inventoryDisplayText.text = sb.ToString();
    }
}
