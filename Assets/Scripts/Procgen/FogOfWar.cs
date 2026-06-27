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

            // 1) UNION of all living members' sight → defines what's MAPPED EVER (permanent).
            var unionVisible = new HashSet<GridCoord>();
            foreach (var (coord, darkvisionFeet) in exploration.CharacterVisionData())
            {
                int radius = Vision.SightRadiusTiles(darkvisionFeet);
                foreach (var t in Vision.VisibleTiles(coord, radius, grid))
                    unionVisible.Add(t);
            }
            foreach (var c in unionVisible) everExplored.Add(c); // accumulate permanent knowledge

            // 2) SELECTED character's sight → defines what's BRIGHT right now.
            var selectedVisible = new HashSet<GridCoord>();
            var sel = exploration.SelectedVisionData();
            if (sel.HasValue)
            {
                int radius = Vision.SightRadiusTiles(sel.Value.darkvisionFeet);
                selectedVisible = Vision.VisibleTiles(sel.Value.coord, radius, grid);
            }

            // 3) Paint every mapped tile: Visible if the SELECTED char sees it, else Explored (dimmed).
            //    (Unseen tiles — never mapped — stay hidden, untouched.)
            foreach (var c in everExplored)
            {
                if (selectedVisible.Contains(c))
                    dungeon.SetTileVisibility(c, DungeonVisualizer.TileVisibility.Visible);
                else
                    dungeon.SetTileVisibility(c, DungeonVisualizer.TileVisibility.Explored);
            }
        }
    }
}