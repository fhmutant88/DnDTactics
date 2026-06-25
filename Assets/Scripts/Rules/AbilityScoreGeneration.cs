namespace DnDTactics.Rules
{
    // The three SRD-legal ways to generate the six base scores.
    public static class AbilityScoreGeneration
    {
        // Standard Array: assign these six numbers as you like.
        public static int[] StandardArray() => new int[] { 15, 14, 13, 12, 10, 8 };

        // Point Buy: 27-point budget; each score 8-15 costs a set amount.
        public const int PointBuyBudget = 27;

        public static int PointBuyCost(int score)
        {
            switch (score)
            {
                case 8: return 0;
                case 9: return 1;
                case 10: return 2;
                case 11: return 3;
                case 12: return 4;
                case 13: return 5;
                case 14: return 7;
                case 15: return 9;
                default: return -1; // outside the legal 8-15 range
            }
        }

        // Random: roll 4d6, drop the lowest, six times.
        public static int[] RollSet()
        {
            int[] results = new int[6];
            for (int i = 0; i < 6; i++) results[i] = RollFourDropLowest();
            return results;
        }

        private static int RollFourDropLowest()
        {
            int total = 0, lowest = 7;
            for (int d = 0; d < 4; d++)
            {
                int roll = UnityEngine.Random.Range(1, 7); // 1..6 (upper bound exclusive)
                total += roll;
                if (roll < lowest) lowest = roll;
            }
            return total - lowest;
        }
    }
}