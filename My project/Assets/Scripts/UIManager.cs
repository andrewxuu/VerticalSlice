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

    // ── HUD ───────────────────────────────────────────────────────────────────
    private VisualElement   sunIcon, moonIcon;
    private VisualElement   chopBarContainer;
    private ProgressBar     chopBar;
    private VisualElement[] hbSlots;
    private VisualElement[] hbIcons;
    private Label[]         hbCounts;

    // Status bars (top-left)
    private ProgressBar warmthBar;
    private ProgressBar healthBar;

    // ── Tab inventory panel (equipment only, left side) ───────────────────────
    private VisualElement inventoryPanel;
    private VisualElement dragGhost;
    private VisualElement equipAxeSlot;
    private VisualElement equipAxeIcon;
    private Label         equipAxeLabel;

    // ── Shared right panel: backpack + crafting ───────────────────────────────
    private VisualElement   craftingPanel;
    private ScrollView      campfireRecipeList;
    private VisualElement[] cfSlots;
    private VisualElement[] cfIcons;
    private Label[]         cfCounts;

    private VisualElement[] invSlots;
    private VisualElement[] invIcons;
    private Label[]         invCounts;
    private Label[]         invNames;

    // ── Campfire status panel (left side) ─────────────────────────────────────
    private VisualElement   campfireStatusPanel;
    private ProgressBar     fuelBar;
    private Label           fuelText, shelterText;
    private VisualElement[] pips;
    private Button          addFuelBtn, upgradeBtn;
    private Label           fuelCostText, upgradeCostText;
    private Label           woodCountLabel, stoneCountLabel;

    // ── Death screen ──────────────────────────────────────────────────────────
    private VisualElement deathScreen;
    private Button        restartBtn;

    // Drag state
    private bool isDragging;
    private int  dragFromIndex = -1;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        document = GetComponent<UIDocument>();
        root     = document.rootVisualElement;

        QueryHUD();
        QueryInventoryPanel();
        QuerySharedCraftingPanel();
        QueryCampfireStatusPanel();
        QueryDeathScreen();
        SetupInventoryInteraction();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (IsInventoryOpen() && !IsCampfireOpen()) CloseInventory();
            else if (!IsCampfireOpen())                 OpenInventory();
        }
    }

    // ── GameState ─────────────────────────────────────────────────────────────
    public static bool IsUIOpen()
    {
        try { if ((bool)Variables.Scene(SceneManager.GetActiveScene()).Get("isOpen")) return true; }
        catch { }

        if (Instance != null)
        {
            if (Instance.IsInventoryOpen()) return true;
            if (Instance.IsCampfireOpen())  return true;
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
        warmthBar        = root.Q<ProgressBar>("warmth-bar");
        healthBar        = root.Q<ProgressBar>("health-bar");

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
        equipAxeSlot   = root.Q("equip-axe");
        equipAxeIcon   = root.Q("equip-axe-icon");
        equipAxeLabel  = root.Q<Label>("equip-axe-label");
    }

    void QuerySharedCraftingPanel()
    {
        craftingPanel      = root.Q("crafting-panel");
        campfireRecipeList = root.Q<ScrollView>("campfire-recipe-list");

        cfSlots  = new VisualElement[SLOT_COUNT];
        cfIcons  = new VisualElement[SLOT_COUNT];
        cfCounts = new Label[SLOT_COUNT];
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            cfSlots[i]  = root.Q($"cf-slot-{i}");
            cfIcons[i]  = root.Q($"cf-icon-{i}");
            cfCounts[i] = root.Q<Label>($"cf-count-{i}");
        }

        invSlots  = cfSlots;
        invIcons  = cfIcons;
        invCounts = cfCounts;
        invNames  = new Label[SLOT_COUNT];
    }

    void QueryCampfireStatusPanel()
    {
        campfireStatusPanel = root.Q("campfire-status-panel");
        fuelBar             = root.Q<ProgressBar>("fuel-bar");
        fuelText            = root.Q<Label>("fuel-text");
        shelterText         = root.Q<Label>("shelter-text");
        addFuelBtn          = root.Q<Button>("add-fuel-btn");
        upgradeBtn          = root.Q<Button>("upgrade-btn");
        fuelCostText        = root.Q<Label>("fuel-cost-text");
        upgradeCostText     = root.Q<Label>("upgrade-cost-text");
        woodCountLabel      = root.Q<Label>("wood-count");
        stoneCountLabel     = root.Q<Label>("stone-count");

        pips = new VisualElement[3];
        for (int i = 0; i < 3; i++)
            pips[i] = root.Q($"pip-{i}");
    }

    void QueryDeathScreen()
    {
        deathScreen = root.Q("death-screen");
        restartBtn  = root.Q<Button>("restart-btn");

        if (restartBtn != null)
            restartBtn.clicked += RestartGame;
    }

    // ── Death screen ──────────────────────────────────────────────────────────
    public void ShowDeathScreen()
    {
        deathScreen?.RemoveFromClassList("hidden");
    }

    public void HideDeathScreen()
    {
        deathScreen?.AddToClassList("hidden");
    }

    void RestartGame()
    {
        // Destroy persistent singletons so they're cleanly recreated on reload
        if (InventoryManager.Instance != null)
            Destroy(InventoryManager.Instance.gameObject);

        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        Destroy(gameObject); // destroys this UIManager; new one created by scene
        SceneManager.LoadScene(sceneIndex);
    }

    // ── Tab inventory ─────────────────────────────────────────────────────────
    public bool IsInventoryOpen()
    {
        return inventoryPanel != null && !inventoryPanel.ClassListContains("hidden");
    }

    public void OpenInventory()
    {
        inventoryPanel.RemoveFromClassList("hidden");
        craftingPanel.RemoveFromClassList("hidden");
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible   = true;
        SetVSOpen(true);
        RefreshRecipeList(campfireRecipeList, false);
        InventoryManager.Instance?.RefreshUI();
    }

    public void CloseInventory()
    {
        inventoryPanel.AddToClassList("hidden");
        craftingPanel.AddToClassList("hidden");
        CancelDrag();
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible   = false;
        SetVSOpen(false);
    }

    // ── Campfire ──────────────────────────────────────────────────────────────
    public bool IsCampfireOpen()
    {
        return campfireStatusPanel != null && !campfireStatusPanel.ClassListContains("hidden");
    }

    public void ShowCampfirePanels()
    {
        campfireStatusPanel.RemoveFromClassList("hidden");
        craftingPanel.RemoveFromClassList("hidden");
        RefreshRecipeList(campfireRecipeList, null);
        RefreshBackpackSlots();
    }

    public void HideCampfirePanels()
    {
        campfireStatusPanel.AddToClassList("hidden");
        craftingPanel.AddToClassList("hidden");
    }

    public bool IsCraftingOpen()    => IsCampfireOpen();
    public void ShowCraftingPanel() => ShowCampfirePanels();
    public void HideCraftingPanel() => HideCampfirePanels();

    void SetVSOpen(bool open)
    {
        try { Variables.Scene(SceneManager.GetActiveScene()).Set("isOpen", open); }
        catch { }
    }

    // ── Inventory interaction ─────────────────────────────────────────────────
    void SetupInventoryInteraction()
    {
        equipAxeSlot?.RegisterCallback<PointerDownEvent>(evt =>
        {
            InventoryManager.Instance?.UnequipAxe();
            evt.StopPropagation();
        });

        if (cfSlots == null) return;

        for (int i = 0; i < SLOT_COUNT; i++)
        {
            if (cfSlots[i] == null) continue;
            int index = i;

            cfSlots[i].RegisterCallback<PointerDownEvent>(evt =>
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

            cfSlots[i].RegisterCallback<PointerEnterEvent>(evt =>
            {
                if (!isDragging || index == dragFromIndex) return;
                cfSlots[index].AddToClassList("inv-slot-drag-over");
            });

            cfSlots[i].RegisterCallback<PointerLeaveEvent>(evt =>
            {
                cfSlots[index].RemoveFromClassList("inv-slot-drag-over");
            });
        }

        craftingPanel?.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (!isDragging) return;
            MoveDragGhost(evt.position);
        });

        craftingPanel?.RegisterCallback<PointerUpEvent>(evt =>
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

    void MoveDragGhost(Vector2 pos)
    {
        if (dragGhost == null) return;
        dragGhost.style.left = pos.x - 24;
        dragGhost.style.top  = pos.y - 24;
    }

    int GetSlotUnderPointer(Vector2 pos)
    {
        for (int i = 0; i < SLOT_COUNT; i++)
            if (cfSlots[i] != null && cfSlots[i].worldBound.Contains(pos)) return i;
        return -1;
    }

    void CancelDrag()
    {
        isDragging    = false;
        dragFromIndex = -1;
        dragGhost?.AddToClassList("hidden");
        for (int i = 0; i < SLOT_COUNT; i++)
            cfSlots[i]?.RemoveFromClassList("inv-slot-drag-over");
    }

    // ── Selected slot ─────────────────────────────────────────────────────────
    public void RefreshSelectedSlot(int selected)
    {
        for (int i = 0; i < SLOT_COUNT; i++)
            cfSlots[i]?.EnableInClassList("inv-slot-selected", i == selected);
    }

    // ── Refresh backpack slots ────────────────────────────────────────────────
    void RefreshBackpackSlots()
    {
        if (cfSlots == null || InventoryManager.Instance == null) return;
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            var slot = InventoryManager.Instance.GetSlot(i);
            bool has = slot.item != null;

            if (cfIcons[i] != null)
            {
                cfIcons[i].style.backgroundImage = has ? new StyleBackground(slot.item.icon) : StyleKeyword.None;
                cfIcons[i].style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (cfCounts[i] != null)
                cfCounts[i].text = has && slot.count > 1 ? slot.count.ToString() : "";
            if (cfSlots[i] != null)
                cfSlots[i].EnableInClassList("inv-slot-empty", !has);
        }
    }

    public void RefreshInventorySlots(InventoryManager.InventorySlot[] slots)
    {
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            bool has = i < slots.Length && slots[i].item != null;

            if (hbIcons[i] != null)
            {
                hbIcons[i].style.backgroundImage = has ? new StyleBackground(slots[i].item.icon) : StyleKeyword.None;
                hbIcons[i].style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (hbCounts[i] != null)
                hbCounts[i].text = has && slots[i].count > 1 ? slots[i].count.ToString() : "";
        }

        RefreshBackpackSlots();

        if (IsInventoryOpen()) RefreshRecipeList(campfireRecipeList, false);
        if (IsCampfireOpen())  RefreshRecipeList(campfireRecipeList, null);
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
    public void RefreshRecipeList(ScrollView list, bool? campfireOnly)
    {
        if (list == null || allRecipes == null) return;
        list.contentContainer.Clear();

        foreach (RecipeData recipe in allRecipes)
        {
            if (recipe == null) continue;
            if (campfireOnly == true  && !recipe.requiresCampfire) continue;
            if (campfireOnly == false &&  recipe.requiresCampfire) continue;

            Button btn = new Button();
            btn.AddToClassList("recipe-btn");

            Label title = new Label(recipe.recipeName);
            title.AddToClassList("recipe-title");
            btn.Add(title);

            string costStr   = "";
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

        if (IsInventoryOpen()) RefreshRecipeList(campfireRecipeList, false);
        if (IsCampfireOpen())  RefreshRecipeList(campfireRecipeList, null);
        RefreshBackpackSlots();
    }

    // ── HUD setters ───────────────────────────────────────────────────────────
    public void SetChopProgress(float n)  { if (chopBar         != null) chopBar.value = n; }
    public void SetChopBarVisible(bool v) { if (chopBarContainer != null) chopBarContainer.EnableInClassList("hidden", !v); }

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

    // ── Warmth & health ───────────────────────────────────────────────────────
    public void SetWarmth(float current, float max, bool isNight)
    {
        if (warmthBar == null) return;
        warmthBar.value = max > 0f ? current / max : 0f;
        // Amber during day, icy-blue at night
        warmthBar.EnableInClassList("warmth-cold", isNight);
    }

    public void SetHealth(float current, float max)
    {
        if (healthBar == null) return;
        float fraction = max > 0f ? current / max : 1f;
        healthBar.value = fraction;
        // Green normally, red when critically low
        healthBar.EnableInClassList("health-low", fraction < 0.33f);
    }

    // ── Campfire status setters ───────────────────────────────────────────────
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

    public void SetFuelCost(string t)     { if (fuelCostText    != null) fuelCostText.text    = t; }
    public void SetUpgradeCost(string t)  { if (upgradeCostText != null) upgradeCostText.text = t; }
    public void SetWoodCount(string t)    { if (woodCountLabel  != null) woodCountLabel.text  = t; }
    public void SetStoneCount(string t)   { if (stoneCountLabel != null) stoneCountLabel.text = t; }
    public void SetAddFuelEnabled(bool e)  { addFuelBtn?.SetEnabled(e); }
    public void SetUpgradeEnabled(bool e)  { upgradeBtn?.SetEnabled(e); }
    public void OnAddFuelClicked(System.Action cb) { if (addFuelBtn != null) addFuelBtn.clicked += cb; }
    public void OnUpgradeClicked(System.Action cb) { if (upgradeBtn != null) upgradeBtn.clicked += cb; }
}
