using System;

namespace DnDTactics.Rules
{
    // One character's six ability scores. PER-CHARACTER data, so a plain
    // serializable class, NOT a ScriptableObject.
    [Serializable]
    public class AbilityScoreSet
    {
        // Indexed by (int)Ability: Strength=0, Dexterity=1, ... Charisma=5.
        public int[] baseScores = new int[6];        // before background
        public int[] backgroundBonuses = new int[6]; // +2/+1 from background

        public int GetBaseScore(Ability ability) => baseScores[(int)ability];
        public void SetBaseScore(Ability ability, int value) => baseScores[(int)ability] = value;

        public void AddBackgroundBonus(Ability ability, int amount) =>
            backgroundBonuses[(int)ability] += amount;

        // What the game actually uses: base + background.
        public int GetFinalScore(Ability ability) =>
            baseScores[(int)ability] + backgroundBonuses[(int)ability];

        // Reuses the modifier rule from your very first script.
        public int GetModifier(Ability ability) =>
            AbilityScores.Modifier(GetFinalScore(ability));
    }
}