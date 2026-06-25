using System.Collections.Generic;

namespace DnDTactics.Combat
{
    // Computes reachable cells from a start within a feet budget, using a breadth-first
    // search over the 8 neighbors. Pure logic — no Unity types.
    public static class MovementRange
    {
        private static readonly int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] dz = { 0, 0, 1, -1, 1, -1, 1, -1 };

        // Returns each reachable cell mapped to the feet cost to reach it.
        public static Dictionary<GridCoord, int> Reachable(
            GridCoord start, int feetBudget, TacticalGrid grid,
            System.Func<GridCoord, bool> isBlocked)
        {
            var cost = new Dictionary<GridCoord, int> { [start] = 0 };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                GridCoord cur = queue.Dequeue();
                int curCost = cost[cur];

                for (int i = 0; i < 8; i++)
                {
                    var next = new GridCoord(cur.x + dx[i], cur.z + dz[i]);
                    if (!grid.IsWalkable(next)) continue;       // bounds + walls
                    if (isBlocked(next)) continue;              // occupied cells
                    if (next.Equals(start)) continue;

                    // 5e basic rule: each step (including diagonal) is 5 ft.
                    int stepCost = curCost + 5;
                    if (stepCost > feetBudget) continue;

                    if (!cost.ContainsKey(next) || stepCost < cost[next])
                    {
                        cost[next] = stepCost;
                        queue.Enqueue(next);
                    }
                }
            }

            cost.Remove(start); // the start cell isn't a "move target"
            return cost;
        }
    }
}