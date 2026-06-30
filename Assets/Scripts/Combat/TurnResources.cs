namespace DnDTactics.Combat
{
    // What the ACTIVE combatant has left to spend on ITS OWN turn.
    // Reset at the start of each turn by CombatManager.BeginTurn.
    // NOTE: Reaction is NOT here — it lives on Combatant, because a reaction is
    // spent during OTHER creatures' turns, not the active creature's turn.
    public class TurnResources
    {
        public int MovementRemaining;          // in feet
        public bool ActionAvailable;
        public bool BonusActionAvailable;
        public bool FreeInteractionAvailable;  // one free object interaction per turn (5e)

        public void ResetForTurn(int speedFeet)
        {
            MovementRemaining = speedFeet;
            ActionAvailable = true;
            BonusActionAvailable = true;
            FreeInteractionAvailable = true;
        }

        // --- Movement (unchanged check-then-spend; TryMoveTo already guards reachability) ---
        public bool CanSpendMovement(int feet) => feet <= MovementRemaining;
        public void SpendMovement(int feet) => MovementRemaining -= feet;

        // --- Action ---
        public bool TrySpendAction()
        {
            if (!ActionAvailable) return false;
            ActionAvailable = false;
            return true;
        }

        // --- Bonus action ---
        public bool TrySpendBonusAction()
        {
            if (!BonusActionAvailable) return false;
            BonusActionAvailable = false;
            return true;
        }

        // --- Free object interaction (draw a weapon, open an unlocked door, etc.) ---
        public bool TrySpendFreeInteraction()
        {
            if (!FreeInteractionAvailable) return false;
            FreeInteractionAvailable = false;
            return true;
        }
    }
}