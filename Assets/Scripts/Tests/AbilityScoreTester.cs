using UnityEngine;
using DnDTactics.Rules;
using DnDTactics.Data;

namespace DnDTactics.Tests
{
    // Throwaway harness: watch the ability-score system run in Play mode.
    public class AbilityScoreTester : MonoBehaviour
    {
        public Background background; // drag a Background asset here in the Inspector

        void Start()
        {
            var scores = new AbilityScoreSet();

            // Assign the Standard Array in ability order for this test.
            int[] array = AbilityScoreGeneration.StandardArray();
            Ability[] all = (Ability[])System.Enum.GetValues(typeof(Ability));
            for (int i = 0; i < all.Length; i++)
                scores.SetBaseScore(all[i], array[i]);

            // Apply the background: +2 to its first option, +1 to its second.
            if (background != null && background.abilityOptions.Count >= 2)
            {
                scores.AddBackgroundBonus(background.abilityOptions[0], 2);
                scores.AddBackgroundBonus(background.abilityOptions[1], 1);
            }

            string label = background != null ? background.backgroundName : "none";
            Debug.Log($"Ability scores (Standard Array + {label}):");
            foreach (Ability a in all)
            {
                int mod = scores.GetModifier(a);
                string sign = mod >= 0 ? "+" : "";
                Debug.Log($"  {a}: {scores.GetFinalScore(a)} ({sign}{mod})");
            }
        }
    }
}