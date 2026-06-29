using System.Collections.Generic;

namespace DnDTactics.Core
{
    // Debug-only: forces specific encounter composition for reproducible combat testing.
    // When Enabled, TriggerEncounter spawns exactly the monsters listed in ForcedMonsters
    // (by monsterName) instead of the random pick. Toggle/edit via the in-scene debug panel.
    public static class DebugSpawn
    {
        public static bool Enabled = false;

        // Monster names to spawn (must match MonsterStats.monsterName in the pool).
        // e.g. ["Goblin"] = one goblin every encounter; ["Goblin","Goblin"] = two; etc.
        public static List<string> ForcedMonsters = new();

        // Convenience: set a single monster type with a count.
        public static void SetSingle(string monsterName, int count)
        {
            ForcedMonsters.Clear();
            for (int i = 0; i < count; i++) ForcedMonsters.Add(monsterName);
            Enabled = ForcedMonsters.Count > 0;
        }

        public static void Clear()
        {
            ForcedMonsters.Clear();
            Enabled = false;
        }
    }
}