using UnityEngine;
using DnDTactics.Rules;

namespace DnDTactics.Data
{
    [CreateAssetMenu(fileName = "NewMonster", menuName = "DnD/Monster")]
    public class MonsterStats : ScriptableObject
    {
        [Header("Identity")]
        public string monsterName = "New Monster";

        [Header("Challenge")]
        [Tooltip("Challenge Rating. Fractions allowed: 0.125 = CR 1/8, 0.5 = CR 1/2.")]
        public float challengeRating = 1f;

        [Tooltip("XP this monster is worth (defeated = reward; also encounter-budget cost). " +
                 "0 = derive from CR automatically.")]
        public int xpValue = 0;

        [Header("Core Combat Stats")]
        public int maxHP = 11;
        public int armorClass = 12;
        public int speed = 30;

        [Tooltip("Flat attack bonus and damage for a basic attack, fleshed out later.")]
        public int attackBonus = 3;
        public int damageDiceCount = 1;
        public int damageDieSides = 6;
        public int damageBonus = 1;

        [Header("Signature Ability (optional — leave 'applies' as None for plain attackers)")]
        [Tooltip("On a successful hit, target makes a save or suffers a condition. None = no ability.")]
        public MonsterAbility onHitAbility;

        // XP to use: explicit xpValue if set, else derived from CR.
        public int XpReward => xpValue > 0 ? xpValue : XpForCR(challengeRating);

        public static int XpForCR(float cr)
        {
            if (cr <= 0f) return 10;
            if (cr <= 0.125f) return 25;
            if (cr <= 0.25f) return 50;
            if (cr <= 0.5f) return 100;
            if (cr <= 1f) return 200;
            if (cr <= 2f) return 450;
            if (cr <= 3f) return 700;
            if (cr <= 4f) return 1100;
            if (cr <= 5f) return 1800;
            if (cr <= 6f) return 2300;
            if (cr <= 7f) return 2900;
            if (cr <= 8f) return 3900;
            if (cr <= 9f) return 5000;
            if (cr <= 10f) return 5900;
            if (cr <= 11f) return 7200;
            if (cr <= 12f) return 8400;
            if (cr <= 13f) return 10000;
            if (cr <= 14f) return 11500;
            if (cr <= 15f) return 13000;
            if (cr <= 16f) return 15000;
            if (cr <= 17f) return 18000;
            if (cr <= 18f) return 20000;
            if (cr <= 19f) return 22000;
            if (cr <= 20f) return 25000;
            if (cr <= 24f) return 62000;
            if (cr <= 30f) return 155000;
            return 25000;
        }
    }
}