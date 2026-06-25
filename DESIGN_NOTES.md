# Future / Deferred Features

Features intentionally postponed, with the agreed approach recorded so we don't
re-litigate the decision later.

## Surface movement (spider-climb, fly, climb speeds)
Deferred — belongs with the future Elevation & Terrain system.

- Implement as MOVEMENT-RULE ABILITIES on the flat tactical plane, NOT literal
  wall/ceiling rendering. A climbing/flying creature gets per-creature traits +
  speeds and a modified blocking/cost rule in MovementRange (which already takes an
  isBlocked function), letting it reach cells others cannot.
- Literal surface traversal (a spider rendered crawling up a vertical wall) is a
  separate, much larger 3D problem (grid model, cross-surface pathfinding, camera,
  animation) and is NOT planned. If ever wanted, it's an art/animation flourish on
  top, not a rules change.
- Clusters with: difficult terrain, height, cover, climb/fly speeds.
- Architecture already supports the rules-level version; no changes needed now.
