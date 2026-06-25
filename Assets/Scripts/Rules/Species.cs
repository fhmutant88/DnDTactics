using System.Collections.Generic;
using UnityEngine;
using DnDTactics.Rules;

namespace DnDTactics.Data
{
    // A 5e species (2024 rules) as a data template.
    // Note: NO ability scores here. In the 2024 ruleset those come from Background.
    [CreateAssetMenu(fileName = "NewSpecies", menuName = "DnD/Species")]
    public class Species : ScriptableObject
    {
        [Header("Identity")]
        public string speciesName = "New Species";

        [TextArea(2, 4)]
        public string description;

        [Header("Physical Traits")]
        public Size size = Size.Medium;

        [Tooltip("Walking speed in feet. Standardized to 30 for most 2024 species.")]
        public int speed = 30;

        [Tooltip("Darkvision range in feet. 0 means none.")]
        public int darkvisionRange = 0;

        [Header("Species Traits")]
        [Tooltip("Trait descriptions for now (e.g. Fey Ancestry). We encode mechanics later.")]
        public List<string> traits = new List<string>();
    }
}