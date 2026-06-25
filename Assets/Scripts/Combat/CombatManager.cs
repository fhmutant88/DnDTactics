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
        private TurnOrder turnOrder = new TurnOrder();
       
        [Header("Movement")]
        public CellHighlighter highlighter;
        private TurnResources resources = new TurnResources();
        private Dictionary<GridCoord, int> reachable = new();

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
            turnOrder.Roll(combatants);
            LogInitiative();
            BeginTurn();
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0)) HandleClick();
            if (Input.GetKeyDown(KeyCode.Space)) EndTurn(); // press Space to pass the turn
        }

        void HandleClick()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 200f)) return;

            // If we hit a combatant, select it (inspect). Otherwise treat as a ground click.
            var clicked = hit.collider.GetComponent<Combatant>();
            if (clicked != null) { Select(clicked); return; }

            GridCoord cell = grid.WorldToCoord(hit.point);
            if (grid.InBounds(cell)) TryMoveTo(cell);
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

        void LogInitiative()
        {
            Debug.Log("=== Initiative Order ===");
            int i = 1;
            foreach (var e in turnOrder.Entries)
                Debug.Log($"{i++}. {e.combatant.Character.characterName} " +
                          $"({e.combatant.Team}) — {e.total} (rolled {e.roll}, Dex {e.dexModifier:+0;-0;0})");
        }

        void BeginTurn()
        {
            var entry = turnOrder.Current;
            if (entry == null) return;

            foreach (var c in combatants) c.SetSelected(c == entry.combatant);
            selected = entry.combatant;

            resources.ResetForTurn(entry.combatant.Character.Speed);
            RefreshMovementRange();

            Debug.Log($"--- Round {turnOrder.Round}: {entry.combatant.Character.characterName}'s turn " +
                      $"({entry.combatant.Team}) — Speed {entry.combatant.Character.Speed} ft ---");
        }

        void RefreshMovementRange()
        {
            var active = turnOrder.Current?.combatant;
            if (active == null) { if (highlighter) highlighter.Clear(); return; }

            reachable = MovementRange.Reachable(
                active.Coord,
                resources.MovementRemaining,
                grid,
                coord => IsOccupied(coord));   // live occupancy as the block test

            if (highlighter) highlighter.Show(reachable.Keys, grid);
        }

        void TryMoveTo(GridCoord target)
        {
            var active = turnOrder.Current?.combatant;
            if (active == null) return;
            if (!reachable.TryGetValue(target, out int cost)) return; // not a legal cell
            if (!resources.CanSpendMovement(cost)) return;

            occupancy.Remove(active.Coord);     // leave old cell
            active.SetCoord(target, grid, tokenYOffset);
            occupancy[active.Coord] = active;   // claim new cell
            resources.SpendMovement(cost);

            Debug.Log($"{active.Character.characterName} moved to {target} " +
                      $"({cost} ft). Movement left: {resources.MovementRemaining} ft.");
            RefreshMovementRange();             // recompute from the new spot
        }

        void EndTurn()
        {
            turnOrder.Advance();
            BeginTurn();
        }
    }
}