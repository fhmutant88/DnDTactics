using UnityEngine;
using DnDTactics.Rules;

namespace DnDTactics.Data
{
    public enum DamageType { Bludgeoning, Piercing, Slashing }

    // A minimal weapon for now: enough to make an attack roll and roll damage.
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "DnD/Weapon")]
    public class Weapon : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName = "New Weapon";

        [Header("Reach & Range")]
        [Tooltip("Reach/range in feet. Melee is typically 5.")]
        public int rangeFeet = 5;

        [Tooltip("If true, this attack uses Dexterity instead of Strength.")]
        public bool usesDexterity = false;

        [Header("Damage")]
        [Tooltip("Number of damage dice, e.g. 1 for 1d8.")]
        public int damageDiceCount = 1;

        [Tooltip("Sides on each damage die, e.g. 8 for 1d8.")]
        public int damageDieSides = 8;

        public DamageType damageType = DamageType.Slashing;
    }
}