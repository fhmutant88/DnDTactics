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

## Encounter triggering with individual movement (movement foundation now; staggered-join later)
- An encounter triggers when ANY one character enters a marker's range (the scout springs it).
  → BUILDABLE NOW with individual tokens.
- FULL VISION (needs combat changes — DEFER): only the triggering character enters initiative
  initially; the rest JOIN initiative as they physically arrive at the fight (mid-combat
  reinforcement + dynamic initiative insertion). Rewards keeping the party together; punishes
  reckless solo scouting; enables bait/ambush tactics.
- Needs combat to support: units entering an in-progress encounter, and inserting them into the
  initiative order on arrival. Current combat rolls initiative once for a fixed roster.
- INTERIM (now): when an encounter triggers, bring the whole living party into the fight at once
  (spawned near their exploration positions), as the current bridge does.

## Movement foundation — piece 1 DONE
- Exploration now spawns one token per deployed living member (leader = gold token[0], others blue),
  on adjacent cells in the first room. Group click-to-move: leader to target, followers cluster.
- Encounters/chests trigger when ANY character nears the marker (exploration.CharacterCoords).
- Chest loot confirmed working (gold/item to leader on proximity).
- NEXT: piece 2 selection (click a token to select), then Group/Individual toggle + true individual
  movement, then the group/ungroup control. Then vision/LOS/fog + traps build on individual positions.

## Movement foundation — pieces 1-3 DONE
- Piece 1: individual character tokens (one per deployed living member; leader = gold token[0],
  others blue), spawned on adjacent cells in the first room. Group click-to-move (leader to target,
  followers cluster). Encounters/chests trigger on ANY character's proximity (CharacterCoords).
- Piece 2: click-to-select a token (scale-up highlight); leader pre-selected; "Selected: [name]"
  label on the exploration HUD (ExplorationManager.SelectedName).
- Piece 3: Group/Individual movement toggle (ExplorationManager.individualMode + ToggleMode()).
  Group = ground-click moves whole party; Individual = ground-click moves only the selected token,
  others hold. Mode button on the exploration HUD.
- CombatHUD now hidden during exploration, shown only in combat (SetVisible(bool); exploration hides
  it via a next-frame coroutine to beat the build-order race, shows it in TriggerEncounter).
- DEFERRED piece 4: "Regroup" command (gather everyone to the leader after a split) — small finisher.

