using System.Collections.Generic;
using System.Linq;
using DnDTactics.Rules;

namespace DnDTactics.Combat
{
    // Decides what one enemy wants to do this turn. Pure-ish planning logic:
    // it picks a target and the best cell to move to, then reports whether it can attack.
    public static class EnemyAI
    {
        public struct Plan
        {
            public Combatant target;
            public GridCoord moveTo;     // where to move (may equal current cell = don't move)
            public bool willAttack;      // can it attack the target after moving?
        }

        public static Plan Decide(
            Combatant self,
            IEnumerable<Combatant> allCombatants,
            int movementFeet,
            TacticalGrid grid,
            System.Func<GridCoord, bool> isOccupied)
        {
            var plan = new Plan { moveTo = self.Coord };

            // Target the nearest living combatant on the other team.
            var enemies = allCombatants
                .Where(c => c != null && c.Team != self.Team)
                .ToList();
            if (enemies.Count == 0) return plan;

            plan.target = enemies
                .OrderBy(e => self.Coord.DistanceInSquares(e.Coord))
                .First();

            int reach = self.Weapon != null ? self.Weapon.rangeFeet : 5;

            // Already in range? Then don't move; just attack.
            if (self.Coord.DistanceInFeet(plan.target.Coord) <= reach)
            {
                plan.willAttack = true;
                return plan;
            }

            // Otherwise find reachable cells and pick the one closest to the target.
            // The target's own cell is occupied, so we aim for the nearest cell we *can* stand on.
            var reachable = MovementRange.Reachable(self.Coord, movementFeet, grid, isOccupied);
            if (reachable.Count == 0) return plan; // boxed in; stay put

            GridCoord best = self.Coord;
            int bestDist = int.MaxValue;
            foreach (var cell in reachable.Keys)
            {
                int d = cell.DistanceInSquares(plan.target.Coord);
                if (d < bestDist) { bestDist = d; best = cell; }
            }
            plan.moveTo = best;

            // After moving to 'best', will the target be within reach?
            int reachAfter = best.DistanceInFeet(plan.target.Coord);
            plan.willAttack = reachAfter <= reach;
            return plan;
        }
    }
}