using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DnDTactics.Combat;       // GridCoord, TacticalGrid
using DnDTactics.Core;         // GameSession
using DnDTactics.Characters;   // BarracksMember
using DnDTactics.Rules;        // Vision

namespace DnDTactics.Procgen
{
    // Fog of war: reveals dungeon tiles as the party explores.
    // Sub-step 2: PARTY-UNION brightness (Visible where ANY member currently sees; Explored where
    // seen-before-but-not-now; Unseen otherwise). Per-selected-character brightness comes in 2b.
    public class FogOfWar : MonoBehaviour
    {
        public DungeonVisualizer dungeon;
        public ExplorationManager exploration;
        public ExplorationEncounters encounters;

        private TacticalGrid grid;
        private readonly HashSet<GridCoord> everExplored = new();  // permanent map knowledge
        private HashSet<GridCoord> currentlyVisible = new();

        public void ClearExplored()
        {
            everExplored.Clear();
            currentlyVisible.Clear();
        }

        void Start()
        {
            if (dungeon == null) dungeon = FindFirstObjectByType<DungeonVisualizer>();
            if (exploration == null) exploration = FindFirstObjectByType<ExplorationManager>();
            if (encounters == null) encounters = FindFirstObjectByType<ExplorationEncounters>();
        }

        // Called by ExplorationManager whenever the party's positions change.
        public void Recompute()
        {
            if (dungeon == null) return;
            grid = dungeon.Grid;
            if (grid == null || exploration == null) return;

            // Gather party member positions once.
            var memberPositions = new List<GridCoord>();
            foreach (var (coord, _) in exploration.CharacterVisionData())
                memberPositions.Add(coord);

            // 1) UNION of all members' raw sight (range-limited) → MAPPED EVER.
            var unionVisible = new HashSet<GridCoord>();
            foreach (var (coord, darkvisionFeet) in exploration.CharacterVisionData())
            {
                int radius = Vision.SightRadiusTiles(darkvisionFeet);
                foreach (var t in Vision.VisibleTiles(coord, radius, grid))
                    unionVisible.Add(t);
            }

            // 2) Raw lit tiles (what the lights illuminate, LOS from the light source).
            var rawLit = new HashSet<GridCoord>();
            foreach (var torchPos in exploration.LitTorchPositions())
                foreach (var t in Vision.VisibleTiles(torchPos, ExplorationManager.TorchRadiusTiles, grid))
                    rawLit.Add(t);
            if (encounters != null)
                foreach (var lightPos in encounters.PlacedLights)
                    foreach (var t in Vision.VisibleTiles(lightPos, encounters.PlacedLightRadius, grid))
                        rawLit.Add(t);

            // 3) A lit tile is only REVEALED to the party if a member has LOS to it (light carries,
            //    so no range limit — but you must be able to SEE it, not be walled off from it).
            var litAndSeen = new HashSet<GridCoord>();
            foreach (var lit in rawLit)
            {
                foreach (var mp in memberPositions)
                {
                    if (Vision.HasLineOfSight(mp, lit, grid)) { litAndSeen.Add(lit); break; }
                }
            }

            // Map anything seen raw OR lit-and-seen.
            foreach (var c in unionVisible) everExplored.Add(c);
            foreach (var c in litAndSeen) everExplored.Add(c);

            // 4) BRIGHT = lit-and-seen (objective) ∪ SELECTED character's sight (subjective).
            var bright = new HashSet<GridCoord>(litAndSeen);
            var sel = exploration.SelectedVisionData();
            if (sel.HasValue)
            {
                int radius = Vision.SightRadiusTiles(sel.Value.darkvisionFeet);
                foreach (var t in Vision.VisibleTiles(sel.Value.coord, radius, grid))
                    bright.Add(t);
            }

            // 5) Paint.
            foreach (var c in everExplored)
            {
                dungeon.SetTileVisibility(c,
                    bright.Contains(c) ? DungeonVisualizer.TileVisibility.Visible
                                       : DungeonVisualizer.TileVisibility.Explored);
            }

            // (temp debug)
            Debug.Log($"[Fog] Recompute: members={memberPositions.Count}, union={unionVisible.Count}, " +
                      $"litAndSeen={litAndSeen.Count}, everExplored={everExplored.Count}");
        
        }

        public void ResetForNewDungeon()
        {
            everExplored.Clear();
            currentlyVisible.Clear();
            grid = dungeon != null ? dungeon.Grid : null;
            Debug.Log($"[Fog] Reset: everExplored cleared (now {everExplored.Count}). Grid={(grid != null)}");
            Recompute();
            Debug.Log($"[Fog] After reset Recompute: everExplored={everExplored.Count} tiles revealed.");
        }
    }
}