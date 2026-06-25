using System.Collections.Generic;
using UnityEngine;
using DnDTactics.Rules;
using DnDTactics.Data;

namespace DnDTactics.Procgen
{
    public class EncounterBudgetTester : MonoBehaviour
    {
        public List<MonsterStats> monsterPool = new();
        public int[] partyLevels = { 3, 3, 3, 3 };
        public Difficulty difficulty = Difficulty.Standard;
        public bool includeBoss = false;
        public int seed = 0;

        void Start()
        {
            int useSeed = seed != 0 ? seed : System.Environment.TickCount;
            var enc = EncounterBuilder.Build(partyLevels, difficulty, monsterPool, useSeed, includeBoss);

            Debug.Log($"=== Encounter (party {string.Join(",", partyLevels)}, " +
                      $"{difficulty}, boss={includeBoss}) ===");
            Debug.Log($"Budget {enc.budget} XP | Spent {enc.totalXp} XP | {enc.monsters.Count} monsters");
            var counts = new Dictionary<string, int>();
            foreach (var m in enc.monsters)
                counts[m.monsterName] = counts.GetValueOrDefault(m.monsterName) + 1;
            foreach (var kv in counts) Debug.Log($"  {kv.Value}x {kv.Key}");
        }
    }
}