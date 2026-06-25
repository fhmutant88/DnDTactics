using UnityEngine;
using DnDTactics.Rules;

namespace DnDTactics.Data
{
    // A minimal monster definition for encounter building and (later) spawning.
    // Combat-relevant stats can grow; for now this is enough to budget and place.
    [CreateAssetMenu(fileName = "NewMonster", menuName = "DnD/Monster")]
    public class MonsterStats : ScriptableObject
    {
        [Header("Identity")]
        public string monsterName = "New Monster";

        [Header("Challenge")]
        [Tooltip("Challenge Rating. Fractions allowed: 0.125 = CR 1/8, 0.5 = CR 1/2.")]
        public float challengeRating = 1f;

        [Header("Core Combat Stats")]
        public int maxHP = 11;
        public int armorClass = 12;
        public int speed = 30;

        [Tooltip("Flat attack bonus and damage for a basic attack, fleshed out later.")]
        public int attackBonus = 3;
        public int damageDiceCount = 1;
        public int damageDieSides = 6;
        public int damageBonus = 1;
    }
}