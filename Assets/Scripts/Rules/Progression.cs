using System;

namespace DnDTactics.Rules
{
    // Level/XP progression and proficiency bonus. Standard 5e values.
    public static class Progression
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 20;

        // XP required to BE a given level. Index = level (1..20); index 0 unused.
        private static readonly int[] XpThresholds =
        {
            0,
            0, 300, 900, 2700, 6500,
            14000, 23000, 34000, 48000, 64000,
            85000, 100000, 120000, 140000, 165000,
            195000, 225000, 265000, 305000, 355000
        };

        public static int XpForLevel(int level)
        {
            level = Math.Clamp(level, MinLevel, MaxLevel);
            return XpThresholds[level];
        }

        // Highest level whose XP requirement is satisfied by this XP total.
        public static int LevelForXp(int xp)
        {
            int level = MinLevel;
            for (int l = MinLevel; l <= MaxLevel; l++)
                if (xp >= XpThresholds[l]) level = l;
            return level;
        }

        // +2 at levels 1-4, then +1 every 4 levels (so +6 at level 20).
        public static int ProficiencyBonus(int level)
        {
            level = Math.Clamp(level, MinLevel, MaxLevel);
            return 2 + (level - 1) / 4;
        }
    }
}