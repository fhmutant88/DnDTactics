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

        private TacticalGrid grid;
        private readonly HashSet<GridCoord> everExplored = new();  // permanent map knowledge
        private HashSet<GridCoord> currentlyVisible = new();

        void Start()
        {
            if (dungeon == null) dungeon = FindFirstObjectByType<DungeonVisualizer>();
            if (exploration == null) exploration = FindFirstObjectByType<ExplorationManager>();
        }

        // Called by ExplorationManager whenever the party's positions change.
        public void Recompute()
        {
            if (dungeon == null) return;
            grid = dungeon.Grid;
            if (grid == null || exploration == null) return;

            // 1) UNION of all members' raw sight → MAPPED EVER (permanent layout knowledge).
            var unionVisible = new HashSet<GridCoord>();
            foreach (var (coord, darkvisionFeet) in exploration.CharacterVisionData())
            {
                int radius = Vision.SightRadiusTiles(darkvisionFeet);
                foreach (var t in Vision.VisibleTiles(coord, radius, grid))
                    unionVisible.Add(t);
            }

            // 2) LIT tiles (objective): within a lit torch's radius, with LOS from the torch.
            var litTiles = new HashSet<GridCoord>();
            foreach (var torchPos in exploration.LitTorchPositions())
                foreach (var t in Vision.VisibleTiles(torchPos, ExplorationManager.TorchRadiusTiles, grid))
                    litTiles.Add(t);

            // Map anything seen raw OR lit.
            foreach (var c in unionVisible) everExplored.Add(c);
            foreach (var c in litTiles) everExplored.Add(c);

            // 3) BRIGHT = lit tiles (objective, all) ∪ SELECTED character's sight (subjective).
            var bright = new HashSet<GridCoord>(litTiles);
            var sel = exploration.SelectedVisionData();
            if (sel.HasValue)
            {
                int radius = Vision.SightRadiusTiles(sel.Value.darkvisionFeet);
                foreach (var t in Vision.VisibleTiles(sel.Value.coord, radius, grid))
                    bright.Add(t);
            }

            // 4) Paint mapped tiles: Visible if bright, else Explored. Unseen stays hidden.
            foreach (var c in everExplored)
            {
                if (bright.Contains(c))
                    dungeon.SetTileVisibility(c, DungeonVisualizer.TileVisibility.Visible);
                else
                    dungeon.SetTileVisibility(c, DungeonVisualizer.TileVisibility.Explored);
            }
        }
    }
}