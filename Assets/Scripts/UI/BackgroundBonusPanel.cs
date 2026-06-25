using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DnDTactics.Rules;
using DnDTactics.Data;

namespace DnDTactics.UI
{
    public class BackgroundBonusPanel : MonoBehaviour
    {
        [Header("Three rows, each picks +0/+1/+2 for one ability")]
        public List<TMP_Text> rowLabels = new List<TMP_Text>();      // 3 labels
        public List<TMP_Dropdown> rowDropdowns = new List<TMP_Dropdown>(); // 3 dropdowns
        public TMP_Text statusText;

        public event Action OnChanged;

        private Background current;
        private readonly List<Ability> rowAbilities = new List<Ability>(); // which ability each row maps to

        void Start()
        {
            for (int i = 0; i < rowDropdowns.Count; i++)
            {
                var d = rowDropdowns[i];
                if (d == null) continue;
                d.ClearOptions();
                d.AddOptions(new List<string> { "+0", "+1", "+2" });
                d.onValueChanged.AddListener(_ => Raise());
            }
        }

        // Called by CharacterCreationUI when the chosen background changes.
        public void SetBackground(Background bg)
        {
            current = bg;
            rowAbilities.Clear();

            for (int i = 0; i < 3; i++)
            {
                bool hasOption = bg != null && i < bg.abilityOptions.Count;
                if (hasOption) rowAbilities.Add(bg.abilityOptions[i]);

                if (i < rowLabels.Count && rowLabels[i] != null)
                    rowLabels[i].text = hasOption ? bg.abilityOptions[i].ToString() : "—";

                if (i < rowDropdowns.Count && rowDropdowns[i] != null)
                {
                    rowDropdowns[i].SetValueWithoutNotify(0);     // reset to +0
                    rowDropdowns[i].gameObject.SetActive(hasOption);
                }
            }
            Raise();
        }

        // Returns the chosen bonus per ability, as a length-6 array indexed by (int)Ability.
        public int[] GetBonuses()
        {
            int[] bonuses = new int[6];
            for (int i = 0; i < rowAbilities.Count && i < rowDropdowns.Count; i++)
            {
                var d = rowDropdowns[i];
                if (d == null) continue;
                bonuses[(int)rowAbilities[i]] = d.value; // value 0/1/2 equals the bonus
            }
            return bonuses;
        }

        void Raise()
        {
            if (statusText != null)
            {
                int total = 0;
                foreach (int b in GetBonuses()) total += b;
                statusText.text = $"Background bonus: +{total} of +3 assigned";
            }
            OnChanged?.Invoke();
        }
    }
}