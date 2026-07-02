namespace DnDTactics.Combat
{
    // Per-combatant death-save tracking (combat-only). 5e: while at 0 HP you roll each turn —
    // 3 successes = stabilized, 3 failures = dead. Nat 20 = up at 1 HP; nat 1 = two failures.
    // Damage while dying = a failure (crit = two). Healing = up immediately.
    // Resolves to barracks Down (stable/still-dying) or Dead (3 failures) at encounter end.
    public class DeathSaves
    {
        public int Successes { get; private set; }
        public int Failures { get; private set; }
        public bool IsDying { get; private set; }    // at 0 HP, still rolling
        public bool IsStable { get; private set; }   // 3 successes — stops rolling, stays down
        public bool IsDead { get; private set; }     // 3 failures — dead

        // Not acting this turn: dying (rolls, doesn't act), stable (unconscious), or dead.
        public bool IsOutOfFight => IsDying || IsStable || IsDead;

        public void BeginDying()
        {
            IsDying = true; IsStable = false; IsDead = false;
            Successes = 0; Failures = 0;
        }

        public void AddSuccess(int n = 1)
        {
            if (!IsDying) return;
            Successes += n;
            if (Successes >= 3) { IsStable = true; IsDying = false; }
        }

        public void AddFailure(int n = 1)
        {
            if (!IsDying) return;
            Failures += n;
            if (Failures >= 3) { IsDead = true; IsDying = false; }
        }

        // Healing or a nat-20: revived to consciousness; tracking cleared.
        public void Revive()
        {
            IsDying = false; IsStable = false; IsDead = false;
            Successes = 0; Failures = 0;
        }
    }
}