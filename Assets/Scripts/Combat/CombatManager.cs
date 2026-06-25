using System.Collections.Generic;
using UnityEngine;
using DnDTactics.Rules;
using DnDTactics.Data;
using DnDTactics.Characters;

namespace DnDTactics.Combat
{
    // Owns the battle: the grid reference, cell occupancy, and selection.
    // Runs AFTER GridVisualizer so the grid exists when we place combatants.
    [DefaultExecutionOrder(100)]
    public class CombatManager : MonoBehaviour
    {
        [Header("Setup")]
        public GridVisualizer gridVisualizer;
        public float tokenYOffset = 0.9f;

        [Header("Test content (drag assets to spawn combatants)")]
        public Species species;
        public CharacterClass playerClass;
        public CharacterClass enemyClass;
        public Background background;

        [Header("Colors")]
        public Color playerColor = new Color(0.25f, 0.5f, 0.9f);
        public Color enemyColor = new Color(0.85f, 0.3f, 0.3f);

        private TacticalGrid grid;
        private readonly Dictionary<GridCoord, Combatant> occupancy = new();
        private readonly List<Combatant> combatants = new();
        private Combatant selected;

        void Start()
        {
            grid = gridVisualizer != null ? gridVisualizer.Grid : null;
            if (grid == null)
            {
                Debug.LogError("CombatManager: no grid. Assign GridVisualizer in the Inspector.");
                return;
            }

            SpawnCombatant("Hero A", playerClass, Team.Player, new GridCoord(2, 1));
            SpawnCombatant("Hero B", playerClass, Team.Player, new GridCoord(3, 1));
            SpawnCombatant("Enemy A", enemyClass, Team.Enemy, new GridCoord(6, 8));
            SpawnCombatant("Enemy B", enemyClass, Team.Enemy, new GridCoord(7, 8));
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0)) HandleClick();
        }

        void HandleClick()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Combatant clicked = null;
            float nearest = float.MaxValue;
            foreach (var h in Physics.RaycastAll(ray))
            {
                var c = h.collider.GetComponent<Combatant>();
                if (c != null && h.distance < nearest) { clicked = c; nearest = h.distance; }
            }
            Select(clicked);
        }

        void Select(Combatant c)
        {
            if (selected == c) return;
            if (selected != null) selected.SetSelected(false);
            selected = c;
            if (selected != null)
            {
                selected.SetSelected(true);
                Character ch = selected.Character;
                Debug.Log($"Selected {ch.characterName} ({selected.Team}) at {selected.Coord} — " +
                          $"HP {ch.currentHP}/{ch.MaxHP}, AC {ch.ArmorClass}");
            }
        }

        void SpawnCombatant(string name, CharacterClass cls, Team team, GridCoord coord)
        {
            if (!grid.InBounds(coord) || occupancy.ContainsKey(coord))
            {
                Debug.LogWarning($"Can't spawn {name} at {coord} (out of bounds or occupied).");
                return;
            }

            Character character = BuildTestCharacter(name, cls);

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"Combatant_{name}";
            go.transform.SetParent(transform);
            go.transform.localScale = new Vector3(0.7f, 0.8f, 0.7f);

            var combatant = go.AddComponent<Combatant>();
            combatant.Initialize(character, team, coord, team == Team.Player ? playerColor : enemyColor);
            combatant.SetCoord(coord, grid, tokenYOffset);

            occupancy[coord] = combatant;
            combatants.Add(combatant);
        }

        Character BuildTestCharacter(string name, CharacterClass cls)
        {
            var abilities = new AbilityScoreSet();
            int[] array = AbilityScoreGeneration.StandardArray();
            Ability[] all = (Ability[])System.Enum.GetValues(typeof(Ability));
            for (int i = 0; i < all.Length; i++) abilities.SetBaseScore(all[i], array[i]);
            return new Character(name, species, cls, background, abilities, 1);
        }

        // Occupancy queries — movement (next pieces) will lean on these.
        public bool IsOccupied(GridCoord c) => occupancy.ContainsKey(c);
        public Combatant GetOccupant(GridCoord c) => occupancy.TryGetValue(c, out var x) ? x : null;
    }
}