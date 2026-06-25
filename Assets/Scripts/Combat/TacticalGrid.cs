using UnityEngine;

namespace DnDTactics.Combat
{
    // One cell of the grid. Occupancy (who stands here) is added with combatants.
    public class GridCell
    {
        public GridCoord coord;
        public bool walkable = true;
        public GridCell(GridCoord coord) { this.coord = coord; }
    }

    // The tactical grid as pure logic: its size, its cells, bounds checks, and
    // conversion between grid coordinates and world positions. No rendering here.
    public class TacticalGrid
    {
        public int Width { get; }    // columns along X
        public int Depth { get; }    // rows along Z
        public float CellSize { get; }

        private readonly GridCell[,] cells;

        public TacticalGrid(int width, int depth, float cellSize = 1f)
        {
            Width = width; Depth = depth; CellSize = cellSize;
            cells = new GridCell[width, depth];
            for (int x = 0; x < width; x++)
                for (int z = 0; z < depth; z++)
                    cells[x, z] = new GridCell(new GridCoord(x, z));
        }

        public bool InBounds(GridCoord c) =>
            c.x >= 0 && c.x < Width && c.z >= 0 && c.z < Depth;

        public GridCell GetCell(GridCoord c) => InBounds(c) ? cells[c.x, c.z] : null;

        public bool IsWalkable(GridCoord c)
        {
            GridCell cell = GetCell(c);
            return cell != null && cell.walkable;
        }

        // Grid -> world: the cell center on the XZ plane at Y = 0.
        public Vector3 CoordToWorld(GridCoord c) =>
            new Vector3(c.x * CellSize, 0f, c.z * CellSize);

        // World -> grid: snap a world point to the nearest cell.
        public GridCoord WorldToCoord(Vector3 world) =>
            new GridCoord(Mathf.RoundToInt(world.x / CellSize),
                          Mathf.RoundToInt(world.z / CellSize));
    }
}