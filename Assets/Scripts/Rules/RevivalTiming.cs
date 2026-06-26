namespace DnDTactics.Rules
{
    // Death-window timing, measured in LONG RESTS (8-hour rests), faithful to 5e.
    // Revivify ≈ immediate (before any long rest). Raise Dead ≈ within 10 days = 10 long rests.
    public static class RevivalTiming
    {
        // Fast field path (Revivify): allowed only before any long rest has passed since death.
        public const int FastWindowLongRests = 0;

        // Revivable window (town healer / slow field): within this many long rests of death.
        // After this, the character becomes permanently Dead.
        public const int RevivableWindowLongRests = 10;

        public static bool FastPathOpen(int longRestsSinceDeath) =>
            longRestsSinceDeath <= FastWindowLongRests;

        public static bool StillRevivable(int longRestsSinceDeath) =>
            longRestsSinceDeath <= RevivableWindowLongRests;
    }
}