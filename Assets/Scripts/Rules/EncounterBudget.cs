namespace DnDTactics.Rules
{
    public enum Difficulty { Easy, Standard, Hard }

    // Our OWN encounter-budget values (SRD doesn't include the DMG's tables).
    // Spirit of 5e — XP budget scales with party level and difficulty — but these
    // numbers are ours to tune for game feel.
    public static class EncounterBudget
    {
        // XP budget PER CHARACTER at a given level, for a "Standard" fight.
        // Tunable. Roughly tracks 5e's curve without copying its tables.
        private static readonly int[] PerCharacterStandard =
        {
            0,                                   // index 0 unused
            50, 100, 150, 250, 500,              // levels 1-5
            600, 750, 900, 1100, 1200,           // 6-10
            1600, 2000, 2200, 2500, 2800,        // 11-15
            3200, 3900, 4200, 4900, 5700         // 16-20
        };

        public static float DifficultyMultiplier(Difficulty d) => d switch
        {
            Difficulty.Easy => 0.6f,
            Difficulty.Hard => 1.5f,
            _ => 1f
        };

        // Total XP budget for the encounter, given party levels and difficulty.
        public static int TotalBudget(int[] partyLevels, Difficulty difficulty)
        {
            int sum = 0;
            foreach (int lvl in partyLevels)
            {
                int clamped = System.Math.Clamp(lvl, 1, 20);
                sum += PerCharacterStandard[clamped];
            }
            return (int)(sum * DifficultyMultiplier(difficulty));
        }
    }
}