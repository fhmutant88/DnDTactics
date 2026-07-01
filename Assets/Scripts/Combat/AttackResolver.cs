using DnDTactics.Rules;
using DnDTactics.Data;
using DnDTactics.Characters;

namespace DnDTactics.Combat
{
    // The outcome of one attack, with the details so we can log it clearly.
    public struct AttackResult
    {
        public bool hit;
        public bool crit;
        public bool critMiss;
        public int attackRoll;     // the raw d20 (the one KEPT, after adv/disadv)
        public int attackTotal;    // d20 + mods
        public int targetAC;
        public int damage;         // 0 on a miss
        public RollMode rollMode;  // how the d20 was rolled (for the log)
        public int otherRoll;      // the discarded d20 when adv/disadv (else 0)
    }

    public static class AttackResolver
    {
        // Resolves a weapon attack. The optional context carries advantage/disadvantage
        // sources; null means a flat roll (so all existing callers still work unchanged).
        public static AttackResult Resolve(Character attacker, Character target, Weapon weapon,
                                           AttackContext context = null)
        {
            var r = new AttackResult();

            Ability atkAbility = weapon.usesDexterity ? Ability.Dexterity : Ability.Strength;
            int abilityMod = attacker.AbilityModifier(atkAbility);
            int profBonus = attacker.ProficiencyBonus;

            // --- The adv/disadvantage roll: this is the whole of phase 2's rules change. ---
            RollMode mode = context != null ? context.NetRollMode : RollMode.Flat;
            r.rollMode = mode;

            if (mode == RollMode.Flat)
            {
                r.attackRoll = Dice.Roll(20);
                r.otherRoll = 0;
            }
            else
            {
                int a = Dice.Roll(20);
                int b = Dice.Roll(20);
                if (mode == RollMode.Advantage) { r.attackRoll = a > b ? a : b; r.otherRoll = a > b ? b : a; }
                else { r.attackRoll = a < b ? a : b; r.otherRoll = a < b ? b : a; }
            }

            r.targetAC = target.ArmorClass;

            // Natural 1 always misses; natural 20 always hits and crits. (Uses the KEPT die.)
            if (r.attackRoll == 1) { r.critMiss = true; r.hit = false; return r; }
            if (r.attackRoll == 20) r.crit = true;

            r.attackTotal = r.attackRoll + abilityMod + profBonus;
            r.hit = r.crit || r.attackTotal >= r.targetAC;
            if (!r.hit) return r;

            // Forced crit (e.g. melee vs. paralyzed): a hit becomes a crit regardless of the die.
            // Applied only on a hit — paralysis auto-crits hits, it doesn't auto-hit.
            if (context != null && context.HasForcedCrit)
                r.crit = true;

            // Damage: weapon dice + ability modifier. A crit doubles the DICE only.
            int diceCount = r.crit ? weapon.damageDiceCount * 2 : weapon.damageDiceCount;
            int damage = Dice.RollMany(diceCount, weapon.damageDieSides) + abilityMod;
            r.damage = damage < 1 ? 1 : damage; // a hit deals at least 1
            return r;
        }
    }
}