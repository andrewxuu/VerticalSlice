using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Recipe Data")]
public class RecipeData : ScriptableObject
{
    [Header("Identity")]
    public string recipeName;
    public Sprite icon;

    [Header("Ingredients")]
    public Ingredient[] ingredients;

    [Header("Result")]
    public ItemData result;
    public int resultCount = 1;

    [Header("Access")]
    [Tooltip("If true, this recipe can only be crafted at the campfire.")]
    public bool requiresCampfire;

    [Tooltip("If true, this recipe can only be crafted once per session.")]
    public bool craftOnce;

    // Runtime only — not serialized, so it resets every play session.
    [System.NonSerialized] public bool isCrafted;

    [System.Serializable]
    public struct Ingredient
    {
        public ItemData item;
        public int      amount;
    }
}