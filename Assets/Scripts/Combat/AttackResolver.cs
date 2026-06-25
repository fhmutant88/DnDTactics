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
        public int attackRoll;     // the raw d20
        public int attackTotal;    // d20 + mods
        public int targetAC;
        public int damage;         // 0 on a miss
    }

    public static class AttackResolver
    {
        // Resolves a weapon attack from attacker against target.
        public static AttackResult Resolve(Character attacker, Character target, Weapon weapon)
        {
            var r = new AttackResult();

            Ability atkAbility = weapon.usesDexterity ? Ability.Dexterity : Ability.Strength;
            int abilityMod = attacker.AbilityModifier(atkAbility);
            int profBonus = attacker.ProficiencyBonus;

            r.attackRoll = Dice.Roll(20);
            r.targetAC = target.ArmorClass;

            // Natural 1 always misses; natural 20 always hits and crits.
            if (r.attackRoll == 1) { r.critMiss = true; r.hit = false; return r; }
            if (r.attackRoll == 20) r.crit = true;

            r.attackTotal = r.attackRoll + abilityMod + profBonus;
            r.hit = r.crit || r.attackTotal >= r.targetAC;
            if (!r.hit) return r;

            // Damage: weapon dice + ability modifier. A crit doubles the DICE only.
            int diceCount = r.crit ? weapon.damageDiceCount * 2 : weapon.damageDiceCount;
            int damage = Dice.RollMany(diceCount, weapon.damageDieSides) + abilityMod;
            r.damage = damage < 1 ? 1 : damage; // a hit deals at least 1
            return r;
        }
    }
}