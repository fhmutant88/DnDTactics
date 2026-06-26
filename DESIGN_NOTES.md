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

## Exploration & progression layer (future milestone — NOT the current economy work)
A major deferred system. Captured now so the economy/movement decisions stay compatible.

- Party physically explores the generated dungeon map BETWEEN encounters, rather than
  loading straight into a fight. The map already generates as full tile rooms/corridors,
  so this layer sits on top of existing procgen.
- Group vs. individual movement (BG3-style): party travels together with a designated
  LEADER (the "active" character), then splits into individual tactical movement once
  combat begins. Needs a group/ungroup toggle.
- Discovery-driven content for "what's around the next corner" tension:
  - Traps (hidden until detected/triggered)
  - Treasure chests (lootable piles placed by generation)
  - Wandering/random monsters encountered via exploration, not all spawned up front
  - Encounters TRIGGER on movement/proximity instead of starting immediately
- Goal: a sense of foreboding and exploration, not a fight-simulator. No authored story
  required — tension comes from procedural discovery.

## Gold/economy ownership (DECIDED — building now)
- Gold is PER-CHARACTER (lives on each BarracksMember, travels with them between parties).
- Items are PER-CHARACTER inventories (travel with the character).
- A party-level "leader"/active-character reference designates who pays (e.g. town healer).
- Combat loot: items drop on the dead character's cell as a lootable pile; a living
  character picks them up on their turn when on/adjacent (free "interact" action).
- Out-of-combat: menu-based transfer of gold/items between members (incl. revival payment:
  move gold to the leader, leader pays).
- NO carrying-capacity limits yet (add encumbrance later if desired).
