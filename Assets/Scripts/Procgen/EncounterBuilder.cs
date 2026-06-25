using System.Collections.Generic;
using DnDTactics.Rules;
using DnDTactics.Data;

namespace DnDTactics.Procgen
{
    public class BuiltEncounter
    {
        public List<MonsterStats> monsters = new();
        public int totalXp;
        public int budget;
    }

    // Selects monsters to fill an XP budget. Pure logic over a provided monster pool.
    public static class EncounterBuilder
    {
        public static BuiltEncounter Build(
            int[] partyLevels, Difficulty difficulty,
            IReadOnlyList<MonsterStats> pool, int seed,
            bool includeBoss = false, int maxMonsters = 8)
        {
            var result = new BuiltEncounter();
            if (pool == null || pool.Count == 0) return result;

            result.budget = EncounterBudget.TotalBudget(partyLevels, difficulty);
            int remaining = result.budget;
            var rng = new System.Random(seed);

            // Optional boss: spend up to ~40% of budget on one strong monster.
            if (includeBoss)
            {
                int bossCap = (int)(result.budget * 0.4f);
                MonsterStats boss = BestUnder(pool, bossCap);
                if (boss != null)
                {
                    int xp = ChallengeRating.XpForCr(boss.challengeRating);
                    result.monsters.Add(boss);
                    result.totalXp += xp;
                    remaining -= xp;
                }
            }

            // Fill the rest with affordable random picks.
            int guard = 0;
            while (remaining > 0 && result.monsters.Count < maxMonsters && guard++ < 200)
            {
                var affordable = new List<MonsterStats>();
                foreach (var m in pool)
                    if (ChallengeRating.XpForCr(m.challengeRating) <= remaining)
                        affordable.Add(m);
                if (affordable.Count == 0) break;

                var pick = affordable[rng.Next(affordable.Count)];
                int xp = ChallengeRating.XpForCr(pick.challengeRating);
                result.monsters.Add(pick);
                result.totalXp += xp;
                remaining -= xp;
            }

            return result;
        }

        // The most expensive monster whose XP fits under a cap (for the boss slot).
        static MonsterStats BestUnder(IReadOnlyList<MonsterStats> pool, int xpCap)
        {
            MonsterStats best = null; int bestXp = -1;
            foreach (var m in pool)
            {
                int xp = ChallengeRating.XpForCr(m.challengeRating);
                if (xp <= xpCap && xp > bestXp) { bestXp = xp; best = m; }
            }
            return best;
        }
    }
}