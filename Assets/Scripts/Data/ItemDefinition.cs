using UnityEngine;

namespace DnDTactics.Data
{
    public enum ItemCategory { Consumable, Component, Misc }

    // A minimal item definition. Enough for consumables and revival components now;
    // expands toward full treasure/equipment later.
    [CreateAssetMenu(fileName = "NewItem", menuName = "DnD/Item")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string itemName = "New Item";

        [TextArea(2, 4)]
        public string description;

        public ItemCategory category = ItemCategory.Consumable;

        [Header("Value")]
        [Tooltip("Gold value, used for buying/selling and as drop weighting.")]
        public int goldValue = 10;

        [Header("Consumable effect (if any)")]
        [Tooltip("HP restored when used. 0 for non-healing items.")]
        public int healAmount = 0;

        [Tooltip("If true, this item can revive a Down character (e.g. a revival diamond).")]
        public bool isRevivalComponent = false;
    }
}