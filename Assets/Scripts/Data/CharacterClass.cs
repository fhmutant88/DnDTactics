using UnityEngine;
using DnDTactics.Rules;   // pulls in the Ability enum you already built

namespace DnDTactics.Data
{
    // A 5e character class (Fighter, Wizard, etc.) as a data template.
    // This describes a class in the abstract; it is NOT a living character.
    [CreateAssetMenu(fileName = "NewClass", menuName = "DnD/Character Class")]
    public class CharacterClass : ScriptableObject
    {
        [Header("Identity")]
        public string className = "New Class";

        [TextArea(2, 4)]
        public string description;

        [Header("Core Mechanics")]
        [Tooltip("Sides on the class hit die. Fighter = 10, Wizard = 6.")]
        public int hitDie = 8;

        [Tooltip("The class's main ability score.")]
        public Ability primaryAbility = Ability.Strength;

        [Header("Saving Throw Proficiencies")]
        [Tooltip("Every 5e class is proficient in exactly two saving throws.")]
        public Ability savingThrow1 = Ability.Strength;
        public Ability savingThrow2 = Ability.Constitution;
    }
}