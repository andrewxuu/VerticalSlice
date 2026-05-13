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

    [Header("Placement")]
    [Tooltip("If true, selecting this item and pressing Q enters placement mode.")]
    public bool       isPlaceable;
    public GameObject placementPrefab;

    [Header("Held Item")]
    public GameObject heldPrefab;
    public Vector3    holdOffset;
    public Vector3    holdRotation;
    public Vector3    holdScale = Vector3.one;
}
