using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DnDTactics.Rules;
using DnDTactics.Data;
using DnDTactics.Characters;

namespace DnDTactics.UI
{
    public class CharacterCreationUI : MonoBehaviour
    {
        [Header("Content (drag your data assets here)")]
        public List<Species> speciesOptions = new List<Species>();
        public List<CharacterClass> classOptions = new List<CharacterClass>();
        public List<Background> backgroundOptions = new List<Background>();

        [Header("Ability Scores")]
        public AbilityScorePanel scorePanel;
        public BackgroundBonusPanel bonusPanel;

        [Header("UI References")]
        public TMP_InputField nameInput;
        public TMP_Dropdown speciesDropdown;
        public TMP_Dropdown classDropdown;
        public TMP_Dropdown backgroundDropdown;
        public Slider levelSlider;
        public TMP_Text levelLabel;     // optional
        public Button createButton;
        public TMP_Text outputText;

        void Start()
        {
            if (MissingRefs()) return;

            PopulateDropdown(speciesDropdown, speciesOptions.ConvertAll(s => s ? s.speciesName : "—"));
            PopulateDropdown(classDropdown, classOptions.ConvertAll(c => c ? c.className : "—"));
            PopulateDropdown(backgroundDropdown, backgroundOptions.ConvertAll(b => b ? b.backgroundName : "—"));

            levelSlider.wholeNumbers = true;
            levelSlider.minValue = Progression.MinLevel;
            levelSlider.maxValue = Progression.MaxLevel;
            levelSlider.value = 1;

            // AddListener is the code equivalent of the Inspector's "On Click ()" box:
            // it says "when this changes, run this method." The _ discards the passed value.
            nameInput.onValueChanged.AddListener(_ => Refresh());
            speciesDropdown.onValueChanged.AddListener(_ => Refresh());
            classDropdown.onValueChanged.AddListener(_ => Refresh());
            backgroundDropdown.onValueChanged.AddListener(_ => Refresh());
            levelSlider.onValueChanged.AddListener(_ => Refresh());
            createButton.onClick.AddListener(OnCreateClicked); 
            
            if (scorePanel != null) scorePanel.OnChanged += Refresh;
            if (bonusPanel != null)
            {
                bonusPanel.OnChanged += Refresh;
                // keep the bonus panel in sync when the background dropdown changes
                backgroundDropdown.onValueChanged.AddListener(_ => SyncBonusPanel());
                SyncBonusPanel(); // initialize for the starting selection
            }

            Refresh();
        }

        void PopulateDropdown(TMP_Dropdown dropdown, List<string> labels)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(labels);
        }

        // Read the current UI state into a fresh builder.
        CharacterBuilder ReadBuilder()
        {
            var b = new CharacterBuilder
            {
                characterName = nameInput.text,
                startingLevel = (int)levelSlider.value
            };

            if (speciesOptions.Count > 0) b.species = speciesOptions[speciesDropdown.value];
            if (classOptions.Count > 0) b.characterClass = classOptions[classDropdown.value];
            if (backgroundOptions.Count > 0) b.background = backgroundOptions[backgroundDropdown.value];

            // Base scores now come from the interactive panel (falls back to
            // Standard Array if the panel isn't wired yet).
            if (scorePanel != null)
            {
                b.method = scorePanel.Method;
                int[] s = scorePanel.GetScores();
                for (int i = 0; i < 6; i++) b.baseScores[i] = s[i];
            }
            else
            {
                b.UseStandardArrayInOrder();
            }
            if (bonusPanel != null)
            {
                int[] bonuses = bonusPanel.GetBonuses();
                for (int i = 0; i < 6; i++) b.backgroundBonuses[i] = bonuses[i];
            }
            else if (b.background != null && b.background.abilityOptions.Count >= 2)
            {
                b.SetBackgroundBonus(b.background.abilityOptions[0], 2);
                b.SetBackgroundBonus(b.background.abilityOptions[1], 1);
            }
            return b;
        }

        void SyncBonusPanel()
        {
            if (bonusPanel == null) return;
            Background bg = backgroundOptions.Count > 0
                ? backgroundOptions[backgroundDropdown.value]
                : null;
            bonusPanel.SetBackground(bg);
        }

        // Live validation: drives the level label, the message area, and the button.
        void Refresh()
        {
            if (levelLabel != null) levelLabel.text = $"Level {(int)levelSlider.value}";

            var problems = ReadBuilder().Validate();
            if (problems.Count == 0)
            {
                outputText.text = "Ready to create.";
                createButton.interactable = true;
            }
            else
            {
                outputText.text = "Fix the following:\n- " + string.Join("\n- ", problems);
                createButton.interactable = false;
            }
        }

        void OnCreateClicked()
        {
            var b = ReadBuilder();
            if (!b.IsComplete()) { Refresh(); return; }

            Character c = b.Build();
            string sheet =
                $"=== {c.characterName} ===\n" +
                $"Level {c.level} {c.species.speciesName} {c.characterClass.className} ({c.background.backgroundName})\n" +
                $"HP {c.currentHP}/{c.MaxHP}   AC {c.ArmorClass}   Prof +{c.ProficiencyBonus}\n" +
                $"STR {Sc(c, Ability.Strength)}  DEX {Sc(c, Ability.Dexterity)}  CON {Sc(c, Ability.Constitution)}\n" +
                $"INT {Sc(c, Ability.Intelligence)}  WIS {Sc(c, Ability.Wisdom)}  CHA {Sc(c, Ability.Charisma)}";
            outputText.text = sheet;
            Debug.Log(sheet);
        }

        string Sc(Character c, Ability a)
        {
            int mod = c.AbilityModifier(a);
            return $"{c.abilities.GetFinalScore(a)} ({(mod >= 0 ? "+" : "")}{mod})";
        }

        bool MissingRefs()
        {
            var missing = new List<string>();
            if (nameInput == null) missing.Add(nameof(nameInput));
            if (speciesDropdown == null) missing.Add(nameof(speciesDropdown));
            if (classDropdown == null) missing.Add(nameof(classDropdown));
            if (backgroundDropdown == null) missing.Add(nameof(backgroundDropdown));
            if (levelSlider == null) missing.Add(nameof(levelSlider));
            if (createButton == null) missing.Add(nameof(createButton));
            if (outputText == null) missing.Add(nameof(outputText));
            if (missing.Count > 0)
                Debug.LogError("CharacterCreationUI: unassigned reference(s) -> " + string.Join(", ", missing));
            return missing.Count > 0;
        }
    }
}