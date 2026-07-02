using System.Collections.Generic;
using DnDTactics.Rules;

namespace DnDTactics.Combat

{
    // The 5e conditions. Add entries as each is built; the framework doesn't change.
    public enum ConditionType
    {
        Prone,
        Paralyzed,
        Petrified,
        Grappled,
        Restrained,
        Incapacitated,
        Poisoned,
        Unconscious,
        Blinded,
        Frightened,
        Stunned,
    }

    // How an active condition ends.
    public enum ClearRule
    {
        UntilRemoved,     // -1 duration: explicit removal only (Prone until you stand)
        DurationRounds,   // ticks down RoundsRemaining; expires at 0
        RepeatingSave,    // re-roll a save at end of the afflicted creature's turn; success ends it
    }

    // One active condition on a combatant. Carries its clear-rule so different conditions can end
    // different ways (fixed duration, explicit removal, or a repeating save — e.g. Ghoul paralysis).
    public class ActiveCondition
    {
        public ConditionType Type { get; }
        public ClearRule Clear { get; }
        public int RoundsRemaining { get; private set; } // used by DurationRounds
        public string Source { get; }

        // Repeating-save parameters (used by ClearRule.RepeatingSave).
        public Ability SaveAbility { get; }
        public int SaveDC { get; }

        public ActiveCondition(ConditionType type, ClearRule clear = ClearRule.UntilRemoved,
                               int rounds = -1, string source = null,
                               Ability saveAbility = Ability.Constitution, int saveDC = 10)
        {
            Type = type;
            Clear = clear;
            RoundsRemaining = rounds;
            Source = source;
            SaveAbility = saveAbility;
            SaveDC = saveDC;
        }

        public bool IsExpired => Clear == ClearRule.DurationRounds && RoundsRemaining == 0;
        public void Tick() { if (Clear == ClearRule.DurationRounds && RoundsRemaining > 0) RoundsRemaining--; }
    }
  
}