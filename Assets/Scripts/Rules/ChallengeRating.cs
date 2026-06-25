using System.Collections.Generic;

namespace DnDTactics.Rules
{
    // Maps a Challenge Rating to its XP value (standard 5e CR->XP values).
    public static class ChallengeRating
    {
        private static readonly Dictionary<float, int> CrToXp = new()
        {
            { 0f, 10 }, { 0.125f, 25 }, { 0.25f, 50 }, { 0.5f, 100 },
            { 1f, 200 }, { 2f, 450 }, { 3f, 700 }, { 4f, 1100 },
            { 5f, 1800 }, { 6f, 2300 }, { 7f, 2900 }, { 8f, 3900 },
            { 9f, 5000 }, { 10f, 5900 }, { 11f, 7200 }, { 12f, 8400 },
            { 13f, 10000 }, { 14f, 11500 }, { 15f, 13000 }, { 16f, 15000 },
            { 17f, 18000 }, { 18f, 20000 }, { 19f, 22000 }, { 20f, 25000 },
            { 21f, 33000 }, { 22f, 41000 }, { 23f, 50000 }, { 24f, 62000 },
            { 25f, 75000 }, { 30f, 155000 }
        };

        // Nearest defined CR's XP (so odd values still resolve sensibly).
        public static int XpForCr(float cr)
        {
            if (CrToXp.TryGetValue(cr, out int xp)) return xp;
            float nearest = 0f; float bestGap = float.MaxValue;
            foreach (var kv in CrToXp)
            {
                float gap = Mathf.AbsF(kv.Key - cr);
                if (gap < bestGap) { bestGap = gap; nearest = kv.Key; }
            }
            return CrToXp[nearest];
        }

        private static class Mathf { public static float AbsF(float v) => v < 0 ? -v : v; }
    }
}