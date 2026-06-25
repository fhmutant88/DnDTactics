using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DnDTactics.Rules;
using DnDTactics.Characters;

namespace DnDTactics.UI
{
    public class AbilityScorePanel : MonoBehaviour
    {
        [Header("Six value dropdowns, in order: STR, DEX, CON, INT, WIS, CHA")]
        public List<TMP_Dropdown> scoreDropdowns = new List<TMP_Dropdown>();

        [Header("Six labels, same order (optional)")]
        public List<TMP_Text> scoreLabels = new List<TMP_Text>();

        [Header("Controls")]
        public TMP_Dropdown methodDropdown;
        public Button rollButton;
        public TMP_Text statusText;

        public event Action OnChanged;
        public ScoreMethod Method { get; private set; } = ScoreMethod.StandardArray;

        private int[] pool;       // available values for Standard Array / Roll
        private int[] lastRoll;   // remembered roll so toggling methods doesn't lose it

        void Start()
        {
            if (methodDropdown == null || scoreDropdowns.Count < 6)
            {
                Debug.LogError("AbilityScorePanel: assign MethodDropdown and six score dropdowns.");
                return;
            }

            methodDropdown.ClearOptions();
            methodDropdown.AddOptions(new List<string> { "Standard Array", "Point Buy", "Roll 4d6" });
            methodDropdown.onValueChanged.AddListener(_ => OnMethodChanged());

            // Label rows STR..CHA (skipped cleanly if you didn't wire labels).
            Ability[] all = (Ability[])Enum.GetValues(typeof(Ability));
            for (int i = 0; i < scoreLabels.Count && i < all.Length; i++)
                if (scoreLabels[i] != null)
                    scoreLabels[i].text = all[i].ToString().Substring(0, 3).ToUpper();

            foreach (var d in scoreDropdowns)
                if (d != null) d.onValueChanged.AddListener(_ => Raise());

            if (rollButton != null) rollButton.onClick.AddListener(RollPool);

            OnMethodChanged(); // initialize to the default method
        }

        void OnMethodChanged()
        {
            Method = (ScoreMethod)methodDropdown.value;
            if (rollButton != null) rollButton.gameObject.SetActive(Method == ScoreMethod.Roll);

            switch (Method)
            {
                case ScoreMethod.StandardArray:
                    pool = AbilityScoreGeneration.StandardArray();
                    PopulateFromPool();
                    break;
                case ScoreMethod.Roll:
                    if (lastRoll == null) lastRoll = AbilityScoreGeneration.RollSet();
                    pool = lastRoll;
                    PopulateFromPool();
                    break;
                case ScoreMethod.PointBuy:
                    PopulatePointBuy();
                    break;
            }
            Raise();
        }

        void RollPool()
        {
            lastRoll = AbilityScoreGeneration.RollSet();
            pool = lastRoll;
            PopulateFromPool();
            Raise();
        }

        // Array / Roll: each dropdown offers the pool values to assign.
        void PopulateFromPool()
        {
            var labels = new List<string>();
            foreach (int v in pool) labels.Add(v.ToString());
            foreach (var d in scoreDropdowns)
            {
                if (d == null) continue;
                int prev = d.value;
                d.ClearOptions();
                d.AddOptions(labels);
                d.SetValueWithoutNotify(Mathf.Clamp(prev, 0, labels.Count - 1));
            }
        }

        // Point Buy: each dropdown offers 8..15.
        void PopulatePointBuy()
        {
            var labels = new List<string>();
            for (int s = 8; s <= 15; s++) labels.Add(s.ToString());
            foreach (var d in scoreDropdowns)
            {
                if (d == null) continue;
                d.ClearOptions();
                d.AddOptions(labels);
                d.SetValueWithoutNotify(0); // default 8
            }
        }

        public int[] GetScores()
        {
            int[] scores = new int[6];
            for (int i = 0; i < 6; i++)
            {
                var d = scoreDropdowns[i];
                if (d == null || d.options.Count == 0) { scores[i] = 10; continue; }
                int.TryParse(d.options[d.value].text, out scores[i]);
            }
            return scores;
        }

        void Raise()
        {
            UpdateStatus();
            OnChanged?.Invoke();
        }

        void UpdateStatus()
        {
            if (statusText == null) return;
            if (Method == ScoreMethod.PointBuy)
            {
                int spent = 0;
                foreach (int v in GetScores())
                    spent += Mathf.Max(0, AbilityScoreGeneration.PointBuyCost(v));
                int left = AbilityScoreGeneration.PointBuyBudget - spent;
                statusText.text = $"Point Buy — {left} points remaining";
            }
            else
            {
                statusText.text = $"Assign each value once: {string.Join(", ", pool)}";
            }
        }
    }
}