using System;
using DnDTactics.Rules;
using DnDTactics.Data;

namespace DnDTactics.Characters
{
    // A living, per-instance character. Plain serializable class (NOT a ScriptableObject):
    // it references shared templates but owns its own mutable state.
    [Serializable]
    public class Character
    {
        // Identity
        public string characterName;

        // Shared template references
        public Species species;
        public CharacterClass characterClass;
        public Background background;

        // Personal state
        public AbilityScoreSet abilities;
        public int level;
        public int currentXp;
        public int currentHP;

        public Character(string name, Species species, CharacterClass characterClass,
                         Background background, AbilityScoreSet abilities, int startingLevel)
        {
            characterName = name;
            this.species = species;
            this.characterClass = characterClass;
            this.background = background;
            this.abilities = abilities;

            level = Math.Clamp(startingLevel, Progression.MinLevel, Progression.MaxLevel);
            currentXp = Progression.XpForLevel(level); // start at the floor for that level
            currentHP = MaxHP;                         // start at full health
        }

        // ---- Derived stats ----

        public int ProficiencyBonus => Progression.ProficiencyBonus(level);
        public int InitiativeModifier => abilities.GetModifier(Ability.Dexterity);

        // Unarmored placeholder. Real AC comes once we have armor/equipment.
        public int ArmorClass => 10 + abilities.GetModifier(Ability.Dexterity);

        public int AbilityModifier(Ability ability) => abilities.GetModifier(ability);

        // Max HP for the current level: level 1 = max hit die + Con mod;
        // each later level = average roll (die/2 + 1) + Con mod. Minimum 1 per level.
        public int MaxHP
        {
            get
            {
                int conMod = abilities.GetModifier(Ability.Constitution);
                int die = characterClass != null ? characterClass.hitDie : 8;

                int hp = Math.Max(1, die + conMod);
                int perLevel = die / 2 + 1;
                for (int l = 2; l <= level; l++)
                    hp += Math.Max(1, perLevel + conMod);
                return hp;
            }
        }

        // ---- XP / leveling ----

        // Adds XP and applies any level-ups. Returns true if at least one level was gained.
        public bool AddXp(int amount)
        {
            if (amount <= 0) return false;

            int oldLevel = level;
            int oldMaxHP = MaxHP;

            currentXp += amount;
            level = Progression.LevelForXp(currentXp);

            if (level > oldLevel)
            {
                currentHP += MaxHP - oldMaxHP; // gain the new HP from leveling
                return true;
            }
            return false;
        }
    }
}