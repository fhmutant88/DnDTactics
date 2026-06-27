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

            // Union of every living member's currently-visible tiles.
            var newVisible = new HashSet<GridCoord>();
            foreach (var (coord, darkvisionFeet) in exploration.CharacterVisionData())
            {
                int radius = Vision.SightRadiusTiles(darkvisionFeet);
                foreach (var t in Vision.VisibleTiles(coord, radius, grid))
                    newVisible.Add(t);
            }

            // Tiles that left view → drop to Explored (if ever explored).
            foreach (var c in currentlyVisible)
                if (!newVisible.Contains(c))
                    dungeon.SetTileVisibility(c, DungeonVisualizer.TileVisibility.Explored);

            // Newly/continuing visible tiles → Visible, and record as explored.
            foreach (var c in newVisible)
            {
                dungeon.SetTileVisibility(c, DungeonVisualizer.TileVisibility.Visible);
                everExplored.Add(c);
            }

            currentlyVisible = newVisible;
        }
    }
}