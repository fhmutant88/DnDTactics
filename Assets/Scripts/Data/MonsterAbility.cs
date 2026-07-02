using System;
using UnityEngine;
using DnDTactics.Rules;

namespace DnDTactics.Data
{
    // A monster's save-or-suffer-condition attack rider (Ghoul paralysis, Cockatrice petrify, etc.).
    // Authored per-monster in the Inspector. The shape most signature monsters share.
    [Serializable]
    public class MonsterAbility
    {
        [Tooltip("Condition applied on a FAILED save. Leave as the 'no ability' sentinel to disable.")]
        public bool enabled = false;

        [Tooltip("The condition inflicted on a failed save.")]
        public ConditionTypeData appliesCondition = ConditionTypeData.Paralyzed;

        [Tooltip("Which save the TARGET rolls to resist.")]
        public Ability saveAbility = Ability.Constitution;

        [Tooltip("Save DC.")]
        public int saveDC = 11;

        [Tooltip("How the condition clears once applied.")]
        public ConditionClearData clearRule = ConditionClearData.RepeatingSave;

        [Tooltip("For fixed-duration clears only: number of rounds.")]
        public int durationRounds = 3;
    }

    // Inspector-facing mirrors of the Combat enums, so Data doesn't depend on Combat.
    // (Combat maps these to its ConditionType/ClearRule.)
    public enum ConditionTypeData { Paralyzed, Petrified, Prone, Stunned, Restrained, Blinded, Frightened, Poisoned }
    public enum ConditionClearData { RepeatingSave, DurationRounds, UntilRemoved }
}