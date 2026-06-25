namespace DnDTactics.Rules
{
    // Central dice roller so every system rolls consistently.
    public static class Dice
    {
        // Roll one die with the given number of sides (e.g. Roll(20) = d20).
        public static int Roll(int sides) => UnityEngine.Random.Range(1, sides + 1);

        // Roll several dice of the same size and sum them (e.g. RollMany(2, 6) = 2d6).
        public static int RollMany(int count, int sides)
        {
            int total = 0;
            for (int i = 0; i < count; i++) total += Roll(sides);
            return total;
        }
    }
}