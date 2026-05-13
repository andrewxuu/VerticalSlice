using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [System.Serializable]
    public struct InventorySlot
    {
        public ItemData item;
        public int      count;
    }

    [Header("Item Registry")]
    public ItemData[] allItems;

    [Header("Equipment")]
    [Tooltip("Items whose name contains this string can be equipped in the axe slot.")]
    public string axeTag = "Axe";

    private static readonly KeyCode[] numberKeys =
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
        KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8
    };

    private InventorySlot[] slots;
    private ItemData        equippedAxe;
    private int             selectedSlotIndex = -1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            slots = new InventorySlot[UIManager.SLOT_COUNT];
        }
        else Destroy(gameObject);
    }

    void Update()
    {
        // Block number key selection when a UI panel is fully blocking input
        if (GameState.IsUIOpen()) return;

        for (int i = 0; i < numberKeys.Length && i < slots.Length; i++)
        {
            if (Input.GetKeyDown(numberKeys[i]))
            {
                // Pressing the same key again deselects
                SetSelectedSlot(selectedSlotIndex == i ? -1 : i);
                break;
            }
        }
    }

    // ── Add item ──────────────────────────────────────────────────────────────
    public int AddItem(ItemData item, int amount = 1)
    {
        if (item == null) { Debug.LogWarning("[Inventory] AddItem called with null."); return 0; }

        int remaining = amount;

        for (int i = 0; i < slots.Length && remaining > 0; i++)
        {
            if (slots[i].item != item) continue;
            int canAdd = Mathf.Min(remaining, item.maxStackSize - slots[i].count);
            if (canAdd <= 0) continue;
            slots[i].count += canAdd;
            remaining      -= canAdd;
        }

        for (int i = 0; i < slots.Length && remaining > 0; i++)
        {
            if (slots[i].item != null) continue;
            int canAdd = Mathf.Min(remaining, item.maxStackSize);
            slots[i].item  = item;
            slots[i].count = canAdd;
            remaining     -= canAdd;
        }

        int added = amount - remaining;
        if (added > 0) Debug.Log($"[Inventory] +{added} {item.itemName}");
        else           Debug.Log($"[Inventory] No room for {item.itemName}.");

        RefreshUI();
        return added;
    }

    // ── Remove item ───────────────────────────────────────────────────────────
    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null) return false;
        if (GetItemCount(item) < amount) return false;

        int remaining = amount;
        for (int i = slots.Length - 1; i >= 0 && remaining > 0; i--)
        {
            if (slots[i].item != item) continue;
            int take = Mathf.Min(remaining, slots[i].count);
            slots[i].count -= take;
            remaining      -= take;
            if (slots[i].count <= 0)
            {
                slots[i].item  = null;
                slots[i].count = 0;
            }
        }

        if (selectedSlotIndex >= 0 && slots[selectedSlotIndex].item == null)
        {
            selectedSlotIndex = -1;
            ItemHolder.Instance?.ClearHeldItem();
        }

        Debug.Log($"[Inventory] -{amount} {item.itemName}");
        RefreshUI();
        return true;
    }

    // ── Remove from specific slot (for placement) ─────────────────────────────
    public bool RemoveFromSlot(int index, int amount = 1)
    {
        if (index < 0 || index >= slots.Length) return false;
        if (slots[index].item == null || slots[index].count < amount) return false;

        slots[index].count -= amount;
        if (slots[index].count <= 0)
        {
            slots[index].item  = null;
            slots[index].count = 0;
        }

        if (selectedSlotIndex == index && slots[index].item == null)
        {
            selectedSlotIndex = -1;
            ItemHolder.Instance?.ClearHeldItem();
        }

        RefreshUI();
        return true;
    }

    // ── Count / Has ───────────────────────────────────────────────────────────
    public int GetItemCount(ItemData item)
    {
        if (item == null) return 0;
        int total = 0;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].item == item) total += slots[i].count;
        return total;
    }

    public bool HasItem(ItemData item, int amount = 1) => GetItemCount(item) >= amount;

    // ── Slot access ───────────────────────────────────────────────────────────
    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return default;
        return slots[index];
    }

    public void SwapSlots(int a, int b)
    {
        if (a < 0 || a >= slots.Length || b < 0 || b >= slots.Length) return;
        (slots[a], slots[b]) = (slots[b], slots[a]);

        if (selectedSlotIndex == a || selectedSlotIndex == b)
            ItemHolder.Instance?.UpdateHeldItem(GetSelectedItem());

        RefreshUI();
    }

    // ── Selection ─────────────────────────────────────────────────────────────
    public int GetSelectedSlotIndex() => selectedSlotIndex;

    public ItemData GetSelectedItem()
    {
        if (selectedSlotIndex < 0 || selectedSlotIndex >= slots.Length) return null;
        return slots[selectedSlotIndex].item;
    }

    public void SetSelectedSlot(int index)
    {
        selectedSlotIndex = index;
        UIManager.Instance?.RefreshSelectedSlot(selectedSlotIndex);
        ItemHolder.Instance?.UpdateHeldItem(GetSelectedItem());
    }

    // ── Equipment ─────────────────────────────────────────────────────────────
    public ItemData GetEquippedAxe() => equippedAxe;
    public bool HasAxeEquipped()     => equippedAxe != null;

    public void EquipFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) return;
        ItemData item = slots[slotIndex].item;
        if (item == null) return;

        if (!item.itemName.ToLower().Contains(axeTag.ToLower()))
        {
            Debug.Log($"[Inventory] {item.itemName} cannot be equipped as an axe.");
            return;
        }

        if (equippedAxe != null)
        {
            ItemData old = equippedAxe;
            equippedAxe = item;
            slots[slotIndex].item  = old;
            slots[slotIndex].count = 1;
        }
        else
        {
            equippedAxe = item;
            slots[slotIndex].count--;
            if (slots[slotIndex].count <= 0)
            {
                slots[slotIndex].item  = null;
                slots[slotIndex].count = 0;
            }
        }

        Debug.Log($"[Inventory] Equipped {item.itemName}");
        RefreshUI();
    }

    public void UnequipAxe()
    {
        if (equippedAxe == null) return;
        if (AddItem(equippedAxe, 1) > 0)
        {
            Debug.Log($"[Inventory] Unequipped {equippedAxe.itemName}");
            equippedAxe = null;
        }
        RefreshUI();
    }

    // ── UI ────────────────────────────────────────────────────────────────────
    public void RefreshUI()
    {
        if (UIManager.Instance == null) return;
        UIManager.Instance.RefreshInventorySlots(slots);
        UIManager.Instance.RefreshEquipment(equippedAxe);
        UIManager.Instance.RefreshSelectedSlot(selectedSlotIndex);
    }
}
