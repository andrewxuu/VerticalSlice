using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName;

    [Header("Stack")]
    [Min(1)] public int maxStackSize = 99;

    [Header("Display")]
    public Sprite icon;

    [Header("Held Model")]
    [Tooltip("3D prefab that appears in the player's hand when this item is selected.")]
    public GameObject heldPrefab;

    [Tooltip("Position offset relative to the hand bone.")]
    public Vector3 holdOffset = Vector3.zero;

    [Tooltip("Rotation offset relative to the hand bone.")]
    public Vector3 holdRotation = Vector3.zero;

    [Header("Placement")]
    [Tooltip("If true, selecting this item and pressing Q enters placement mode.")]
    public bool isPlaceable;

    [Tooltip("The prefab to spawn when this item is placed in the world.")]
    public GameObject placementPrefab;
}
