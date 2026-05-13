using UnityEngine;

/// Attach to the root Player GameObject.
/// Drag the RightHand bone from the character Hierarchy into Hand Bone.
public class ItemHolder : MonoBehaviour
{
    public static ItemHolder Instance { get; private set; }

    [Header("Hand Bone")]
    [Tooltip("Drag the RightHand (or equivalent) bone from the Hierarchy here.")]
    public Transform handBone;

    private GameObject currentHeldObject;
    private ItemData   currentHeldItem;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    // ── Called by InventoryManager whenever selected slot changes ─────────────
    public void UpdateHeldItem(ItemData item)
    {
        // No change
        if (item == currentHeldItem) return;

        // Destroy previous held model
        if (currentHeldObject != null)
        {
            Destroy(currentHeldObject);
            currentHeldObject = null;
        }

        currentHeldItem = item;

        // Nothing to show
        if (item == null || item.heldPrefab == null) return;

        if (handBone == null)
        {
            Debug.LogWarning("[ItemHolder] No hand bone assigned — drag your RightHand bone into the Inspector.");
            return;
        }

        // Spawn and parent to hand bone
        currentHeldObject = Instantiate(item.heldPrefab, handBone);
        currentHeldObject.transform.localPosition    = item.holdOffset;
        currentHeldObject.transform.localEulerAngles = item.holdRotation;
        currentHeldObject.name = $"_Held_{item.itemName}";

        // Strip physics so it doesn't collide with anything
        foreach (Rigidbody   rb  in currentHeldObject.GetComponentsInChildren<Rigidbody>())  Destroy(rb);
        foreach (Collider    col in currentHeldObject.GetComponentsInChildren<Collider>())    Destroy(col);
    }

    public void ClearHeldItem() => UpdateHeldItem(null);
}
