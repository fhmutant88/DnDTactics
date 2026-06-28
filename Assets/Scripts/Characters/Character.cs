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
        public int Speed => speedOverride ?? (species != null ? species.speed : 30);
        public int InitiativeModifier => abilities.GetModifier(Ability.Dexterity);

        // Unarmored placeholder. Real AC comes once we have armor/equipment.
        public int ArmorClass => acOverride ?? (10 + abilities.GetModifier(Ability.Dexterity));

        public int AbilityModifier(Ability ability) => abilities.GetModifier(ability);

        // Max HP for the current level: level 1 = max hit die + Con mod;
        // each later level = average roll (die/2 + 1) + Con mod. Minimum 1 per level.
        public int MaxHP
            
        {
            get
            {
                if (hpOverride.HasValue) return hpOverride.Value;

                int conMod = abilities.GetModifier(Ability.Constitution);
                int die = characterClass != null ? characterClass.hitDie : 8;

                int hp = Math.Max(1, die + conMod);
                int perLevel = die / 2 + 1;
                for (int l = 2; l <= level; l++)
                    hp += Math.Max(1, perLevel + conMod);
                return hp;
            }
        }

        // ---- Health / damage ----

        public bool IsDown { get; private set; }   // dropped to 0 HP

        // Returns the actual damage applied (clamped so HP never goes below 0).
        public int TakeDamage(int amount)
        {
            if (amount <= 0) return 0;
            int before = currentHP;
            currentHP = System.Math.Max(0, currentHP - amount);
            if (currentHP == 0) IsDown = true;
            return before - currentHP;
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || IsDown) return; // revival is a separate system (Milestone 6)
            currentHP = System.Math.Min(MaxHP, currentHP + amount);
        }

        // Restore exact saved state after construction (used by save/load).
        public void RestoreState(int xp, int hp, bool down)
        {
            currentXp = xp;
            currentHP = hp;
            IsDown = down;
        }

        // Bring a downed character back to life at the given HP. Clears the down flag.
        public void Revive(int hp)
        {
            currentHP = System.Math.Max(1, hp);
            IsDown = false;
        }

        // Raise Dead penalty: counts down 1 per long rest, 4 rests to clear.
        // (The actual -4 to d20 rolls gets wired into combat math later.)
        public int raiseDeadPenalty;   // 0 = none

        public void ApplyRaiseDeadPenalty() => raiseDeadPenalty = 4;

        // Called on each long rest; reduces the penalty toward 0.
        public void TickLongRestRecovery()
        {
            if (raiseDeadPenalty > 0) raiseDeadPenalty--;
        }

        public bool HasRaiseDeadPenalty => raiseDeadPenalty > 0;

        // Monster support: override derived stats with flat values from MonsterStats.
        private int? hpOverride, acOverride, speedOverride;

        public void OverrideCombatStats(int hp, int ac, int speed)
        {
            hpOverride = hp; acOverride = ac; speedOverride = speed;
            currentHP = hp;
        }

        // ---- XP / leveling (deferred: earn now, level up on a long rest) ----

        // Accrue XP. Does NOT change level — leveling applies on a long rest.
        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            currentXp += amount;
        }

        // The level this character's XP qualifies them for (may exceed current level).
        public int QualifiedLevel => Progression.LevelForXp(currentXp);

        // True if enough XP earned to level up (pending).
        public bool LevelUpPending => QualifiedLevel > level;

        // Apply pending level-up (called on a long rest). Returns levels gained (0 if none).
        public int ApplyPendingLevelUp()
        {
            int qualified = QualifiedLevel;
            if (qualified <= level) return 0;
            int oldLevel = level;
            int oldMaxHP = MaxHP;
            level = qualified;
            currentHP += MaxHP - oldMaxHP;
            return level - oldLevel;
        }
    }
}