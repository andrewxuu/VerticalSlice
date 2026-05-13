using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

public class CampfireInteraction : MonoBehaviour
{
    [Header("Fuel")]
    public float maxFuel      = 100f;
    public float fuelBurnRate = 1f;
    public float fuelPerWood  = 25f;

    [Header("Shelter")]
    public int maxShelterLevel = 3;
    public int woodPerUpgrade  = 3;
    public int stonePerUpgrade = 2;

    [Header("Warmth")]
    public float baseWarmthRadius = 3f;
    public float radiusPerLevel   = 2f;

    [Header("Items")]
    public ItemData woodItem;
    public ItemData stoneItem;

    [Header("Interaction")]
    public float interactRange = 3f;
    [HideInInspector] public Transform player;

    [Header("Visuals")]
    public Light      fireLight;
    public float      maxLightIntensity = 2f;
    public GameObject fireVFX;

    private float         currentFuel;
    private int           shelterLevel;
    private bool          menuOpen;
    private DayNightCycle dayNight;
    private bool          buttonsWired;

    void Start()
    {
        currentFuel  = 0f;
        shelterLevel = 0;

        if (fireLight != null) fireLight.intensity = 0f;
        if (fireVFX   != null) fireVFX.SetActive(false);

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        dayNight = FindObjectOfType<DayNightCycle>();
    }

    void Update()
    {
        BurnFuel();
        UpdateFireVisuals();

        if (!buttonsWired && UIManager.Instance != null)
        {
            UIManager.Instance.OnAddFuelClicked(AddFuel);
            UIManager.Instance.OnUpgradeClicked(UpgradeShelter);
            buttonsWired = true;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (menuOpen)          CloseMenu();
            else if (IsInRange())  OpenMenu();
        }

        if (menuOpen && !IsInRange()) CloseMenu();
        if (menuOpen) RefreshUI();
    }

    void BurnFuel()
    {
        if (currentFuel <= 0f) return;
        currentFuel = Mathf.Max(0f, currentFuel - fuelBurnRate * Time.deltaTime);
    }

    public void AddFuel()
    {
        if (!InventoryManager.Instance.HasItem(woodItem, 1)) return;
        InventoryManager.Instance.RemoveItem(woodItem, 1);
        currentFuel = Mathf.Min(currentFuel + fuelPerWood, maxFuel);
    }

    public void UpgradeShelter()
    {
        if (shelterLevel >= maxShelterLevel) return;
        if (!InventoryManager.Instance.HasItem(woodItem,  woodPerUpgrade))  return;
        if (!InventoryManager.Instance.HasItem(stoneItem, stonePerUpgrade)) return;
        InventoryManager.Instance.RemoveItem(woodItem,  woodPerUpgrade);
        InventoryManager.Instance.RemoveItem(stoneItem, stonePerUpgrade);
        shelterLevel++;
    }

    void OpenMenu()
    {
        menuOpen = true;
        UIManager.Instance.ShowCraftingPanel();
        if (dayNight != null) dayNight.timeMultiplier = 0f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible   = true;
        SetVSOpen(true);
    }

    void CloseMenu()
    {
        menuOpen = false;
        UIManager.Instance.HideCraftingPanel();
        if (dayNight != null) dayNight.timeMultiplier = 1f;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible   = false;
        SetVSOpen(false);
    }

    void SetVSOpen(bool open)
    {
        try { Variables.Scene(SceneManager.GetActiveScene()).Set("isOpen", open); }
        catch { }
    }

    void RefreshUI()
    {
        if (UIManager.Instance == null) return;

        UIManager.Instance.SetFuel(currentFuel, maxFuel);
        UIManager.Instance.SetShelterLevel(shelterLevel, maxShelterLevel);

        int w = InventoryManager.Instance.GetItemCount(woodItem);
        int s = InventoryManager.Instance.GetItemCount(stoneItem);

        UIManager.Instance.SetFuelCost($"1 wood — have: {w}");
        UIManager.Instance.SetWoodCount($"Wood: {w}");
        UIManager.Instance.SetStoneCount($"Stone: {s}");
        UIManager.Instance.SetUpgradeCost(shelterLevel >= maxShelterLevel
            ? "Maxed out"
            : $"{woodPerUpgrade}W + {stonePerUpgrade}S — have: {w}W / {s}S");

        UIManager.Instance.SetAddFuelEnabled(InventoryManager.Instance.HasItem(woodItem, 1));
        UIManager.Instance.SetUpgradeEnabled(
            shelterLevel < maxShelterLevel
            && InventoryManager.Instance.HasItem(woodItem,  woodPerUpgrade)
            && InventoryManager.Instance.HasItem(stoneItem, stonePerUpgrade));
    }

    void UpdateFireVisuals()
    {
        bool hasFuel = currentFuel > 0f;
        if (fireVFX   != null) fireVFX.SetActive(hasFuel);
        if (fireLight == null) return;

        if (!hasFuel) { fireLight.intensity = 0f; return; }
        float ratio         = currentFuel / maxFuel;
        fireLight.intensity = ratio * maxLightIntensity * (1f + shelterLevel * 0.3f);
        fireLight.range     = baseWarmthRadius + shelterLevel * radiusPerLevel;
    }

    public int   GetShelterLevel()  => shelterLevel;
    public float GetWarmthRadius()  => baseWarmthRadius + shelterLevel * radiusPerLevel;
    public bool  HasFuel()          => currentFuel > 0f;

    bool IsInRange()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= interactRange;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, baseWarmthRadius + shelterLevel * radiusPerLevel);
    }
}
