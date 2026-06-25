using System;
using System.Collections.Generic;
using System.Linq;
using DnDTactics.Rules;
using DnDTactics.Data;

namespace DnDTactics.Characters
{
    public enum ScoreMethod { StandardArray, PointBuy, Roll }

    // Holds in-progress character-creation choices, validates them against the
    // rules, and builds a finished Character. The UI will be a thin shell over this.
    public class CharacterBuilder
    {
        public string characterName = "";
        public Species species;
        public CharacterClass characterClass;
        public Background background;
        public int startingLevel = 1;

        public ScoreMethod method = ScoreMethod.StandardArray;

        public int[] baseScores = new int[6];        // indexed by (int)Ability
        public int[] backgroundBonuses = new int[6]; // the +2/+1 the player assigns

        // ---- convenience setters (the UI will call these) ----

        public void SetBaseScore(Ability a, int value) => baseScores[(int)a] = value;
        public void SetBackgroundBonus(Ability a, int amount) => backgroundBonuses[(int)a] = amount;

        public void UseStandardArrayInOrder()
        {
            method = ScoreMethod.StandardArray;
            int[] arr = AbilityScoreGeneration.StandardArray();
            for (int i = 0; i < 6; i++) baseScores[i] = arr[i];
        }

        // ---- point-buy helpers ----

        public int PointBuySpent()
        {
            int total = 0;
            foreach (int s in baseScores)
            {
                int cost = AbilityScoreGeneration.PointBuyCost(s);
                if (cost < 0) return -1; // an illegal score is present
                total += cost;
            }
            return total;
        }

        public int PointsRemaining() =>
            AbilityScoreGeneration.PointBuyBudget - Math.Max(0, PointBuySpent());

        // ---- validation: returns a list of human-readable problems ----

        public List<string> Validate()
        {
            var problems = new List<string>();

            if (string.IsNullOrWhiteSpace(characterName)) problems.Add("Name is empty.");
            if (species == null) problems.Add("No species selected.");
            if (characterClass == null) problems.Add("No class selected.");
            if (startingLevel < Progression.MinLevel || startingLevel > Progression.MaxLevel)
                problems.Add($"Starting level must be {Progression.MinLevel}-{Progression.MaxLevel}.");

            switch (method)
            {
                case ScoreMethod.PointBuy:
                    if (baseScores.Any(s => AbilityScoreGeneration.PointBuyCost(s) < 0))
                        problems.Add("Point-buy scores must each be 8-15.");
                    else if (PointBuySpent() > AbilityScoreGeneration.PointBuyBudget)
                        problems.Add($"Point-buy over budget by " +
                                     $"{PointBuySpent() - AbilityScoreGeneration.PointBuyBudget}.");
                    break;
                case ScoreMethod.StandardArray:
                    if (!baseScores.OrderBy(x => x)
                            .SequenceEqual(AbilityScoreGeneration.StandardArray().OrderBy(x => x)))
                        problems.Add("Standard array must use 15,14,13,12,10,8 each exactly once.");
                    break;
                case ScoreMethod.Roll:
                    if (baseScores.Any(s => s < 3 || s > 18))
                        problems.Add("Rolled scores must be 3-18.");
                    break;
            }

            problems.AddRange(ValidateBackgroundBonuses());
            return problems;
        }

        private List<string> ValidateBackgroundBonuses()
        {
            var problems = new List<string>();
            if (background == null) { problems.Add("No background selected."); return problems; }

            var allowed = new HashSet<Ability>(background.abilityOptions);
            int total = 0;
            var amounts = new List<int>();

            for (int i = 0; i < 6; i++)
            {
                int amt = backgroundBonuses[i];
                if (amt == 0) continue;
                total += amt;
                amounts.Add(amt);
                if (!allowed.Contains((Ability)i))
                    problems.Add($"{background.backgroundName} can't boost {(Ability)i}.");
            }

            if (total != 3)
                problems.Add($"Background bonuses must total +3 (currently +{total}).");
            else
            {
                amounts.Sort();
                bool twoOne = amounts.SequenceEqual(new[] { 1, 2 });
                bool oneEach = amounts.SequenceEqual(new[] { 1, 1, 1 });
                if (!twoOne && !oneEach)
                    problems.Add("Background bonus must be +2/+1 to two abilities, or +1 to three.");
            }
            return problems;
        }

        public bool IsComplete() => Validate().Count == 0;

        // ---- build the finished character ----

        public Character Build()
        {
            var problems = Validate();
            if (problems.Count > 0)
                throw new InvalidOperationException("Cannot build: " + string.Join(" ", problems));

            var abilities = new AbilityScoreSet();
            for (int i = 0; i < 6; i++)
            {
                abilities.baseScores[i] = baseScores[i];
                abilities.backgroundBonuses[i] = backgroundBonuses[i];
            }
            return new Character(characterName, species, characterClass,
                                 background, abilities, startingLevel);
        }
    }
}