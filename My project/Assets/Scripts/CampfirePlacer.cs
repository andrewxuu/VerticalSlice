using UnityEngine;

/// Attach to the Player.
/// Placement is triggered by selecting a placeable item in the inventory
/// and pressing Q. No separate material cost — the item itself is consumed.
public class CampfirePlacer : MonoBehaviour
{
    [Header("Placement")]
    public LayerMask groundLayer;
    public KeyCode   placeKey        = KeyCode.Q;
    public float     maxPlaceDistance = 10f;

    [Header("Ghost Preview")]
    public Color validColor   = new Color(0f, 1f, 0f, 0.35f);
    public Color invalidColor = new Color(1f, 0f, 0f, 0.35f);

    private bool          isPlacing;
    private ItemData      placingItem;
    private int           placingSlotIndex = -1;
    private GameObject    ghostObject;
    private Renderer[]    ghostRenderers;
    private bool          ghostOnGround;
    private Material      ghostMaterial;

    void Start()
    {
        ghostMaterial = new Material(Shader.Find("Standard"));
        ghostMaterial.SetFloat("_Mode", 3);
        ghostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        ghostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        ghostMaterial.SetInt("_ZWrite", 0);
        ghostMaterial.DisableKeyword("_ALPHATEST_ON");
        ghostMaterial.EnableKeyword("_ALPHABLEND_ON");
        ghostMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        ghostMaterial.renderQueue = 3000;
        ghostMaterial.color = validColor;
    }

    void Update()
    {
        if (GameState.IsUIOpen()) { CancelPlacement(); return; }

        CheckForPlaceableSelection();

        if (!isPlacing) return;

        UpdateGhostPosition();

        if (Input.GetMouseButtonDown(0) && ghostOnGround)
            PlaceItem();

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(placeKey))
            CancelPlacement();
    }

    // ── Watch for Q pressed when a placeable item is selected ─────────────────
    void CheckForPlaceableSelection()
    {
        if (!Input.GetKeyDown(placeKey)) return;
        if (isPlacing) return;

        ItemData selected = InventoryManager.Instance?.GetSelectedItem();
        if (selected == null || !selected.isPlaceable || selected.placementPrefab == null)
        {
            Debug.Log("[CampfirePlacer] Selected item is not placeable.");
            return;
        }

        int slotIndex = InventoryManager.Instance.GetSelectedSlotIndex();
        StartPlacement(selected, slotIndex);
    }

    // ── Placement flow ────────────────────────────────────────────────────────
    void StartPlacement(ItemData item, int slotIndex)
    {
        isPlacing        = true;
        placingItem      = item;
        placingSlotIndex = slotIndex;

        ghostObject = Instantiate(item.placementPrefab);
        ghostObject.name = "_PlacementGhost";

        // Strip physics, colliders, and scripts so the ghost is purely visual
        foreach (Collider    col in ghostObject.GetComponentsInChildren<Collider>())    Destroy(col);
        foreach (Rigidbody   rb  in ghostObject.GetComponentsInChildren<Rigidbody>())   Destroy(rb);
        foreach (MonoBehaviour mb in ghostObject.GetComponentsInChildren<MonoBehaviour>()) Destroy(mb);

        ghostRenderers = ghostObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in ghostRenderers)
            r.material = new Material(ghostMaterial);

        ghostObject.SetActive(false);
    }

    void UpdateGhostPosition()
    {
        if (ghostObject == null) return;

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, maxPlaceDistance, groundLayer))
        {
            ghostObject.SetActive(true);
            ghostObject.transform.position = hit.point;
            ghostOnGround = true;
            foreach (Renderer r in ghostRenderers) r.material.color = validColor;
        }
        else
        {
            ghostObject.SetActive(true);
            ghostOnGround = false;
            ghostObject.transform.position = transform.position + transform.forward * 3f;
            foreach (Renderer r in ghostRenderers) r.material.color = invalidColor;
        }
    }

    void PlaceItem()
    {
        if (ghostObject == null || placingItem == null) return;

        Vector3 position = ghostObject.transform.position;
        Destroy(ghostObject);

        // Consume one item from the inventory slot
        InventoryManager.Instance.RemoveFromSlot(placingSlotIndex, 1);

        // Spawn the real prefab
        GameObject placed = Instantiate(placingItem.placementPrefab, position, Quaternion.identity);
        placed.name = placingItem.itemName;

        // Wire up campfire interaction if present
        CampfireInteraction interaction = placed.GetComponent<CampfireInteraction>();
        if (interaction != null)
            interaction.player = transform;

        isPlacing        = false;
        placingItem      = null;
        placingSlotIndex = -1;
    }

    void CancelPlacement()
    {
        if (ghostObject != null) Destroy(ghostObject);
        isPlacing        = false;
        placingItem      = null;
        placingSlotIndex = -1;
        ghostOnGround    = false;
    }

    void OnDrawGizmosSelected()
    {
        if (!isPlacing) return;
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, maxPlaceDistance);
    }
}
