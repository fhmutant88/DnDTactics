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

## Town & economy expansion (future — beyond current revival work)
- TOWN as a hub: restock, heal, and a SHOP to buy goods/armor/weapons/diamonds.
  Town healer (built) is the first piece; shop is a later system (catalog + pricing + UI).
- Revival DIAMONDS enter inventory via: (a) exploration loot (random, ~uncommon rarity),
  or (b) purchase at the town shop. Characters do NOT start with diamonds.
  (Field-revival code consumes diamonds from inventory regardless of source — forward-compatible.)
- STARTING GOLD at character creation, allotted by class + background, spent on an initial
  equipment loadout. Requires the equipment/shop system to exist first. Capture now, build later.
- SANCTUARY PORTAL reconceived as a consumable "Portal Scroll": each character STARTS with one.
  Instantly leaves exploration → returns to town. Build with the sanctuary-portal piece.

## Field revival — gating & penalty (current build = simplified, upgrade later)
- CASTER GATE (deferred): full rule requires a living member who has Revivify/Raise Dead
  PREPARED, or consumes a revival SCROLL — PLUS the diamond. Needs the spellcasting system
  (prepared spells) + a scroll item type, both deferred. FOR NOW: gate on the DIAMOND only
  (any party with the right diamond can revive). Add the prepared-spell/scroll check when
  spellcasting is built.
- RAISE DEAD PENALTY: full 5e fidelity desired (−4 to all d20 rolls, recovering 1 per long
  rest over 4 long rests). BUILD NOW: store the penalty as a recovery counter on the character
  that ticks down over 4 long rests (the "stay in barracks 4 rests to be normal" mechanic).
  WIRE LATER: apply the actual −4 to combat d20 rolls when next in the combat-rolls code.
  Intent: exploring before the penalty clears should really hurt.

## Persistent character conditions (future — pattern established by Raise Dead penalty)
- Conditions persist on the character until cleared by their specific condition (rest, treatment, etc.).
- Example: POISONED — if a character is barracked while poisoned and untreated, the poison
  persists until TREATED (not just rested). Show a "sick"/condition tag on the character card.
- Build conditions as a general system (a list of active conditions, each with its own clear
  rule + combat effect). The Raise Dead recovery counter is the first instance of this pattern.
- Not needed now; capture so the penalty system is designed to generalize.

## Character creation — score pool display (art-phase polish)
- Roll-a-pool-then-assign logic works correctly (6 independent 4d6-drop-lowest, assign each once,
  reroll = full reset). BUG FIXED: previously every stat got the same value.
- DEFERRED: the rolled pool isn't visibly displayed yet — you assign by cycling each ability's
  ◄ ► through the remaining values. Art phase: show the pool as on-screen dice/chips you can
  see and (eventually) drag. A dedicated poolText element is the quick interim fix if wanted.

## Exploration layer — decisions & deferred notes
DECIDED for piece 1:
- Camera: reuse the isometric tactical camera + grid (dungeon already drawn this way).
- Movement: party moves as a SINGLE UNIT, free real-time movement (click a tile, party walks there),
  no turn order outside combat. BG3-style group/individual-split control deferred.
- Triggers: proximity/step-on. Moving onto/near a marker fires encounters/chests/traps.

DEFERRED (flagged during piece 1, build later):
- Camera angle control: let player rotate/change camera angles.
- Wall transparency: walls that block the view of the party should fade/become transparent
  when they occlude navigation (occlusion handling).
- Perception rolls on discovery: traps/hidden things get a silent Perception check in the
  background. FAIL = no notice (you might trigger it). SUCCESS = notify the player ("you spot...").
  Needs the trap/discovery content + a perception check; wire when discoveries are built.

## Fog of war (exploration — build later, design now)
The full dungeon must NOT be visible on entry — core to the "what's around the corner" tension.
- Tiles start HIDDEN; revealed as the party explores (sight radius around the party, and/or
  revealing a room when entered).
- Likely THREE states per tile: Unseen (hidden/black), Explored (seen before, dimmed — you
  remember the layout but not current contents), Visible (currently in sight, fully lit).
- Discoveries (encounters/chests/traps) should be hidden under fog until revealed/perceived —
  pairs directly with the deferred Perception-roll mechanic (silent check; success reveals).
- Interacts with deferred wall-transparency (occluding walls fade) and camera-angle control.
- Implementation note: DungeonVisualizer currently renders ALL floors + border walls up front.
  Fog will need tile renderers toggled/tinted by visibility state, so consider keeping per-tile
  GameObject refs (or a visibility layer) when we build it. Not now — piece 1 shows the full map.

## Save model (Model B) + TPK — DECIDED, building now
- TWO files per slot: autosave (live, always current, overwrites) + ONE manual save (player-controlled).
- Autosave = where you are (incl. deaths). Manual save = a deliberate checkpoint/fallback.
- Loading the manual save OVERWRITES the autosave (rewind-and-continue: checkpoint becomes live state).
- "Save Game" (manual save) is TOWN-ONLY (Roster). Unlimited overwrites of the one manual slot.
- Once you launch a run you are LOCKED IN — no save options mid-dungeon.
- TPK (all deployed members dead, no survivors): the run/party + everything they carried is LOST.
  Autosave persists the wipe. Recourse = Load Manual Save (if one exists). Else loss stands.
  TPK routes to MainMenu with a "your party has fallen" message.
