using System.Collections.Generic;
using DnDTactics.Combat;   // TacticalGrid, GridCoord

namespace DnDTactics.Rules
{
    // Pure logic: which grid tiles a character can currently see.
    // Vision range (in tiles) = max(baseline, darkvision), trimmed by line-of-sight (walls block).
    public static class Vision
    {
        public const int FeetPerTile = 5;
        public const int BaselineTiles = 1; // see your own 3x3 even in darkness (no light/darkvision)

        // Convert a feet value (e.g. species darkvisionRange) to tiles.
        public static int FeetToTiles(int feet) => feet / FeetPerTile;

        // The effective sight radius in tiles for a given darkvision (feet).
        // (Lighting will later raise the baseline in lit areas — deferred.)
        public static int SightRadiusTiles(int darkvisionFeet)
        {
            int dv = FeetToTiles(darkvisionFeet);
            return dv > BaselineTiles ? dv : BaselineTiles;
        }

        // All tiles visible from `origin` within `radiusTiles`, with walls blocking line-of-sight.
        // "Visible" = within Chebyshev range AND an unobstructed Bresenham line from origin.
        public static HashSet<GridCoord> VisibleTiles(GridCoord origin, int radiusTiles, TacticalGrid grid)
        {
            var visible = new HashSet<GridCoord>();
            if (grid == null) return visible;

            visible.Add(origin); // you always see your own tile

            int minX = origin.x - radiusTiles, maxX = origin.x + radiusTiles;
            int minZ = origin.z - radiusTiles, maxZ = origin.z + radiusTiles;

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    var target = new GridCoord(x, z);
                    if (!grid.InBounds(target)) continue;
                    // Chebyshev range check (square area; range 1 = full 3x3).
                    if (Chebyshev(origin, target) > radiusTiles) continue;
                    if (HasLineOfSight(origin, target, grid))
                        visible.Add(target);
                }
            }
            return visible;
        }

        static int Chebyshev(GridCoord a, GridCoord b)
        {
            int dx = a.x - b.x; if (dx < 0) dx = -dx;
            int dz = a.z - b.z; if (dz < 0) dz = -dz;
            return dx > dz ? dx : dz;
        }

        // Bresenham line from origin to target; blocked if any wall tile lies strictly between
        // them. The target tile itself may be a wall (you can SEE a wall face) — we only block
        // when a wall sits *between* origin and target.
        public static bool HasLineOfSight(GridCoord origin, GridCoord target, TacticalGrid grid)
        {
            int x0 = origin.x, z0 = origin.z;
            int x1 = target.x, z1 = target.z;

            int dx = System.Math.Abs(x1 - x0);
            int dz = System.Math.Abs(z1 - z0);
            int sx = x0 < x1 ? 1 : -1;
            int sz = z0 < z1 ? 1 : -1;
            int err = dx - dz;

            int cx = x0, cz = z0;
            while (true)
            {
                if (cx == x1 && cz == z1) break; // reached target

                // Step to the next cell along the line.
                int e2 = 2 * err;
                if (e2 > -dz) { err -= dz; cx += sx; }
                if (e2 < dx) { err += dx; cz += sz; }

                if (cx == x1 && cz == z1) break; // arrived at target — don't treat target as blocker

                // A wall strictly between origin and target blocks the view.
                if (!grid.IsWalkable(new GridCoord(cx, cz)))
                    return false;
            }
            return true;
        }
    }
}