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

## Save identity = content asset filename
Saves reference Species/Class/Background by the asset's filename (its .name),
resolved on load via Assets/Resources/ContentDatabase.asset.
- DO NOT rename content asset files once real saves exist, or those saves can't
  re-link that reference. (Add new assets freely; just don't rename existing ones.)
- Upgrade path if needed later: add an explicit string `id` field to content SOs
  and key the database on that instead, decoupling save identity from filename.
- Remember to add new Species/Classes/Backgrounds to the ContentDatabase lists.
