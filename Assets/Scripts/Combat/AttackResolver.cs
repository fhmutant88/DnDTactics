using DnDTactics.Rules;
using DnDTactics.Characters;

namespace DnDTactics.Combat
{
    // The outcome of one attack, so the caller can log/animate it.
    public struct AttackResult
    {
        public bool hit;
        public bool crit;
        public int attackRoll;     // the raw d20
        public int attackTotal;    // d20 + mods
        public int targetAC;
        public int damage;
        public string summary;
    }

    // Resolves a single basic melee attack per 5e rules. No Unity types.
    public static class AttackResolver
    {
        // A simple melee attack: STR to hit and damage, 1d8 weapon die.
        // Real weapons (finesse, ranged, damage types) arrive with equipment.
        public static AttackResult ResolveMeleeAttack(Character attacker, Character target,
                                                      int weaponDie = 8)
        {
            var r = new AttackResult();

            int strMod = attacker.AbilityModifier(Ability.Strength);
            int prof = attacker.ProficiencyBonus;

            int d20 = Dice.Roll(20);
            r.attackRoll = d20;
            r.targetAC = target.ArmorClass;

            bool natOne = d20 == 1;
            bool natTwenty = d20 == 20;
            r.attackTotal = d20 + strMod + prof;

            // Nat 20 always hits and crits; nat 1 always misses; else compare to AC.
            r.crit = natTwenty;
            r.hit = !natOne && (natTwenty || r.attackTotal >= r.targetAC);

            if (r.hit)
            {
                // Crit doubles the weapon dice (not the modifier).
                int diceCount = r.crit ? 2 : 1;
                int weaponDamage = Dice.RollMany(diceCount, weaponDie);
                r.damage = System.Math.Max(1, weaponDamage + strMod); // min 1 on a hit
            }

            r.summary = BuildSummary(attacker, target, r, strMod, prof);
            return r;
        }

        private static string BuildSummary(Character a, Character t, AttackResult r, int strMod, int prof)
        {
            string roll = $"d20({r.attackRoll}){Signed(strMod)}{Signed(prof)} = {r.attackTotal} vs AC {r.targetAC}";
            if (!r.hit) return $"{a.characterName} attacks {t.characterName}: {roll} — MISS"
                               + (r.attackRoll == 1 ? " (nat 1)" : "");
            string critTxt = r.crit ? " CRITICAL HIT!" : "";
            return $"{a.characterName} attacks {t.characterName}: {roll} — HIT{critTxt} for {r.damage} damage";
        }

        private static string Signed(int n) => (n >= 0 ? "+" : "") + n;
    }
}