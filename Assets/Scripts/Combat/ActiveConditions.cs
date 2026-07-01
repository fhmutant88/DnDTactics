using System.Collections.Generic;

namespace DnDTactics.Combat
{
    // The 5e conditions. Add entries as each is built; the framework doesn't change.
    public enum ConditionType
    {
        Prone,
        Paralyzed,
        Grappled,
        Restrained,
        Incapacitated,
        Poisoned,
        Unconscious,
        Blinded,
        Frightened,
        Stunned,
    }

    // One active condition on a combatant. Carries optional duration so durationed
    // conditions (Poisoned for N rounds) work later for free; Prone uses -1 = until
    // explicitly removed (you stay prone until you stand up).
    public class ActiveCondition
    {
        public ConditionType Type { get; }
        public int RoundsRemaining { get; private set; } // -1 = until removed
        public string Source { get; }                    // what applied it (log / clear rules)

        public ActiveCondition(ConditionType type, int rounds = -1, string source = null)
        {
            Type = type;
            RoundsRemaining = rounds;
            Source = source;
        }

        public bool IsExpired => RoundsRemaining == 0;
        public void Tick() { if (RoundsRemaining > 0) RoundsRemaining--; }
    }
}