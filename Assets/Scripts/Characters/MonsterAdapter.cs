using DnDTactics.Rules;
using DnDTactics.Data;

namespace DnDTactics.Characters
{
    // Builds a combat-ready Character from a MonsterStats asset. Monsters don't use
    // species/class/background, so we synthesize ability scores from their flat stats.
    public static class MonsterAdapter
    {
        public static Character ToCharacter(MonsterStats stats)
        {
            // Derive a Dex for initiative and an effective stat line. Simple for now.
            var abilities = new AbilityScoreSet();
            abilities.SetBaseScore(Ability.Strength, 12);
            abilities.SetBaseScore(Ability.Dexterity, 12);
            abilities.SetBaseScore(Ability.Constitution, 12);
            abilities.SetBaseScore(Ability.Intelligence, 8);
            abilities.SetBaseScore(Ability.Wisdom, 10);
            abilities.SetBaseScore(Ability.Charisma, 8);

            var c = new Character(stats.monsterName, null, null, null, abilities, 1);
            c.OverrideCombatStats(stats.maxHP, stats.armorClass, stats.speed);
            return c;
        }
    }
}