using UnityEngine;
using DnDTactics.Rules;
using DnDTactics.Data;
using DnDTactics.Characters;

namespace DnDTactics.Tests
{
    public class BuilderTester : MonoBehaviour
    {
        public Species species;
        public CharacterClass characterClass;
        public Background background; // use Sage for the expected output below

        void Start()
        {
            var b = new CharacterBuilder
            {
                characterName = "Test Mage",
                species = species,
                characterClass = characterClass,
                background = background,
                startingLevel = 1
            };
            b.UseStandardArrayInOrder();                  // STR15 DEX14 CON13 INT12 WIS10 CHA8
            b.SetBackgroundBonus(Ability.Intelligence, 2); // Sage offers Con/Int/Wis
            b.SetBackgroundBonus(Ability.Constitution, 1);

            var problems = b.Validate();
            Debug.Log($"Valid: {problems.Count == 0} (problems: {problems.Count})");

            if (b.IsComplete())
            {
                Character c = b.Build();
                Debug.Log($"Built: {c.characterName}, L{c.level} " +
                          $"{c.species.speciesName} {c.characterClass.className} " +
                          $"({c.background.backgroundName})");
                Debug.Log($"HP {c.currentHP}/{c.MaxHP} | AC {c.ArmorClass} | " +
                          $"INT {c.abilities.GetFinalScore(Ability.Intelligence)} " +
                          $"CON {c.abilities.GetFinalScore(Ability.Constitution)}");
            }

            // Now deliberately break a rule to watch validation catch it:
            b.SetBackgroundBonus(Ability.Strength, 1); // Sage can't boost STR, and total is now +4
            Debug.Log("After invalid edit:");
            foreach (string p in b.Validate()) Debug.Log("  - " + p);
        }
    }
}