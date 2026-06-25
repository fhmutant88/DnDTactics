namespace DnDTactics.Combat
{
    // One combatant's place in the initiative order.
    public class InitiativeEntry
    {
        public Combatant combatant;
        public int roll;          // the d20 result
        public int total;         // roll + Dex modifier
        public int dexModifier;   // kept for tie-breaking

        public InitiativeEntry(Combatant combatant, int roll, int dexModifier)
        {
            this.combatant = combatant;
            this.roll = roll;
            this.dexModifier = dexModifier;
            this.total = roll + dexModifier;
        }
    }
}