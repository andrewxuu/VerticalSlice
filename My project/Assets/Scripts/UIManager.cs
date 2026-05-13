using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public const int SLOT_COUNT = 8;

    [Header("Recipes")]
    public RecipeData[] allRecipes;

    private UIDocument    document;
    private VisualElement root;

    // HUD
    private VisualElement   sunIcon, moonIcon;
    private VisualElement   chopBarContainer;
    private ProgressBar     chopBar;
    private VisualElement[] hbSlots;
    private VisualElement[] hbIcons;
    private Label[]         hbCounts;

    // Inventory panel
    private VisualElement   inventoryPanel;
    private VisualElement[] invSlots;
    private VisualElement[] invIcons;
    private Label[]         invCounts;
    private Label[]         invNames;
    private VisualElement   dragGhost;

    // Equipment
    private VisualElement equipAxeSlot;
    private VisualElement equipAxeIcon;
    private Label         equipAxeLabel;

    // Crafting
    private ScrollView invRecipeList;

    // Drag state
    private bool isDragging;
    private int  dragFromIndex = -1;

    // Campfire panel
    private VisualElement   craftingPanel;
    private ProgressBar     fuelBar;
    private Label           fuelText, shelterText;
    private VisualElement[] pips;
    private Button          addFuelBtn, upgradeBtn;
    private Label           fuelCostText, upgradeCostText;
    private Label           woodCountLabel, stoneCountLabel;
    private ScrollView      campfireRecipeList;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        document = GetComponent<UIDocument>();
        root     = document.rootVisualElement;

        QueryHUD();
        QueryInventoryPanel();
        QueryCraftingPanel();
        SetupInventoryInteraction();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (IsInventoryOpen()) CloseInventory();
            else if (!IsCraftingOpen()) OpenInventory();
        }
    }

    // ── GameState (folded in from GameState.cs) ───────────────────────────────
    // Any script that previously called GameState.IsUIOpen() now calls UIManager.IsUIOpen()
    public static bool IsUIOpen()
    {
        // Check VS scene variable (used by the movement graph)
        try
        {
            if ((bool)Variables.Scene(SceneManager.GetActiveScene()).Get("isOpen"))
                return true;
        }
        catch { }

        if (Instance != null)
        {
            if (Instance.IsInventoryOpen()) return true;
            if (Instance.IsCraftingOpen())  return true;
        }

        return false;
    }

    // ── Queries ───────────────────────────────────────────────────────────────
    void QueryHUD()
    {
        sunIcon          = root.Q("sun-icon");
        moonIcon         = root.Q("moon-icon");
        chopBarContainer = root.Q("chop-bar-container");
        chopBar          = root.Q<ProgressBar>("chop-bar");

        hbSlots  = new VisualElement[SLOT_COUNT];
        hbIcons  = new VisualElement[SLOT_COUNT];
        hbCounts = new Label[SLOT_COUNT];
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            hbSlots[i]  = root.Q($"hb-{i}");
            hbIcons[i]  = root.Q($"hb-icon-{i}");
            hbCounts[i] = root.Q<Label>($"hb-count-{i}");
        }
    }

    void QueryInventoryPanel()
    {
        inventoryPanel = root.Q("inventory-panel");
        dragGhost      = root.Q("drag-ghost");

        invSlots  = new VisualElement[SLOT_COUNT];
        invIcons  = new VisualElement[SLOT_COUNT];
        invCounts = new Label[SLOT_COUNT];
        invNames  = new Label[SLOT_COUNT];
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            invSlots[i]  = root.Q($"inv-slot-{i}");
            invIcons[i]  = root.Q($"inv-icon-{i}");
            invCounts[i] = root.Q<Label>($"inv-count-{i}");
            invNames[i]  = root.Q<Label>($"inv-name-{i}");
        }

        equipAxeSlot  = root.Q("equip-axe");
        equipAxeIcon  = root.Q("equip-axe-icon");
        equipAxeLabel = root.Q<Label>("equip-axe-label");
        invRecipeList = root.Q<ScrollView>("recipe-list");
    }

    void QueryCraftingPanel()
    {
        craftingPanel   = root.Q("crafting-panel");
        fuelBar         = root.Q<ProgressBar>("fuel-bar");
        fuelText        = root.Q<Label>("fuel-text");
        shelterText     = root.Q<Label>("shelter-text");
        addFuelBtn      = root.Q<Button>("add-fuel-btn");
        upgradeBtn      = root.Q<Button>("upgrade-btn");
        fuelCostText    = root.Q<Label>("fuel-cost-text");
        upgradeCostText = root.Q<Label>("upgrade-cost-text");
        woodCountLabel  = root.Q<Label>("wood-count");
        stoneCountLabel = root.Q<Label>("stone-count");

        pips = new VisualElement[3];
        for (int i = 0; i < 3; i++)
            pips[i] = root.Q($"pip-{i}");

        campfireRecipeList = root.Q<ScrollView>("campfire-recipe-list");
    }

    // ── Inventory open / close ────────────────────────────────────────────────
    public bool IsInventoryOpen()
    {
        return inventoryPanel != null && !inventoryPanel.ClassListContains("hidden");
    }

    public void OpenInventory()
    {
        inventoryPanel.RemoveFromClassList("hidden");
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible   = true;
        SetVSOpen(true);
        RefreshRecipeList(invRecipeList, false);
        InventoryManager.Instance?.RefreshUI();
    }

    public void CloseInventory()
    {
        inventoryPanel.AddToClassList("hidden");
        CancelDrag();
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible   = false;
        SetVSOpen(false);
    }

    void SetVSOpen(bool open)
    {
        try { Variables.Scene(SceneManager.GetActiveScene()).Set("isOpen", open); }
        catch { }
    }

    // ── Inventory interaction ─────────────────────────────────────────────────
    void SetupInventoryInteraction()
    {
        if (invSlots == null) return;

        for (int i = 0; i < SLOT_COUNT; i++)
        {
            if (invSlots[i] == null) continue;
            int index = i;

            invSlots[i].RegisterCallback<PointerDownEvent>(evt =>
            {
                if (InventoryManager.Instance == null) return;
                var slot = InventoryManager.Instance.GetSlot(index);
                if (slot.item == null) return;

                InventoryManager.Instance.SetSelectedSlot(index);

                isDragging    = true;
                dragFromIndex = index;

                if (dragGhost != null)
                {
                    dragGhost.style.backgroundImage = new StyleBackground(slot.item.icon);
                    dragGhost.RemoveFromClassList("hidden");
                    MoveDragGhost(evt.position);
                }

                evt.StopPropagation();
            });

            invSlots[i].RegisterCallback<PointerEnterEvent>(evt =>
            {
                if (!isDragging || index == dragFromIndex) return;
                invSlots[index].AddToClassList("inv-slot-drag-over");
            });

            invSlots[i].RegisterCallback<PointerLeaveEvent>(evt =>
            {
                invSlots[index].RemoveFromClassList("inv-slot-drag-over");
            });
        }

        if (inventoryPanel != null)
        {
            inventoryPanel.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!isDragging) return;
                MoveDragGhost(evt.position);
            });

            inventoryPanel.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!isDragging) return;

                if (equipAxeSlot != null && equipAxeSlot.worldBound.Contains(evt.position))
                {
                    InventoryManager.Instance?.EquipFromSlot(dragFromIndex);
                    CancelDrag();
                    return;
                }

                int dropIndex = GetSlotUnderPointer(evt.position);
                if (dropIndex >= 0 && dropIndex != dragFromIndex)
                    InventoryManager.Instance?.SwapSlots(dragFromIndex, dropIndex);

                CancelDrag();
            });
        }

        equipAxeSlot?.RegisterCallback<PointerDownEvent>(evt =>
        {
            InventoryManager.Instance?.UnequipAxe();
            evt.StopPropagation();
        });
    }

    void MoveDragGhost(Vector2 position)
    {
        if (dragGhost == null) return;
        dragGhost.style.left = position.x - 24;
        dragGhost.style.top  = position.y - 24;
    }

    int GetSlotUnderPointer(Vector2 position)
    {
        for (int i = 0; i < SLOT_COUNT; i++)
            if (invSlots[i] != null && invSlots[i].worldBound.Contains(position))
                return i;
        return -1;
    }

    void CancelDrag()
    {
        isDragging    = false;
        dragFromIndex = -1;
        dragGhost?.AddToClassList("hidden");
        for (int i = 0; i < SLOT_COUNT; i++)
            invSlots[i]?.RemoveFromClassList("inv-slot-drag-over");
    }

    // ── Selected slot highlight ───────────────────────────────────────────────
    public void RefreshSelectedSlot(int selectedIndex)
    {
        for (int i = 0; i < SLOT_COUNT; i++)
            invSlots[i]?.EnableInClassList("inv-slot-selected", i == selectedIndex);
    }

    // ── Refresh inventory slots ───────────────────────────────────────────────
    public void RefreshInventorySlots(InventoryManager.InventorySlot[] slots)
    {
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            bool hasItem = i < slots.Length && slots[i].item != null;

            if (hbIcons[i] != null)
            {
                hbIcons[i].style.backgroundImage = hasItem ? new StyleBackground(slots[i].item.icon) : StyleKeyword.None;
                hbIcons[i].style.display = hasItem ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (hbCounts[i] != null)
                hbCounts[i].text = hasItem && slots[i].count > 1 ? slots[i].count.ToString() : "";

            if (invIcons[i] != null)
            {
                invIcons[i].style.backgroundImage = hasItem ? new StyleBackground(slots[i].item.icon) : StyleKeyword.None;
                invIcons[i].style.display = hasItem ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (invCounts[i] != null)
                invCounts[i].text = hasItem && slots[i].count > 1 ? slots[i].count.ToString() : "";
            if (invNames[i] != null)
                invNames[i].text = hasItem ? slots[i].item.itemName : "";
            if (invSlots[i] != null)
                invSlots[i].EnableInClassList("inv-slot-empty", !hasItem);
        }

        if (IsInventoryOpen())
            RefreshRecipeList(invRecipeList, false);
    }

    // ── Equipment ─────────────────────────────────────────────────────────────
    public void RefreshEquipment(ItemData equipped)
    {
        if (equipAxeIcon == null) return;
        if (equipped != null)
        {
            equipAxeIcon.style.backgroundImage = new StyleBackground(equipped.icon);
            equipAxeIcon.style.display = DisplayStyle.Flex;
            if (equipAxeLabel != null) equipAxeLabel.text = equipped.itemName;
            equipAxeSlot?.AddToClassList("equip-slot-filled");
        }
        else
        {
            equipAxeIcon.style.backgroundImage = StyleKeyword.None;
            equipAxeIcon.style.display = DisplayStyle.None;
            if (equipAxeLabel != null) equipAxeLabel.text = "Axe";
            equipAxeSlot?.RemoveFromClassList("equip-slot-filled");
        }
    }

    // ── Recipe system ─────────────────────────────────────────────────────────
    public void RefreshRecipeList(ScrollView list, bool campfireOnly)
    {
        if (list == null || allRecipes == null) return;
        list.contentContainer.Clear();

        foreach (RecipeData recipe in allRecipes)
        {
            if (recipe == null) continue;
            if (campfireOnly  && !recipe.requiresCampfire) continue;
            if (!campfireOnly &&  recipe.requiresCampfire) continue;

            Button btn = new Button();
            btn.AddToClassList("recipe-btn");

            Label title = new Label(recipe.recipeName);
            title.AddToClassList("recipe-title");
            btn.Add(title);

            string costStr  = "";
            bool   canAfford = true;
            foreach (var ing in recipe.ingredients)
            {
                int have = InventoryManager.Instance.GetItemCount(ing.item);
                if (costStr.Length > 0) costStr += "  +  ";
                costStr += $"{ing.amount}x {ing.item.itemName} ({have})";
                if (have < ing.amount) canAfford = false;
            }

            Label cost = new Label(costStr);
            cost.AddToClassList("recipe-cost");
            btn.Add(cost);
            btn.SetEnabled(canAfford);

            RecipeData r = recipe;
            btn.clicked += () => CraftRecipe(r);
            list.contentContainer.Add(btn);
        }
    }

    void CraftRecipe(RecipeData recipe)
    {
        if (InventoryManager.Instance == null || recipe == null) return;
        foreach (var ing in recipe.ingredients)
            if (!InventoryManager.Instance.HasItem(ing.item, ing.amount)) return;
        foreach (var ing in recipe.ingredients)
            InventoryManager.Instance.RemoveItem(ing.item, ing.amount);
        InventoryManager.Instance.AddItem(recipe.result, recipe.resultCount);

        if (IsInventoryOpen()) RefreshRecipeList(invRecipeList,      false);
        if (IsCraftingOpen())  RefreshRecipeList(campfireRecipeList, true);
    }

    // ── HUD ───────────────────────────────────────────────────────────────────
    public void SetChopProgress(float n)  { if (chopBar          != null) chopBar.value = n; }
    public void SetChopBarVisible(bool v) { if (chopBarContainer  != null) chopBarContainer.EnableInClassList("hidden", !v); }

    public void SetSunIcon(Sprite s, Color c)
    {
        if (sunIcon == null) return;
        sunIcon.style.backgroundImage = new StyleBackground(s);
        sunIcon.style.unityBackgroundImageTintColor = c;
    }

    public void SetMoonIcon(Sprite s, Color c)
    {
        if (moonIcon == null) return;
        moonIcon.style.backgroundImage = new StyleBackground(s);
        moonIcon.style.unityBackgroundImageTintColor = c;
    }

    // ── Campfire panel ────────────────────────────────────────────────────────
    public bool IsCraftingOpen()
    {
        return craftingPanel != null && !craftingPanel.ClassListContains("hidden");
    }

    public void ShowCraftingPanel()
    {
        craftingPanel.RemoveFromClassList("hidden");
        RefreshRecipeList(campfireRecipeList, true);
    }

    public void HideCraftingPanel() { craftingPanel.AddToClassList("hidden"); }

    public void SetFuel(float current, float max)
    {
        if (fuelBar  != null) fuelBar.value = current / max;
        if (fuelText != null) fuelText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    public void SetShelterLevel(int level, int maxLevel)
    {
        if (shelterText != null) shelterText.text = $"Shelter Level: {level} / {maxLevel}";
        if (pips != null)
            for (int i = 0; i < pips.Length; i++)
                if (pips[i] != null) pips[i].EnableInClassList("pip-active", i < level);
    }

    public void SetFuelCost(string t)    { if (fuelCostText    != null) fuelCostText.text    = t; }
    public void SetUpgradeCost(string t) { if (upgradeCostText != null) upgradeCostText.text = t; }
    public void SetWoodCount(string t)   { if (woodCountLabel  != null) woodCountLabel.text  = t; }
    public void SetStoneCount(string t)  { if (stoneCountLabel != null) stoneCountLabel.text = t; }
    public void SetAddFuelEnabled(bool e) { addFuelBtn?.SetEnabled(e); }
    public void SetUpgradeEnabled(bool e) { upgradeBtn?.SetEnabled(e); }
    public void OnAddFuelClicked(System.Action cb) { if (addFuelBtn != null) addFuelBtn.clicked += cb; }
    public void OnUpgradeClicked(System.Action cb) { if (upgradeBtn != null) upgradeBtn.clicked += cb; }
}
