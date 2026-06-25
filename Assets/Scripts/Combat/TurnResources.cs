namespace DnDTactics.Combat
{
    // What the active combatant has left to spend this turn. Reset at the start of each turn.
    public class TurnResources
    {
        public int MovementRemaining;  // in feet
        public bool ActionAvailable;
        public bool BonusActionAvailable;
        public bool ReactionAvailable;  // reactions reset differently later; fine for now

        public void ResetForTurn(int speedFeet)
        {
            MovementRemaining = speedFeet;
            ActionAvailable = true;
            BonusActionAvailable = true;
            ReactionAvailable = true;
        }

        public bool CanSpendMovement(int feet) => feet <= MovementRemaining;
        public void SpendMovement(int feet) => MovementRemaining -= feet;
    }
}