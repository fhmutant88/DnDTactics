using UnityEngine;
using DnDTactics.Rules;
using DnDTactics.Data;
using DnDTactics.Characters;

namespace DnDTactics.Tests
{
    public class CharacterTester : MonoBehaviour
    {
        public string characterName = "Test Hero";
        public Species species;
        public CharacterClass characterClass;
        public Background background;
        [Range(1, 20)] public int startingLevel = 1;

        void Start()
        {
            // Base scores: Standard Array assigned in ability order.
            var abilities = new AbilityScoreSet();
            int[] array = AbilityScoreGeneration.StandardArray();
            Ability[] all = (Ability[])System.Enum.GetValues(typeof(Ability));
            for (int i = 0; i < all.Length; i++)
                abilities.SetBaseScore(all[i], array[i]);

            // Background: +2 to its first option, +1 to its second.
            if (background != null && background.abilityOptions.Count >= 2)
            {
                abilities.AddBackgroundBonus(background.abilityOptions[0], 2);
                abilities.AddBackgroundBonus(background.abilityOptions[1], 1);
            }

            var hero = new Character(characterName, species, characterClass,
                                     background, abilities, startingLevel);
            PrintSheet(hero);

            // XP test: only meaningful from level 1.
            if (hero.level == 1)
            {
                bool leveled = hero.AddXp(300);
                Debug.Log($"Granted 300 XP. Leveled up: {leveled}. Now level {hero.level}.");
                PrintSheet(hero);
            }
        }

        void PrintSheet(Character c)
        {
            string sp = c.species ? c.species.speciesName : "—";
            string cl = c.characterClass ? c.characterClass.className : "—";
            string bg = c.background ? c.background.backgroundName : "—";

            Debug.Log(
                $"=== {c.characterName} ===\n" +
                $"Level {c.level} {sp} {cl} ({bg})\n" +
                $"XP {c.currentXp} | Prof +{c.ProficiencyBonus}\n" +
                $"HP {c.currentHP}/{c.MaxHP} | AC {c.ArmorClass} | Init {Signed(c.InitiativeModifier)}\n" +
                $"STR {Score(c, Ability.Strength)}  DEX {Score(c, Ability.Dexterity)}  CON {Score(c, Ability.Constitution)}\n" +
                $"INT {Score(c, Ability.Intelligence)}  WIS {Score(c, Ability.Wisdom)}  CHA {Score(c, Ability.Charisma)}");
        }

        string Score(Character c, Ability a) => $"{c.abilities.GetFinalScore(a)} ({Signed(c.AbilityModifier(a))})";
        string Signed(int n) => (n >= 0 ? "+" : "") + n;
    }
}