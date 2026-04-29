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

}