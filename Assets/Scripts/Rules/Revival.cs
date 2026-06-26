namespace DnDTactics.Rules
{
    // Revival cost/rules. Our own tunable values (SRD doesn't price town revival),
    // scaled by the dead character's level so it tracks progression.
    public static class Revival
    {
        // Town healer fee to revive a Down character of the given level.
        // Base + per-level, tunable for game feel.
        public static int TownHealerCost(int level)
        {
            int clamped = System.Math.Clamp(level, 1, 20);
            return 100 + clamped * 50;   // L1 = 150g, L5 = 350g, L10 = 600g, L20 = 1100g
        }

        // HP a character returns with after revival: a fraction of max (comes back fragile).
        public static int RevivedHP(int maxHP)
        {
            return System.Math.Max(1, maxHP / 4); // 25% of max, at least 1
        }
    }
}