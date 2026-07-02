using DnDTactics.Rules;
using DnDTactics.Characters;

namespace DnDTactics.Combat
{
    // The outcome of one saving throw, with detail for a transparent log.
    public struct SaveResult
    {
        public bool success;
        public bool autoFailed;    // a condition (e.g. Paralyzed) forced failure, no roll
        public bool autoSucceeded; // reserved for future forced-success effects
        public int roll;           // the d20 kept (0 if auto-resolved)
        public int total;          // roll + ability mod + proficiency
        public int dc;
        public Ability ability;
        public RollMode rollMode;
        public int otherRoll;      // discarded die on adv/disadv (else 0)
    }

    // Rolls saving throws vs. a DC. Sibling to AttackResolver: same d20 + mods vs. target-number
    // shape, but the DEFENDER rolls and the target is a DC. Reuses AttackContext roll-modes for
    // advantage/disadvantage on saves. Checks defender conditions for auto-fail BEFORE rolling.
    public static class SaveResolver
    {
        public static SaveResult Resolve(Combatant defender, Ability ability, int dc,
                                         AttackContext context = null)
        {
            var r = new SaveResult { dc = dc, ability = ability };

            // --- Auto-fail from conditions (checked before any roll). ---
            // Paralyzed / Stunned / Unconscious auto-fail STR and DEX saves (5e).
            if ((ability == Ability.Strength || ability == Ability.Dexterity) &&
                DefenderAutoFailsStrDex(defender))
            {
                r.autoFailed = true;
                r.success = false;
                return r;
            }

            Character c = defender.Character;
            int abilityMod = c.AbilityModifier(ability);
            int prof = c.IsProficientInSave(ability) ? c.ProficiencyBonus : 0;

            // --- Roll (respecting adv/disadv roll-mode if a context was supplied). ---
            RollMode mode = context != null ? context.NetRollMode : RollMode.Flat;
            r.rollMode = mode;
            if (mode == RollMode.Flat)
            {
                r.roll = Dice.Roll(20);
                r.otherRoll = 0;
            }
            else
            {
                int a = Dice.Roll(20), b = Dice.Roll(20);
                if (mode == RollMode.Advantage) { r.roll = a > b ? a : b; r.otherRoll = a > b ? b : a; }
                else { r.roll = a < b ? a : b; r.otherRoll = a < b ? b : a; }
            }

            r.total = r.roll + abilityMod + prof;
            r.success = r.total >= dc;
            return r;
        }

        // 5e: Paralyzed, Stunned, and Unconscious creatures auto-fail Strength and Dexterity saves.
        static bool DefenderAutoFailsStrDex(Combatant d) =>
            d.HasCondition(ConditionType.Paralyzed) ||
            d.HasCondition(ConditionType.Stunned) ||
            d.HasCondition(ConditionType.Unconscious);
    }
}