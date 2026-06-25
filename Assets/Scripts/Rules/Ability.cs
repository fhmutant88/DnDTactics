using UnityEngine;

namespace DnDTactics.Rules
{
    // The six core 5e abilities.
    public enum Ability
    {
        Strength,
        Dexterity,
        Constitution,
        Intelligence,
        Wisdom,
        Charisma
    }

    // Shared rules math. Not attached to anything; just logic other code calls.
    public static class AbilityScores
    {
        // 5e modifier: floor((score - 10) / 2). A 16 gives +3, an 8 gives -1.
        public static int Modifier(int score)
        {
            return Mathf.FloorToInt((score - 10) / 2f);
        }
    }
}