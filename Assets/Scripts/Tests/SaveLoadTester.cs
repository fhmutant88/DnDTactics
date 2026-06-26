using UnityEngine;
using DnDTactics.Rules;
using DnDTactics.Data;
using DnDTactics.Characters;

namespace DnDTactics.Tests
{
    public class SaveLoadTester : MonoBehaviour
    {
        public Species species;        // drag Human
        public CharacterClass cls;     // drag Fighter
        public Background background;   // drag Soldier

        void Start()
        {
            // 1. Build a distinctive character.
            var abilities = new AbilityScoreSet();
            int[] arr = AbilityScoreGeneration.StandardArray();
            Ability[] all = (Ability[])System.Enum.GetValues(typeof(Ability));
            for (int i = 0; i < all.Length; i++) abilities.SetBaseScore(all[i], arr[i]);
            abilities.AddBackgroundBonus(Ability.Strength, 2);
            abilities.AddBackgroundBonus(Ability.Dexterity, 1);

            var original = new Character("Sir Testalot", species, cls, background, abilities, 5);
            original.TakeDamage(7); // make HP non-default so we can verify it round-trips
            Log("ORIGINAL", original);

            // 2. Save, then "forget" it and load a fresh copy from disk.
            SaveSystem.SaveCharacter("slot1", original);
            Character loaded = SaveSystem.LoadCharacter("slot1");
            Log("LOADED  ", loaded);

            // 3. Compare the fields that matter.
            bool match =
                loaded != null &&
                loaded.characterName == original.characterName &&
                loaded.species == original.species &&
                loaded.characterClass == original.characterClass &&
                loaded.background == original.background &&
                loaded.level == original.level &&
                loaded.currentXp == original.currentXp &&
                loaded.currentHP == original.currentHP &&
                loaded.MaxHP == original.MaxHP &&
                loaded.AbilityModifier(Ability.Strength) == original.AbilityModifier(Ability.Strength);
            Debug.Log(match ? "<color=green>ROUND-TRIP MATCH ✓</color>"
                            : "<color=red>ROUND-TRIP MISMATCH ✗</color>");

            // 4. Permadeath check: a downed character must reload as still down.
            original.TakeDamage(9999);
            SaveSystem.SaveCharacter("slot1", original);
            Character deadLoaded = SaveSystem.LoadCharacter("slot1");
            Debug.Log($"Dead round-trip: IsDown={deadLoaded.IsDown} HP={deadLoaded.currentHP} " +
                      (deadLoaded.IsDown ? "✓" : "✗ (should be down)"));
        }

        void Log(string tag, Character c)
        {
            if (c == null) { Debug.Log($"{tag}: <null>"); return; }
            Debug.Log($"{tag}: {c.characterName} L{c.level} " +
                      $"{(c.species ? c.species.speciesName : "—")} " +
                      $"{(c.characterClass ? c.characterClass.className : "—")} " +
                      $"({(c.background ? c.background.backgroundName : "—")}) " +
                      $"HP {c.currentHP}/{c.MaxHP} XP {c.currentXp} STR {c.abilities.GetFinalScore(Ability.Strength)}");
        }
    }
}