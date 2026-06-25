using System.Collections.Generic;
using UnityEngine;
using DnDTactics.Rules;

namespace DnDTactics.Data
{
    // A 5e background (2024 rules). This is the source of ability score increases.
    [CreateAssetMenu(fileName = "NewBackground", menuName = "DnD/Background")]
    public class Background : ScriptableObject
    {
        [Header("Identity")]
        public string backgroundName = "New Background";

        [TextArea(2, 4)]
        public string description;

        [Header("Ability Score Options")]
        [Tooltip("The three abilities this background can boost. At creation the player puts " +
                 "+2/+1 into two of them, or +1 into all three.")]
        public List<Ability> abilityOptions = new List<Ability>();

        [Header("Other Benefits (text for now)")]
        public string originFeat;
        public List<string> skillProficiencies = new List<string>();
        public string toolProficiency;
    }
}