- One survivor = party salvageable (can revive fallen via existing revival system).

## Sanctuary portal — strategic role clarified (build with exploration, still deferred)
- The portal is the ONLY way to bank progress mid-run: saving is town-only, so portaling out
  (exploration → town) is what lets you then manual-save and lock in gains.
- Risk/reward: push deeper (committed, no save) vs. portal out (bank progress, can save).
- Each character starts with one Portal Scroll (consumable). Build with exploration/portal piece.

## Death saving throws (5e fidelity — deferred)
Currently a hero at 0 HP goes straight to Down (binary, out of the fight). Authentic 5e:
- At 0 HP, roll a death save each turn: d20, 10+ = success, <10 = failure.
- 3 successes = stabilized; 3 failures = dead. Nat 20 = revive at 1 HP. Nat 1 = 2 failures.
- Taking damage while down = 1 failure (a crit = 2). Healing any amount = back up immediately.
- Adds a mid-combat window to stabilize/heal a downed ally before they're lost — ties into
  revival, the Down/Dead distinction, and in-combat healing (which itself is deferred).
- Needs: per-combatant death-save tracking (success/fail counts), a check each turn while at 0 HP,
  and combat to handle "down but not out" as a distinct state from the current "Down" (barracks).
- Build when fleshing out combat depth (alongside spellcasting / in-combat healing).

## Portal scroll — in-combat extraction, FULL design (build with combat-abilities system)
The portal is a PERSISTENT, STATEFUL combat object (a real ability w/ a state machine):
- CAST as an action (consumes caster's scroll); portal opens at caster's cell, stays open.
- Each later turn, any PARTY member may ENTER as their action → extracted safe to town, removed
  from initiative/field. Monsters cannot enter.
- The portal CLOSES when: (a) the caster goes through (caster must be LAST — holds the door for
  the party), OR (b) the caster deliberately closes it early as an action (to evacuate only 1-2
  and keep fighting with the rest — tactical split), OR (c) it TIMES OUT (e.g. end of encounter;
  if cast in exploration, persists until end of the NEXT encounter).
- Drama/decisions: who escapes vs. stays, does the caster hold too long and get downed, split the
  party (send the wounded home, press on). Heroes downed before entering follow revival rules.
- Run resolves to "returned to town" once all leaving heroes are through (full or partial extraction).
- This is a stateful combat ability — build alongside spellcasting/abilities action economy.

## Portal scroll — EXPLORATION version (BUILD NOW, simple)
- Between fights only (no action economy). Use scroll → consumed → party returns to town → run banked.
- No open/close/timeout/selective-entry — those are inherently combat-only mechanics.

## Portal scroll — EXPLORATION version: DONE
- Each new character starts with one Portal Scroll (granted at creation).
- "Use Portal (N)" button in exploration HUD; consumes one scroll, banks the run, returns to town.
- Disabled during combat (exploration-only). Full in-combat extraction still deferred (see above).

## Chests & traps — FULL design (most deferred; depends on foundational systems)
CHESTS are containers of possibilities, not simple pickups:
- May be: plainly lootable, TRAPPED (trap attached to container), a MIMIC (monster — combat handoff),
  and/or LOCKED (needs a lockpick mechanic — a skill check, deferred).
- Interaction flow: inspect → detect trap (vision/perception) → locked? → pick lock → loot OR fight.
TRAPS are TYPED, data-driven, each with its own effect/save/area/footprint ("space quotient"):
- e.g. fireball (AoE, Dex save), pit (fall, single tile), spikes, etc. Different ranges & areas.
- Traps attach to many surfaces: FLOORS, WALLS, CEILINGS, and CHESTS — a cross-cutting concept.
- Need a typed-effects system (areas + saves) — overlaps with the spellcasting/abilities effect system.
LIGHTING is partly GAMEPLAY, not just art:
- Light sources: torches, candles, magic items. Lighting may be randomized per dungeon build.
- Feeds vision/darkvision → what you can see → trap detection + fog of war.

## FOUNDATIONAL ORDER (keystones that unlock the above)
Highest-leverage systems, build these first; the rich features above depend on them:
1. VISION / DARKVISION / LIGHTING — unlocks trap detection, fog of war, chest-trap spotting,
   line-of-sight, the "what's around the corner" tension. (Fog of war == this system.)
2. INDIVIDUAL CHARACTER POSITIONS in exploration (group/individual movement, BG3-style) —
   unlocks "who sees/who's nearest," tactical positioning; vision needs it.
3. TYPED EFFECTS w/ areas + saves — unlocks traps' typed effects AND spellcasting; shared system.
4. CONTAINER INTERACTIONS (lock/trap/mimic) + LOCKPICKING skill check — built on 1-3.
=> Natural milestone: vision + individual movement + lighting + fog of war + traps, as ONE cluster.

## Chests — TRIVIAL loot version (OK to build now, standalone, no throwaway)
- Reach a chest → gold + maybe item to leader. No lock/trap/mimic yet (those need the foundation).
