using UnityEngine;

namespace Brewing.Minigames
{
    /// <summary>
    /// A brewing ingredient and its art. Minigames point at these instead of at sprites, so the same
    /// ingredient looks the same everywhere: whole on the recipe book, on the shelf, and ground to powder.
    /// </summary>
    [CreateAssetMenu(menuName = "Brewing/Ingredient", fileName = "Ingredient")]
    public class Ingredient : ScriptableObject
    {
        public string displayName;

        [Tooltip("The whole ingredient, as shown in the recipe book and before it is crushed.")]
        public Sprite sprite;

        [Tooltip("How it looks stocked on the shelf.")]
        public Sprite shelfSprite;

        [Tooltip("Tint for the shared gray powder sprite once the ingredient is crushed.")]
        public Color powderColor = Color.white;
    }
}