## NEXT MILESTONE: vision / darkvision / line-of-sight / fog of war
Built on the individual positions now established. The keystone that unlocks: per-character vision
(selected character's vision/darkvision property), true line-of-sight (no seeing through walls/corners),
fog of war (Unseen/Explored/Visible tile states), and vision-driven trap detection + rich chests.
Lighting (torches/candles/magic, possibly randomized per dungeon) feeds vision; partly gameplay,
not just art. Fog of war == this same vision system rendered.

## Vision system — DUAL-LAYER design (KEY decision)
Two distinct kinds of "seeing", built on the same per-character visible-tile computation
(vision range + species darkvision + Bresenham LOS, walls block):

LAYER 1 — MAP LAYOUT (party-collective, permanent once seen) = FOG OF WAR:
- A tile's layout is EXPLORED if ANY living member has ever had LOS to it in range. Stays known.
- Tile states: VISIBLE (in ANY member's current sight — bright) / EXPLORED (seen before, not now —
  dimmed) / UNSEEN (never seen — hidden). Layout Visible/Explored uses the PARTY UNION.
- Once a room's geography is seen, the party "knows the layout" permanently.

LAYER 2 — LIVE CONTENTS (per-SELECTED-character, dynamic) = chests/monsters/(traps):
- A chest/monster is SHOWN only if the CURRENTLY SELECTED character has LOS to it within THEIR
  vision range right now. Switch characters → visible contents change to that character's POV.
- Example: human + elf party. Both reveal a room's LAYOUT (union). But a chest 60ft away in the
  dark is SEEN only when the elf (darkvision 60) is selected; select the human → chest hidden,
  though the room's layout is still known.
- Monsters in exploration are "contents" too: only the selected character sees a lurking monster
  (scout-ahead tension). Once COMBAT triggers, all combatants visible (it's a fight).
- Always a selected character driving contents (leader pre-selected, selection persists).

ARCHITECTURE: per-character "tiles I can see now" = range + darkvision + Bresenham LOS.
  Layout fog = UNION of all members' sets (Visible) + accumulated history (Explored).
  Contents = the SELECTED character's set only.
Vision range = max(baseline ambient sight, species darkvision). Real lighting deferred.

## Vision system — REFINEMENTS (supersedes/clarifies above)
FOG vs. BRIGHTNESS split (refined):
- FOG STATE (mapped-ever?) = PARTY-COLLECTIVE and PERMANENT. A tile is "mapped" once ANY living
  member has had LOS to it in range; stays mapped forever (you know the layout).
- CURRENT BRIGHTNESS of mapped tiles = the SELECTED character's POV. Dark dungeon example:
  mapped room shows as grey layout when human selected; lights up brightly within the elf's
  darkvision range when elf selected. Union = what's mapped ever; selected = how it presents now.
- Leader downed mid-room → brightness/contents tied to them go dark, but fog stays mapped →
  select another living character to re-illuminate from their POV and continue. (Emergent.)

MAP GENERATION MUST CHANGE (structural — do as part of fog):
- DungeonVisualizer currently renders the ENTIRE dungeon up front → contradicts fog of war.
- Tiles must start UNSEEN (hidden/black) and reveal as explored. The map "opens up" as you go.
- You should NOT see the layout until you explore it.

TOGGLEABLE MINIMAP HUD (later, follow-on):
- A separate top-down overview map that fills in as you explore; toggle on/off by player preference.
- Reads the same "what's been mapped" data as the in-world fog. Build after in-world fog works.

MONSTER VISION + REACTIVE AI (later, consumes vision):
- Monsters have their own vision/darkvision (symmetrical to the party). A darkvision monster in the
  dark can see + act on the party before a low-vision party sees it → ambush. Dark is dangerous.
- TYPED monster behavior/intelligence (enhances current simple EnemyAI): kobolds hang back & ambush
  from behind traps; spiders rush; slimes passive/oblivious. Needs vision (ambush) + traps (kobold
  positioning). Build as a combat-AI/behavior milestone AFTER vision (and alongside traps).

## Darkness model + lighting sequencing (DECIDED)
- Environment: COMMIT TO DUNGEON CRAWL. Dark dungeons where vision/light/darkvision are central.
  Forest/city (ambient-lit) environments deferred indefinitely — don't spread scope.
- Darkness is HARSH/atmospheric (fits the hardcore design): baseline ambient sight ~2 tiles
  (you see your own square + immediate neighbors / adjacent party members, but nothing else in
  the dark). NOT literally 0 (so a no-light party isn't hard-stuck and we can build/test vision
  before lighting exists), but low enough that darkness is oppressive and light/darkvision are
  precious.
- Darkvision (species, ~60ft = 12 tiles) is HUGELY valuable vs. the ~2-tile baseline — the
  contrast makes vision matter.
- SEQUENCING: build VISION now (low baseline, testable). Then LIGHTING is the very NEXT milestone:
  torches, magical light items, randomly-lit/unlit dungeon areas that RAISE ambient sight where lit.
  A human (no darkvision) party will depend on carried light → torches become precious.

## Vision constrains COMBAT (deferred — needs vision + lighting + AttackResolver hook)
Vision/darkvision/lighting applies in COMBAT, not just exploration. Maps to 5e unseen rules:
- A character can only cleanly attack a target they can SEE (within vision/darkvision range, LOS,
  and — later — lit). The party's map-knowledge that a monster is "there" does NOT mean you can see it.
- Attacking an UNSEEN target (monster in darkness beyond your sight): DISADVANTAGE + must guess its
  square. At range (bow into a dark room): effectively futile. So a human can't snipe an unlit monster
  across the room; needs it lit or within their sight.
- Even ADJACENT to an unseen foe: attack at disadvantage (you sense the square but can't aim).
- Attacking FROM darkness (you unseen by the target): attacker has advantage / target defends at
  disadvantage. Cuts both ways → darkvision monsters can ambush an unlit party at advantage.
- Tactical payoff: keep darkvision chars forward, bring light, don't shoot into the dark. Positioning
  + light control become combat-relevant.
- IMPLEMENTATION (later): AttackResolver checks "can attacker see target?" (vision range + LOS + lit)
  → applies disadvantage / blocks clean targeting. Built on vision (this milestone) + lighting (next).

## CORRECTION to "vision constrains combat" — two SEPARATE rules (don't conflate):
- RULE 1 (ranged-in-melee, NOT vision-related): firing a RANGED weapon with an enemy within 5ft
  = disadvantage. Applies even if fully visible. "Don't shoot a bow in someone's face."
- RULE 2 (unseen target, vision-related): attacking a target you CAN'T SEE = disadvantage + guess square.
- So: a LIT adjacent monster → MELEE attack is NORMAL (seen + adjacent). Only a BOW at that range is
  disadvantage (Rule 1). The earlier note wrongly implied adjacency causes a vision penalty — it does
  not. Vision disadvantage (Rule 2) applies only when the target is genuinely UNSEEN (e.g., in darkness
  beyond sight). A human firing a bow into a dark room at an unlit monster = BOTH rules potentially:
  unseen (Rule 2) and, if the monster closed to melee, ranged-in-melee (Rule 1).

## Isometric wall occlusion (navigation issue — deferred)
- At the iso camera angle (30°/45°), walls with height occlude floor tiles in hallways running
  "away" from the camera (up-down on screen) → click-raycast hits the wall, can't select those floors.
  Left-right hallways are fine (camera sees into them).
- FIXES (either/both, later): (1) WALL TRANSPARENCY — fade walls between camera and view area so you
  see/click floor behind them (BG3/Diablo-style occlusion fade). (2) CAMERA control — steeper angle
  and/or player-rotatable camera to see around walls.
- Not blocking vision work. Revisit during a navigation/camera polish pass (pairs naturally with the
  fog-of-war reveal, since both touch how the dungeon presents visually).

## Resting in a dungeon + repopulation (DEFERRED — rich mechanic, document now)
- Resting IN a dungeon (mid-run) is RISKY: the engine rolls to spawn new monsters during a rest,
  placed ANYWHERE incl. already-cleared rooms (the dungeon repopulates behind you).
- LONG rest = HIGHER spawn chance than SHORT rest (more time = more wandering monsters).
- Consequence: a dungeon is only TRULY completable (all explored + all monsters dead + all loot)
  on a no-rest run (or lucky rests that spawn nothing). Rest and the dungeon can refill.
- BRILLIANT INFO-DESIGN: the "DUNGEON COMPLETE" prompt is itself the signal. Enter the last room:
  - Prompt appears → nothing spawned during rest → dungeon genuinely clear → leave clean.
  - NO prompt (room empty but no completion) → something spawned → monsters lurk in "cleared" rooms
    → dungeon NOT done; go hunt them or flee. The ABSENCE of the prompt is the warning.
- DEPENDS ON (all new):
  - IN-DUNGEON resting action (currently RestService.LongRest is TOWN-only from Roster). Need a
    "rest here" exploration action (short + long).
  - A reason to rest mid-run: HP (and later spell-slot/ability) recovery — the risk/reward of
    "heal but maybe repopulate." 
  - DUNGEON-COMPLETION condition + tracking (all rooms entered + all monsters dead + all loot?),
    so the engine knows whether to show "Dungeon Complete."
  - Spawn-during-rest logic (chance by rest type; placement anywhere; respects monster budget/level).
- Fits the all-or-nothing/risk-lever design: rest to heal vs. keep the dungeon static. Another gamble
  alongside push-deeper / portal-out.
- Q to resolve later: exact completion condition; short vs long rest spawn chances; do spawned
  monsters scale to party level/depth; does the party get a vague "you hear skittering" hint on a
  spawn, or only the missing-prompt signal.

## Dungeon completion + resting — REFINEMENTS (decided)
- COMPLETION = all monsters dead + all rooms explored. NOT all loot (a failed lockpick could make
  a chest permanently inaccessible — loot is a bonus, not a completion gate).
- FALLEN BODIES: dragged with the party (carried; tracked in party data / downed token). Revival can
  happen anywhere (no corpse-retrieval logistics). Recovered to town at run end (portal or complete).
  [Chosen over bodies-stay-where-they-fall for simplicity + "don't abandon your people" feel.]
- IN-DUNGEON REST (new): short + long rest options during exploration (heal mid-run).
  - SHORT rest: VERY LOW spawn chance; heals short-rest amount (5e: spend Hit Dice, partial).
  - LONG rest: HIGHER spawn chance; FULL heal IF uninterrupted.
    - INTERRUPTION: spawned monsters can appear in the REST ROOM mid-long-rest, before healing
      completes → party gets only SHORT-rest healing (partial) AND must fight the spawn. Great
      risk/reward: gambled on a long rest, got ambushed, come out partially healed + in a fight.
  - Needs: a ShortRest (partial heal) distinct from existing RestService.LongRest (full); interruption
    logic; spawn-during-rest placement.
- Repopulation/"Dungeon Complete" prompt-as-signal: as documented above.

## LEVELED DUNGEONS (unifying progression concept — confirmed)
- Party LEVEL drives the random dungeon build: SIZE (bigger for higher level), monster budget/
  difficulty, chest count/quality. Dungeons are effectively "leveled."
- Current dungeons are smallish + use a FLAT encounter setup. TODO: feed party level into the
  generator (size) AND into encounter/chest placement (budget/quality). Unifies several threads:
  difficulty-scaled chests, level-scaled encounters, bigger high-level dungeons.
- This is the backbone of a future RUN STRUCTURE / PROGRESSION milestone.

## Fallen bodies — STAY WHERE THEY FALL (decided — reverses earlier "dragged" note)
- Bodies STAY at the tile where the character fell (avoids carrying-capacity/weight problems).
  A downed/dead token remains on that tile (you can see where they fell, walk back to it).
- RECOVERY to town only via: PORTAL-OUT or DUNGEON-COMPLETE. Both recover ALL the party's fallen,
  wherever in the dungeon they died (ending the run gathers everyone, living + dead; no scene needed,
  no physical drag-to-exit). [Confirm: recover ALL fallen regardless of location — sane run-end rule.]
- LOOTING THE FALLEN (new mechanic): a living character can loot a downed ally's inventory (potions,
  diamonds, scrolls, strong items) for use during the rest of the crawl. Brutal, thematic resource
  recovery — a death's gear isn't stranded; it redistributes to survivors.
  - Requires reaching the body: move a living character to the fallen's tile → "loot downed ally" action.
  - Looting is PERMANENT redistribution. If you later revive that character, they come back WITHOUT the
    looted items (you took them) — revival does not restore looted gear.
- Revival has a LOCATION too: revive at/near the body (in-dungeon diamond revival happens at the corpse).
- Depends on: downed-token persistence at the fall tile; an "interact with downed ally" action
  (loot / revive); run-end recovery sweeping all fallen.

## Fallen recovery — FINAL rule (harsher; supersedes the "recover ALL" note above)
- DUNGEON-COMPLETE: recovers ALL fallen (you cleared everything → sweep the whole dungeon leaving).
- PORTAL-OUT: recovers ONLY fallen in the SAME ROOM as the portal when opened. Fallen left in OTHER
  rooms are LOST PERMANENTLY (body + gear) unless looted first.
- => Makes LOOTING THE FALLEN essential: when someone drops, loot their gear NOW (secure potions/
  diamonds/scrolls) because you may never recover the body. Every death = "strip the corpse now or
  gamble on recovering them later."
- => To save a fallen ally's BODY (for town revival), either COMPLETE the dungeon or PORTAL FROM THEIR
  ROOM. Otherwise looted gear is all you salvage; the character is lost.
- Geography matters: a far fallen ally = go back + portal from their room (risky traverse through a
  possibly-repopulated dungeon) vs. cut losses, loot what you can, portal from here (lose the body).

## Contents visibility = unified Layer-2 rule (chests, monsters, traps)
- ALL "contents" (chests, lurking monsters, traps) key off the SAME check: does the SELECTED
  character currently SEE this tile? (Vision range + darkvision + Bresenham LOS.) Mapping the room
  (fog/union) does NOT reveal contents — only the selected character's LIVE sight does.
- Manifestation differs by type:
  - CHEST → show/hide a visible token (building now).
  - MONSTER (in exploration) → show/hide a lurking token (deferred to monster-vision/AI milestone).
  - TRAP → when the selected char sees the trap's tile, roll PERCEPTION vs. the trap's DC (random/
    TBD per trap); success → trap revealed/flagged; failure → undetected (trigger on contact).
    (Deferred to traps milestone — but reuses this exact sight gate + adds the detection roll.)
- So traps = chest-style sight gate + a Perception-vs-DC roll. Same machinery, different manifestation.
- Example: elf thief selected, sees a floor tile with a trap in darkvision LOS → rolls Perception vs DC.
  Human selected (can't see that tile in the dark) → no roll, no detection → blunders in.

## Vision milestone — COMPLETE
- Vision rules class (DnDTactics.Rules.Vision): VisibleTiles(origin, radiusTiles, grid) =
  Chebyshev range + Bresenham LOS (walls block). SightRadiusTiles(darkvisionFeet) = max(baseline 1,
  darkvision/5). Baseline 1 tile (3x3), darkvision from Species.darkvisionRange (60ft = 12 tiles).
- DungeonVisualizer: per-tile GameObject + own material, tiles start hidden; SetTileVisibility
  (Unseen=hidden / Explored=dimmed / Visible=full).
- FogOfWar: everExplored = UNION of all living members' sight (permanent map knowledge). Brightness
  per the SELECTED character's live sight (Visible) else Explored. Recompute on move + on selection change.
- Contents (chests): tokens shown only when the SELECTED character currently sees the tile; hide on loot.
- Confirmed working: human (1-tile) vs elf (darkvision) brightness + chest visibility differ by selection.
- Foundation for deferred: traps (contents gate + Perception-vs-DC roll), monster-vision/ambush,
  in-combat unseen-attacker rules, lighting (raises baseline in lit areas). All reuse Vision + LOS.

## Lighting milestone — DESIGN ANSWERS (build after break)
- BOTH carried light (character holds a torch → lights a radius around them) AND placed/ambient
  light (some dungeon tiles/areas lit by braziers etc.).
- Lit tiles become visible to ANYONE with LOS, regardless of darkvision (light overrides the
  darkness baseline → a no-darkvision human sees normally within lit areas).
- Dungeons randomly lit/unlit: some builds have light sources, some are pitch black (darkness
  + no torch + no darkvision = the harsh near-blind baseline). Randomization per dungeon build.
- TORCH RESOURCE MODEL (decided): a torch "just works" while held/in-use (lights its radius);
  goes dark when STOWED. No burn-out timer. Cost is opportunity (a hand/slot + the toggle choice),
  not duration. Toggling held vs. stowed is the meaningful decision.
- Integration: lighting RAISES effective sight in lit areas — layers on the existing Vision system
  (lit tile + LOS = visible regardless of darkvision). Vision.SightRadiusTiles / VisibleTiles get a
  lighting input. Unblocks the eventual Model-2 "main view = current sight only" presentation.
- OPEN (resolve when building): torch light radius (tiles); is a torch an inventory item every
  character can carry/toggle; do placed dungeon lights show as always-lit tiles; how carried-light
  from MULTIPLE party members combines; interaction with the per-selected-character brightness
  (does a lit area stay bright even when a low-vision character is selected? — likely YES, light is
  objective, darkvision is the character-subjective part).

## Item weights (DEFERRED — systemic)
- Items need WEIGHTS eventually, for carrying capacity / encumbrance (and it's why fallen bodies
  stay where they fall rather than being dragged). Affects how much a character can carry/loot.
- Add a `weight` field to ItemDefinition; sum carried weight per character vs. a capacity (STR-based
  in 5e). Build with an inventory/encumbrance pass (alongside equipment/loadout systems).

## Torch consumption — REVISED to "N lights" (simpler, supersedes the 2-long-rest model)
- A torch has a fixed number of LIGHTS (uses), e.g. 10. Each time it's LIT, one use is consumed.
  At 0, the torch is spent (removed). NO dependency on the long-rest system — self-contained,
  buildable now.
- Per-light-action: light → stow → light again = 2 uses. So toggling has a real cost (relighting
  burns a use) → decision: keep it burning vs. stow-and-relight-later. Leaving it lit avoids relight cost.
- IMPLEMENTATION: per-character "active torch" with a remaining-lights counter. First light when none
  active → consume one Torch from inventory (id+count), set counter = torchLights (e.g. 10). Each
  subsequent LIGHT action decrements. Stow = free (no decrement). Counter 0 → active torch spent;
  light another from stock if any, else go dark.
- Starting value ~10 (tunable; torches cheap/common so could be 10–20). Price 1 GP.
- Supersedes the "2 long rests" model (which needed the deferred rest plumbing). This is independent.
