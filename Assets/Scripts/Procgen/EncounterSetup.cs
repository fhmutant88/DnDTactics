using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DnDTactics.Rules;
using DnDTactics.Data;
using DnDTactics.Characters;
using DnDTactics.Combat;

namespace DnDTactics.Procgen
{
    // Conducts the full slice: generate dungeon -> build encounter -> place -> spawn -> fight.
    [DefaultExecutionOrder(200)] // after dungeon (Start) and CombatManager
    public class EncounterSetup : MonoBehaviour
    {
        [Header("References")]
        public DungeonVisualizer dungeon;
        public CombatManager combat;

        [Header("Party (test characters for now)")]
        public Species partySpecies;
        public CharacterClass partyClass;
        public Background partyBackground;
        public int partySize = 4;
        public int[] partyLevels = { 3, 3, 3, 3 };

        [Header("Encounter")]
        public List<MonsterStats> monsterPool = new();
        public Difficulty difficulty = Difficulty.Standard;
        public bool includeBoss = false;
        public int seed = 0;

        IEnumerator Start()
        {
            // Wait one frame so DungeonVisualizer.Start() has generated the map + grid.
            yield return null;

            if (dungeon == null || combat == null || dungeon.Grid == null)
            {
                Debug.LogError("EncounterSetup: missing references or grid not ready.");
                yield break;
            }

            int useSeed = seed != 0 ? seed : System.Environment.TickCount;

            // 1. Build the encounter from the budget.
            var enc = EncounterBuilder.Build(partyLevels, difficulty, monsterPool, useSeed, includeBoss);
            Debug.Log($"Encounter: {enc.monsters.Count} monsters, {enc.totalXp}/{enc.budget} XP.");

            // 2. Place party + enemies into rooms.
            var placement = EncounterPlacer.Place(dungeon.Map, partySize, enc.monsters.Count, useSeed);

            // 3. Hand the generated grid to combat.
            combat.useExternalSetup = true;
            combat.SetGrid(dungeon.Grid);

            // 4. Spawn the party.
            int placedParty = Mathf.Min(partySize, placement.partySpawns.Count);
            for (int i = 0; i < placedParty; i++)
            {
                int lvl = i < partyLevels.Length ? partyLevels[i] : 1;
                var hero = MakeHero($"Hero {i + 1}", lvl);
                combat.SpawnPlayer(hero, placement.partySpawns[i]);
            }

            // 5. Spawn the monsters.
            int placedEnemies = Mathf.Min(enc.monsters.Count, placement.enemySpawns.Count);
            for (int i = 0; i < placedEnemies; i++)
                combat.SpawnMonster(enc.monsters[i], placement.enemySpawns[i]);

            // 6. Fight!
            combat.StartExternalEncounter();
        }

        Character MakeHero(string name, int level)
        {
            var abilities = new AbilityScoreSet();
            int[] array = AbilityScoreGeneration.StandardArray();
            Ability[] all = (Ability[])System.Enum.GetValues(typeof(Ability));
            for (int i = 0; i < all.Length; i++) abilities.SetBaseScore(all[i], array[i]);
            return new Character(name, partySpecies, partyClass, partyBackground, abilities, level);
        }
    }
}