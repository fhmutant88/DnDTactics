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

## Lighting (carried torches) — COMPLETE
- Torch item (1 GP), granted at creation. Carried torch toggled lit/stowed via exploration HUD.
- Lit torch illuminates ~4 tiles (TorchRadiusTiles) around bearer, with LOS — OBJECTIVE (bright for
  ALL, regardless of darkvision). Integrated into FogOfWar.Recompute (bright = lit ∪ selected sight)
  and chest visibility (chest in torchlight visible to all).
- Consumption: N-lights model (TorchLightsPerTorch=10). Lighting a fresh torch consumes 1 Torch from
  inventory + sets counter; each LIGHT action decrements; stow is free; at 0 the active torch is spent
  (light another from stock or go dark). Per-character "active torch" counter (no per-instance inventory).
- Confirmed: no-darkvision human sees in torchlight; stow/relight consumes correctly; chests in torchlight show.
- FOLLOW-ON: placed/ambient dungeon lights (some tiles always-lit, randomized per dungeon) — quick
  additive layer (mark certain tiles lit; fold into the litTiles set in FogOfWar + chest visibility).

## Lighting — FULLY COMPLETE (carried + placed, all LOS-gated)
- Carried torches: held/stowed, ~4-tile objective light, N-lights consumption (10/torch). DONE.
- Placed braziers: two-stage gen (d100: 1-49 dark; else d100→decile % of ROOMS lit). ~5-tile objective light.
- CRITICAL FIX: lit tiles are only revealed if a party member has LINE OF SIGHT to them (light is
  objective but PERCEIVING it needs LOS — a brazier behind walls doesn't leak onto the map).
  Applied consistently to: fog tiles (FogOfWar.Recompute litAndSeen), chest tokens, brazier tokens.
- Brazier tokens + chest tokens start hidden (SetActive false); shown only when a party member has
  LOS to their tile (UpdateBrazierVisibility / UpdateChestVisibility).
- Confirmed: undiscovered/walled-off lit rooms, their braziers, and chests all stay hidden until a
  character has LOS into the room; then tiles + tokens reveal together. No leaking through walls.

## Progression — dungeon completion DONE (keystone)
- Completion = all rooms VISITED (any character inside a room's bounds) + all encounters CLEARED
  (markers triggered = won, since surviving to explore means victory). Checked each frame, not in combat.
- On completion: "DUNGEON COMPLETE!" (HUD + log), simple reward (150 gold to leader), SaveActive,
  stop exploring, return to town (Roster) after ~2.5s.
- ExplorationEncounters tracks visitedRooms + dungeonComplete; exposes DungeonComplete for the HUD.
- NEXT progression pieces build on this completion event: descending deeper, leveled dungeons,
  completion rewards (XP), in-dungeon resting + repopulation (+ the prompt-as-signal nuance).

## Run structure — boss + rewards REFINEMENTS (decided)
- A: Non-boss dungeons = normal random layout (as now). BOSS dungeon = ONE largish arena room.
- B: Boss = a hard ENCOUNTER by CR (one big monster OR several tough ones — CR budget decides).
  High-level play / leveling to 20 is a future nuance (may CAP levels — TBD; adds a lot to consider).
- C: Boss reward = gold ~ partyLevel * 500 (big). PLUS after level 5: a low-% roll for a WONDROUS item
  (rare item table is deferred content; the roll mechanic is simple — stub now, real items later).
- D: Boss/run reward also grants enough XP to LEVEL UP any character who hasn't leveled during the run.

## XP & LEVELING — must be ACTIVATED (foundational dependency, surfaced by D)
- The data model EXISTS (Character XP/level, Progression, level-up HP logic from early milestones) but
  in PRACTICE characters don't gain XP or level up during play (encounters grant gold to leader, not XP).
- The ENTIRE progression vision hinges on leveling working: run depth = party level + 2, leveled
  dungeons, boss rewards-as-levels. Party level must actually CHANGE.
- Needs: (1) award XP from encounters + boss to party characters; (2) level-up flow when XP thresholds
  hit (HP increase, etc. — model supports it, needs triggering); (3) surface level/XP in UI.
- => "XP & leveling" is its own foundational piece, and it's a PREREQUISITE for the depth=level+2
  run structure to mean anything. Likely build BEFORE or ALONGSIDE leveled dungeons/boss.

## Encounter difficulty + XP = 2024 XP-BUDGET model (verified, aligns with existing systems)
- 2024 replaced CR-threshold encounter building with an XP BUDGET: difficulty tier (LOW/MODERATE/HIGH,
  replacing easy/medium/hard/deadly) → per-character XP budget by party level × party size → spend on
  monsters by summing their XP values. NO multipliers for group size (removed in 2024). Just sum XP.
- We ALREADY have the pieces (Milestone 5): MonsterStats has XP values; EncounterBudget/EncounterBuilder
  do budget-based building; Difficulty enum exists. The exploration bridge currently BYPASSES this with
  a flat "2-4 random monsters" — we should route exploration encounters through the real budget system.
- XP-AS-REWARD: a defeated monster's XP value (already on MonsterStats) = the XP the party earns.
  Award XP = sum defeated monsters' XP, distribute to party characters. Same numbers used for budgeting.
- DIFFICULTY RAMP (run structure): early dungeons LOW/MODERATE, boss = HIGH (or deliberate over-budget
  spike). Matches 2024 guidance (low/moderate ramping to a climactic high-difficulty boss).
- Leveled dungeons: target a difficulty tier per depth; budget = per-char-by-level × party size; spend
  on monsters. This IS the 2024 model + our existing EncounterBudget.

## No friendly fire on direct attacks (FIXED)
- TryAttack now rejects targets on the attacker's own team (can't sword/bow your own party).
- NUANCE for later: area-of-effect spells (fireball etc.) MAY legitimately catch allies in their
  radius (5e AoE doesn't discriminate). So "no friendly fire" applies to DIRECT/targeted attacks;
  AoE friendly-fire is a separate (intended) consideration when spellcasting is built.

## XP & leveling — design (building now, partially)
- XP ACCRUAL (continuous): defeated monsters' XP (already on MonsterStats) divided among all
  DEPLOYED members (downed included). Accrues to Character.currentXP during the run. If a downed
  character is never revived, their XP is lost with them.
- LEVEL-UP is NOT instant. Earning enough XP sets a PENDING LEVEL-UP state (a character "can level").
  Notification: a `*` by the character's name/card (art-polished later). 
- Level-up is APPLIED on a LONG REST (triggers a level-up screen for any pending character):
  - TOWN long rest (RestService.LongRest exists) → buildable NOW.
  - IN-DUNGEON long rest (DEFERRED rest cluster) → levels up mid-run when built. Player's choice to
    rest in-dungeon (risking monster respawns) vs. wait for town.
  - NEVER during combat.
- So leveling makes long rests more meaningful (heal + level), and feeds the rest-gamble tension.
- BUILD NOW: XP accrual from fights + pending-level-up state + `*` notification + apply-on-town-rest.
  IN-DUNGEON leveling comes with the deferred in-dungeon resting system.
- Run structure (depth = level+2 etc.) reads party level, which now actually changes via this system.

## XP awarding — DONE (accrual)
- MonsterStats.XpReward = explicit xpValue or derived from CR (XpForCR table). Same number = defeat
  reward AND encounter-budget cost (2024 model).
- Combatant carries XpReward (set in SpawnMonster from stats.XpReward). CombatManager tallies
  defeatedEnemyXp as enemies drop; on victory, divides among deployed (LivingMembers) → character.AddXp.
- Character.AddXp now ONLY accrues currentXp (no instant level). QualifiedLevel/LevelUpPending/
  ApplyPendingLevelUp added for deferred leveling. Confirmed: "Awarded N XP" after fights.
- REMAINING for XP/leveling piece: (1) `*` pending-level-up notification on Roster cards;
  (2) apply level-ups on the TOWN long rest (RestService). In-dungeon leveling deferred with rest cluster.

## TODO (near-term)
- Combat floor GRID overlay (tile boundaries) for movement readability — at least during combat.
  Helps planning/testing. Self-contained visual task. (Requested during leveling testing.)
- Replace placeholder Unicode UI glyphs (★ ⚠) with ASCII now (font lacks them); real icons in art pass.

## Monster spawn placement (requested change)
- Currently encounters spawn at ROOM CENTERS only. Want them able to spawn ANYWHERE walkable
  (rooms AND corridors) for variety/ambushes. Change PlaceMarkers to pick from all walkable tiles.
- Consideration: corridor fights are tactically different (cramped). Watch that it's interesting,
  not annoying. May want a room/corridor mix rather than pure-random.

## Movement & positioning rules (2024) — DEFERRED (major combat system)
- BUG NOW: characters/enemies can currently move THROUGH enemies freely. Real rules below.
- OCCUPANCY / "champagne cork": a 5-ft (1-tile) corridor is blocked by the front creature; others
  can't pass through/around a HOSTILE creature's space (unless target is incapacitated, Tiny, or
  2+ size categories different). Front-liner bottlenecks the hall.
- MOVING THROUGH ENEMIES: not allowed unless 2+ size categories different; if allowed, the enemy's
  space is Difficult Terrain (10 ft per 5 ft). Never END movement in an enemy's space.
- MOVING THROUGH ALLIES: allowed, no speed penalty (2024 change). But cannot END turn in an ally's
  space → forced PRONE if you do (unless occupier is Tiny or your size+). 
- OPPORTUNITY ATTACKS: leaving an enemy's reach without Disengage provokes an OA (enemy reaction =
  melee attack). Moving through an ally's square does NOT provoke. Needs a REACTION system.
- DISENGAGE action: spend Action (Rogue: Bonus Action) to move without provoking OAs.
- MELEE BOTTLENECK: only the front character can melee foes ahead, unless rear chars have REACH weapons.
- RANGED-IN-MELEE: shooting while an enemy is adjacent → DISADVANTAGE on the ranged attack.
  (NOTE: distinct from the vision unseen-target disadvantage — two separate rules.)
- HELP action: a rear character grants ADVANTAGE to the front ally's next attack/check.
- SHOVE / GRAPPLE (2024): can shove an enemy back/aside to open a corridor; forced movement via Shove
  doesn't provoke OAs. Grapple to hold. 
- PRONE condition: disadvantage on your attacks; melee attackers within 5 ft get advantage; ranged
  attackers against you get disadvantage. (Need a conditions system.)
- TUMBLE (optional DMG): Action/Bonus Action, Dex(Acrobatics) contest to move through (not end in) a
  foe's space as Difficult Terrain.
- SQUEEZING: a creature in a too-small space (or Large forced into 5-ft corridor) = Difficult Terrain.
- DEPENDENCIES: needs (1) ACTION ECONOMY with named actions (Disengage/Shove/Grapple/Help), (2) a
  REACTION system (opportunity attacks), (3) a CONDITIONS system (Prone, Grappled, Incapacitated),
  (4) creature SIZE categories, (5) reach-weapon property, (6) movement-cost/difficult-terrain in
  pathfinding. Build alongside the broader combat-depth + action-economy work.
- MINIMAL FIRST STEP (cheap, high value): just BLOCK moving through occupied tiles (ally = pass-through-
  but-not-stop; enemy = can't pass). Fixes the "move through enemies" bug without the full system.
  Opportunity attacks / Disengage / Shove etc. come with the action-economy build.

## TODO (near-term, testing/usability)
- DEBUG SPAWN CONTROL: a debug panel (Exploration, toggle key) to force specific monster lineups for
  encounters (pick types + counts, or normal random). For reproducible combat testing without random
  ogre/bugbear stomps. Gate behind a debug flag; won't ship. (Building now.)
- CAMERA CONTROL (near-term priority): currently fixed iso camera makes positioning in combat hard.
  Need pan (WASD/edge/middle-drag), zoom (scroll), maybe rotate. Hurts testing + play. Build soon.

## Camera control — DONE
- CameraController on the Exploration main camera: pan (WASD/arrows + screen-edge), zoom (scroll →
  ortho size, clamped 4–22), recenter-on-selected (F). Keeps the fixed iso ANGLE (only translates +
  zooms) so grid/click mapping is unaffected. Pan axes flattened from camera yaw (screen-aligned);
  pan speed scales with zoom. recenterTarget auto-set by ExplorationManager.SelectToken.
- DEFERRED: 90° snap-rotation (for occlusion / seeing around walls) — add later if needed.
- Confirmed: pan/zoom/recenter work; clicking still targets correct tiles after moving the camera.

## Mixed-level party handling (decided)
- ENCOUNTER BUDGET (2024-correct): sum EACH character's individual per-level XP budget (a L1 + a L3
  contribute their own budgets, added). Don't average for the budget — summing per-PC is the 2024 rule
  and handles mixed levels precisely.
- RUN DEPTH (= party level + 2) and BOSS GOLD (= level × 500): use AVERAGE party level (rounded),
  since these need a single number. AveragePartyLevel helper (rounded, min 1).

## Boss encounter sizing — FULL party strength (decided)
- Boss budget uses the FULL DEPLOYED party's levels (including downed/dead members' levels), NOT just
  living survivors. A depleted party still faces a full-strength boss — that's the climax's tension.
- Rationale: eventually in-dungeon rest/heal/revive lets you arrive at the boss in good shape; choosing
  not to (or failing) is the player's managed risk. The boss doesn't scale down to your losses.
- Boss = Hard difficulty + a spike multiplier, includeBoss:true (one strong monster anchors it),
  in a single big ARENA room. Contrast: normal encounters scale to LIVING party (survivor-sized).
- BUG that surfaced this: by depth 3 the party was 1 survivor, so the survivor-based budget made the
  "boss" trivially small (75 XP, 1 goblin). Fix: boss uses full-party budget.

## Boss encounter sizing — FULL party strength (decided)
- Boss budget uses the FULL DEPLOYED party's levels (including downed/dead members' levels), NOT just
  living survivors. A depleted party still faces a full-strength boss — that's the climax's tension.
- Rationale: eventually in-dungeon rest/heal/revive lets you arrive at the boss in good shape; choosing
  not to (or failing) is the player's managed risk. The boss doesn't scale down to your losses.
- Boss = Hard difficulty + a spike multiplier, includeBoss:true (one strong monster anchors it),
  in a single big ARENA room. Contrast: normal encounters scale to LIVING party (survivor-sized).
- BUG that surfaced this: by depth 3 the party was 1 survivor, so the survivor-based budget made the
  "boss" trivially small (75 XP, 1 goblin). Fix: boss uses full-party budget.

## Dungeon encounter distribution — budget-split model (decided, supersedes fixed count)
- A dungeon has a TOTAL combat budget scaled by party level (per-PC sum) × difficulty × dungeon-size
  factor. Higher level → much bigger total → naturally more/denser encounters (a L10 dungeon is a
  sprawling gauntlet; a L1 dungeon is small). Variety/unpredictability is the point — NOT a fixed
  "3, 3, boss" rhythm.
- The total budget is split into a RANDOM number of encounters of VARIED (uneven) sizes: e.g. an
  8-monster low-CR pack in one room, a solo monster elsewhere, a pair in another. Uneven budget
  partitioning + the builder filling each chunk → the size variety.
- Each chunk is budget-built from the monster pool (EncounterBuilder). XP math stays correct (chunks
  sum to the dungeon budget). Completion still = all rooms visited + all placed encounters cleared.
- DEFERRED refinement: monster PACK BEHAVIOR drives chunking — some monsters swarm (kobolds, rats →
  big packs), others are solo (ogre, owlbear → solo bruiser), gnolls → small groups. Composition
  reflects monster "social" nature, not just math. Wire when monsters have social/behavior traits.
- Boss is separate (single full-party Hard encounter in the arena), unaffected by this split.

## Monsters visible in exploration (motivated by playtest — still deferred, but wanted)
- OBSERVED: a lit room (brazier) visible down a hallway shows the floor/brazier/chests, but NOT the
  monsters — because monsters don't EXIST in exploration yet. Encounters are invisible MARKERS
  (proximity triggers); monster tokens only spawn at trigger time → "ambush from an empty lit room."
- DESIRED (the vision/lighting payoff): monsters are VISIBLE lurking tokens in the dungeon, shown by
  the SAME rule as chests/braziers (lit + any-member LOS, OR selected char's darkvision). You spot them
  from afar in lit/seen areas → decide to approach (trigger combat), avoid, or prepare.
- Turns "ambush from nowhere" into scouting/threat-assessment — rewards the vision & light systems.
- DESIGN DEPTH (why deferred to monster-vision/AI milestone): when do monsters notice/aggro the party?
  do they patrol/move? can you avoid the fight entirely by not approaching? do they ambush from the
  DARK (monster darkvision sees you first)? This is the monster-vision/ambush cluster.
- MINIMAL near-term option: spawn monster tokens at encounter placement (visible, gated by the same
  contents-visibility rule as chests), stationary, and trigger combat when the party gets close —
  so you at least SEE them in lit/seen rooms before engaging. Full aggro/patrol/ambush comes later.

## Boss rewards + run end (building now)
- On BOSS dungeon completion: big gold (avg party level × 500) to leader; "level-everyone" XP =
  ensure each surviving character gains AT LEAST one level's worth (grant enough XP to qualify for
  their NEXT level if they haven't already leveled this run — respects different class XP curves,
  no flattening to a uniform level); stub a post-L5 wondrous-item roll (low %, log only for now);
  then FORCED return to town (no Go Deeper past the boss — run complete).
- Leveling still applies on the TOWN long rest (not auto-applied on boss return) — consistent with
  the D&D mechanic + existing system. (Later: a "Long rest to level up" disclaimer/nudge.)
- Normal (non-boss) completion is unchanged (Go Deeper / Save / Town panel).

## PROGRESSION MILESTONE — COMPLETE
- Dungeon completion (all rooms + encounters) → choice (Town/Descend/Save), TPK no longer false-completes.
- Descending: multi-dungeon runs, party state carries forward (no free heal), depth counter, fog/dungeon
  regenerate cleanly (fog reset ordering fixed: ClearExplored before respawn, Recompute after).
- XP & leveling: defeated-monster XP (by-CR) divided among deployed (downed included, dead excluded);
  deferred level-up applied on TOWN long rest (heal to new max); "* LEVEL UP!" marker on Roster cards.
- Leveled dungeons: encounters routed through XP-budget builder; per-dungeon total budget (party level ×
  difficulty × random 1.5–3.5 mult) split into varied uneven chunks (1..N encounters) → unpredictable,
  scales with level. Difficulty ramps Easy→Standard→Hard by depth.
- Boss: at depth = avgLevel+2, single arena room, ONE full-party-strength Hard encounter (includeBoss).
- Boss rewards + run end: big gold (avgLevel×500), level-everyone XP (each survivor ≥1 level's worth,
  respecting class curves), stubbed post-L5 wondrous-item roll, forced return to town (no descend past boss).
- Core loop COMPLETE: party → depth=level+2 run → escalating budgeted dungeons → boss → rewards → town
  → level up → deeper. The systems are now a GAME.
  
  ## Combat depth — Phase 1: action economy foundation (DONE)
First slice of the combat-depth cluster. Formalizes the per-turn resource budget so all later
named actions (Dash/Dodge/Disengage/Shove/Help) and reactions (opportunity attacks) route through
ONE economy instead of ad-hoc boolean pokes.
- TurnResources = the ACTIVE creature's own-turn budget: movement, action, bonus action, free
  object interaction. Spend via TrySpendAction / TrySpendBonusAction / TrySpendFreeInteraction
  (+ existing movement spend). Reset each BeginTurn.
- KEY ARCHITECTURE: REACTION moved OFF TurnResources ONTO Combatant. A reaction is spent on OTHER
  creatures' turns, so it can't live in the active creature's shared budget. Combatant.ReactionAvailable,
  reset at the start of the combatant's OWN turn (BeginTurn); spend via Combatant.TrySpendReaction().
  This is the hook opportunity attacks will consume.
- Attack commits its action via resources.TrySpendAction() at the moment of resolve (only after
  target/range/weapon validated — no spending the action on an illegal attack).
- DASH = first named action (proof the action→resource routing works): spends the action, adds Speed
  ft to MovementRemaining. Bound to 'D' on a player turn for now (HUD button in the art/UI pass).
- HUD reads added: ActiveBonusActionAvailable, ActiveFreeInteractionAvailable, ActiveReactionAvailable.
- Files touched: TurnResources.cs (rewritten), Combatant.cs (reaction state added),
  CombatManager.cs (reset reaction in BeginTurn; reroute attack spend; RequestDash/Dash; HUD reads).
- DEFERRED to later phases: adv/disadvantage modifier-collection in AttackResolver (phase 2); conditions
  list on Combatant (phase 3); reaction system + opportunity attacks + Disengage, needing step-wise
  movement/path (phase 4); death saves (phase 5); named-action menu Dodge/Disengage/Shove/Grapple/Help
  (phase 6). Spellcasting + typed AoE/saves = combat-depth part 2.
  
  ## Combat↔exploration position handoff + fog (DEFERRED — surfaced during Dash testing)
ROOT CAUSE: combat and the fog/vision system don't communicate. Combat (CombatManager.TryMoveTo)
updates occupancy + token transform but never calls FogOfWar.Recompute — combat runs the grid as a
flat tactical plane (correct per "all combatants visible in combat"), while fog is an exploration concept.
TWO CONSEQUENCES, same cause:
- VISUAL: dashing/moving in combat lands you on tiles still rendered fogged (Unseen/Explored) — combat
  doesn't reveal fog as you move.
- CONSEQUENTIAL: IF combat end-position writes back to exploration, the return-side fog recompute can
  reveal new chests/braziers/monsters/encounter markers (per the Layer-2 contents-visibility rule) that
  weren't visible when the fight began — potentially even triggering new encounters.
OPEN — does combat end-position currently write back to exploration at all? Unknown from combat files;
  depends on the exploration↔combat bridge (NOT yet reviewed).
DESIGN FORK (pick when built):
- A: combat tactical-only — return at pre-combat positions (no surprise reveals; simple).
- B: combat position carries back + fog recompute on return (emergent "fought deeper"; needs a recompute
  + possible trigger check on the combat→exploration handoff).
FILES WHEN BUILT: the exploration manager + return-from-combat bridge (unreviewed), FogOfWar, CombatManager
  (end-position export). NOT part of the combat-depth spine.
  
  ## Live fog + fight-widening in combat (DEFERRED — direction LOCKED: Option B+)
Decided during Dash testing. Combat is NOT a sealed tactical plane — fog/LOS/lighting stay LIVE during
a fight, and moving can WIDEN the current battle. Core to the game's danger/over-extension design:
run from a fight or stray too far and you can reveal — and pull in — more monsters.
THREE consequences (all from the same root: combat currently never recomputes fog/vision):
- VISUAL: moving in combat reveals fog as you go (lighting + LOS + darkvision — systems already built),
  instead of leaving traversed tiles fogged.
- HANDOFF: combat END-position carries back to exploration (Option B); return-side reveal opens new
  map/contents for what's next.
- ESCALATION (the danger lever): a monster newly brought into view during combat movement can be
  inserted into the ACTIVE initiative — the fight grows around an over-extending party. "Stray too far,
  open a bigger battle."
DEPENDS ON (three deferred systems converging — this is combat-depth PART 2+, not the martial spine):
- Live fog/vision recompute INSIDE combat (CombatManager.TryMoveTo → FogOfWar.Recompute; today it never does).
- Monster-vision + lurking monster tokens in the dungeon (deferred monster-vision/ambush milestone).
- DYNAMIC INITIATIVE INSERTION — same machinery as the deferred "staggered-join" note (units joining an
  in-progress encounter, inserted into initiative on arrival). Current combat rolls initiative ONCE for a
  fixed roster; this is the piece that lets the roster grow mid-fight.
OPEN — JOIN TRIGGER: does a lurking monster join when the PARTY sees it, or when IT sees the party?
  Vision is asymmetric (darkvision monster in dark sees unlit party first → ambush). "Monster sees you
  first" is the scarier, on-theme option (over-extend → something you couldn't see joins from the dark);
  lean that way, but needs monster-vision to exist first. Resolve when built.
FILES WHEN BUILT: FogOfWar, CombatManager (live recompute + end-position export + dynamic insert),
  TurnOrder (mid-fight initiative insertion), exploration↔combat bridge (unreviewed), monster-vision system.
  
  ## Live fog + fight-widening — JOIN TRIGGER refinement (behavior-driven, not a fixed rule)
Resolves the open join-trigger question from the note above: it's TWO steps, not one.
- STEP 1 — DETECTION (vision/LOS gate): does anyone see anyone, asymmetrically (who sees whom first).
  Settled — this is the vision system. Mutual or one-sided sighting just OPENS the question.
- STEP 2 — REACTION (per-monster BEHAVIOR, not a combat rule): given detection, what the monster DOES
  is keyed to its nature — the typed monster-behavior/intelligence system (deferred monster-vision/AI
  milestone). Same detection event → opposite outcomes by monster type.
MONSTER-REACTION ARCHETYPES (at least three):
- ATTACK (e.g. zombie — mindless, aggressive, no self-preservation): engages the strayed character now.
  OPEN: separate SIDE initiative (isolated solo duel — you wandered off, now you're alone with it) vs.
  INSERT into the existing initiative (one unified fight, wider roster). Likely depends on party proximity.
  Resolve at AI-milestone time.
- RETREAT-AND-FETCH (e.g. lone kobold — intelligent, social, self-preserving): does NOT widen this fight;
  LEAVES and escalates a FUTURE encounter (runs to its clan). Slow-burn off-screen threat — scarier in a
  different register: something saw you, left, and you don't know what it's bringing back. Ties into the
  patrol/wandering-monster + pack-behavior notes (retreat ≈ activating/spawning a future encounter).
- IGNORE/OBLIVIOUS (e.g. slime/passive per existing AI notes): detection → no reaction.
=> The join mechanic is another CONSUMER of the typed monster-AI system, not its own rule. Flesh out the
  attack-form (side vs. insert) and retreat-to-encounter wiring when monster-AI/vision is built.
  
## Monster AI = "DM-like" via BOUNDED ROSTER + authored behavior traits (direction LOCKED)
VISION: the AI should feel like a DM running the game — situational, reactive, monster-appropriate
(kobold retreats to clan; zombie attacks; slime ignores). FEASIBILITY: yes, BECAUSE of tight scope —
NOT a general creature-reasoner (unbounded trap), but a BOUNDED roster of hand-authored monsters whose
typed BEHAVIOR TRAITS the DM-layer reads + combines per situation. Same pattern as conditions (typed list),
encounter budget (typed pool): a typed data layer + a system that reads the types. Intelligence is in the
AUTHORING + situational selection, not a general reasoner.
SETTING AS SCOPE FILTER: dungeon-crawl setting legitimately excludes most of the 5e bestiary (no forest
dragons, ocean aberrations, political devils). Coherent dungeon ecology: undead, oozes, kobolds/goblinoids,
spiders/vermin, constructs, occasional aberration, boss tier. Exclusion = design discipline (less behavior
to author, roster stays comprehensible). Target ~12–15 well-authored monsters.
ARCHITECTURE WARNING (act on this DURING roster authoring, before the AI milestone):
- Add a BEHAVIOR-TRAIT dimension to MonsterStats (today stats-only: CR/XP/attacks/AC/HP). Axes likely
  include aggression, intelligence, self-preservation, social/pack tendency, preferred tactic — TBD.
- Decide each monster's behavior axis AS you author it, even though nothing reads it yet. Cheap now;
  expensive to retrofit 15 assets later. (Same lesson as reaction-flag placement: data where its
  lifecycle belongs, ahead of the consumer.)
- DO NOT design the full trait schema yet — downstream of conditions + vision; designing cold = guessing.
  Capture the decision; schema comes with the monster-vision/AI milestone.
CONSUMERS of this trait system (already noted): join-trigger reaction archetypes (attack/retreat/ignore),
patrol/wandering monsters, pack-behavior encounter chunking, ambush-from-dark.

## Combat depth — Phase 2: advantage/disadvantage modifier-collection (DONE)
The connective tissue of the cluster: ONE place that decides adv/disadvantage, so every source
(vision, conditions, positioning, Help) is an add-a-source call, never a resolver change.
- AttackContext (new): collects advantage/disadvantage SOURCES, each with a reason string for the log.
  Resolves the 5e CANCELLATION rule — any adv + any dis = flat, regardless of counts (not a counter, two
  flags). NetRollMode → Flat/Advantage/Disadvantage; DescribeNet() for the log.
- AttackResolver.Resolve gains an optional AttackContext param (null = flat → all existing callers still
  compile). The single Dice.Roll(20) becomes mode-aware: flat = one die; adv = roll 2 keep higher; dis =
  roll 2 keep lower. AttackResult gains rollMode + otherRoll (the dropped die) for transparent logging.
- SOURCE DECISIONS live in CombatManager.AddAttackModifiers (NOT the resolver) — game state decides
  adv/disadv, resolver stays pure rules. Enemy attacks route through the same TryAttack, so they get
  every source for free.
- WIRED THIS PHASE: Rule 1 ranged-in-melee (ranged weapon + hostile within 5 ft = disadvantage,
  vision-independent). EnemyWithinReach(self, feet) helper added (reused later by reaction/positioning).
- STUBBED IN PLACE (commented hooks in AddAttackModifiers): Rule 2 unseen-target / attack-from-darkness
  (vision hook — wire when vision→combat lands); prone + other conditions (phase 3).
- Files touched: AttackContext.cs (new), AttackResolver.cs (mode-aware roll + result fields),
  CombatManager.cs (build context in TryAttack; AddAttackModifiers; EnemyWithinReach; roll-mode log tag).
- NEXT: vision→combat hook (make unseen-target real — the finished Vision system's combat payoff), then
  phase 3 conditions list on Combatant (Prone the first instance, reusing these stubbed hooks).
  
  ## Combat darkness — rendering vs. rules MISMATCH (DEFERRED, surfaced testing phase 2b)
Combat lights the dungeon visually (the player sees the whole lit room when a fight starts), but CanSee
computes vision by the RULES as if dark (baseline 1 tile + darkvision + LOS; no lighting — FogOfWar.IsLit
absent in combat). So the unseen-target rule fires (rules say dark) while the SCREEN shows it lit — math
and visuals disagree.
DESIGN LEAN: combat should be DARK too (respect fog/darkvision/torches in combat), else the entire vision
system + the phase-2b unseen-target rule is invisible/pointless once a fight begins. Thematic + makes the
vision investment pay off in combat.
PARTS (same deferred combat↔exploration cluster):
- Run fog/darkness RENDERING in combat (combat-side fog, not just the flat lit plane today).
- Close the lighting SEAM in CanSee (lit + LOS = seen) — needs lit-tile data in combat (the deferred
  lighting-in-combat note).
- Together: what the player SEES matches what CanSee computes.
NOT the combat-depth spine; build with the bridge/fog-in-combat work. Capture now so phase 2b's invisibility
on a lit screen is understood, not mistaken for a bug.
## GENRE LOCK: semi-tactical RPG / survival horror (design compass)
The game's identity, named explicitly. NOT a power-fantasy tactical RPG — survival horror with tactical
combat. Retroactively justifies decisions already made on instinct; guides future forks.
SURVIVAL-HORROR GRAMMAR (scarcity / vulnerability / dread) — already present:
- Harsh near-blind darkness + precious light (torch consumable, opportunity cost).
- Permadeath on TPK; town-only saves (committed once you descend); fallen bodies maybe never recovered.
- Dungeon repopulates behind you on rest; missing "Dungeon Complete" prompt = a WARNING, not silence.
- Over-extension can widen a fight / wake worse things; retreat-to-fetch slow-burn threats.
COMPASS RULE: when a fork is ambiguous, pick the option that PRESERVES TENSION over the one that grants
POWER or CONVENIENCE. Already-parked decisions all point this way:
- Darkvision → TAX it (+ magical darkness), don't let it trivialize the dark.
- Monster join-trigger → "monster sees you first, joins from the dark" over a clean symmetric rule.
- Kobold retreat-to-clan → off-screen escalating dread over an immediate fightable thing.
PRODUCTIVE TENSION TO MANAGE: "semi-tactical" (mastery, legibility, fair readable rules — the combat-depth
spine) vs. "survival horror" (scarcity, dread, cruelty). NOT a contradiction — the tactics make the horror
FAIR. Players should lose to understood bad decisions, not unseen dice. Visible modifiers / honest math
(adv-disadv log, action economy) EARN the right to be cruel with the horror layer.
SUSPECT: anything that makes the player feel safe + powerful. That's where horror dies.
## DESIGN PERSONA: the malicious-but-fair DM (refines the genre compass)
The game should feel like a dungeon crawl run by a DM bent on killing your party — who NEVER cheats.
Both clauses essential; the tension between them IS the game.
- MALICIOUS: takes no pity, exploits every legal advantage the rules grant monsters, engineers suffering.
  Reframes monster AI from "tactically competent" to "intelligently CRUEL within the rules" — ask not
  "what's balanced?" but "what would a ruthless DM do with these monsters + this rule set to make the
  party suffer, LEGALLY?"
- FAIR / TRUE TO THE RULES: never fudges. No information or actions the rules don't grant — monsters
  don't see through walls/fog, don't meta-target the wizard by peeking at hit dice (only by legal cues:
  saw them cast, visible robes, inference a smart creature could make). No invented rolls, no
  unavoidable "rocks fall." Every death is one the player could have seen coming.
WHY FAIR = THE SOURCE OF THE MALICE'S POWER (not a limit on it): a rule-breaking malicious DM is just
unfair → rage-quit. A rule-TRUE malicious DM is terrifying → the player loses and thinks "I should've
brought more torches / not split the party / retreated," never "the game cheated." The constraint makes
the threat credible.
CRUELTY EXPRESSED THROUGH LEGAL MOVES (examples, all already parked):
- Kobold RETREATS to fetch the clan (legal, worse for you) vs. attacks and dies.
- Blind-fire into darkness can hit your OWN party (the rule resolved honestly) vs. a quiet whiff.
- Dungeon REPOPULATES the cleared room during rest; missing completion prompt = the only warning.
HARD CONSTRAINT ON THE AI (on US): the DM-AI operates ONLY on legal information + legal actions. The
instant it cheats (fog it shouldn't see, rolls it shouldn't make), horror → unfairness. Auditable:
every AI decision should trace to rules-legal inputs.

## POSITIONING / HOOK — market gap (recorded to consider for the pitch)
Checked the landscape: the GENRE is crowded, but the specific CONTRACT is open.
WHAT EXISTS (tactical turn-based + survival horror + darkness + permadeath is NOT virgin territory):
- Darkest Dungeon = the flagship (turn-based, permadeath, encroaching-dark mechanic, flawed-hero roster).
  But runs on a BESPOKE stress/position system — not a real ruleset; achieves dread via OPACITY + punishing
  randomness (sometimes feel-bad — exactly what our "fair" clause rejects).
- Active indie wave: LURKS WITHIN WALLS ("turn-based RPG x Resident Evil"), Stoneshard, Quasimorph, etc.
  All use CUSTOM horror-tuned combat math. Dread via opaque + punishing.
THE GAP (our differentiator, unoccupied in the search): nobody marries a FAITHFUL TABLETOP RULESET +
TRANSPARENT math + a MALICIOUS-BUT-FAIR DM persona. Our bet inverts the genre norm — dread through
TRANSPARENCY, not obscurity: real D&D 5e rules, visible advantage/disadvantage, honest dice; the horror
is a ruthless intelligence exploiting rules you CAN see, not hidden math you can't.
- Dark Souls proved fair-brutality sells ("hard but never cheap, every death is your fault"). Darkest
  Dungeon proved turn-based horror sells. NOBODY combined fair-brutality + turn-based horror + a real
  tabletop ruleset. That marriage is the hook.
- The "why play this over Darkest Dungeon?" answer is concrete: because it's FAIR, and it's actually D&D.
TWO CAUTIONS (honest):
- 5e-fidelity cuts both ways: it's the differentiator AND a constraint — 5e is a heroic power-fantasy
  system (bounded accuracy, characters get tanky). We're bending it toward horror (taxed darkvision,
  harsh-darkness baseline). Real work, not free — "fighting our own ruleset's optimism."
- "DM-like AI" is the novel-sounding part AND the likeliest vaporware if overscoped. The bounded-roster
  discipline (12–15 monsters, authored traits, no general reasoner) is what makes it shippable. The
  differentiation lives there → protecting its scope protects the whole pitch.
- Files touched: (none — design/positioning note.)

## Combat depth — Phase 3 Prone: UNRESOLVED, troubleshoot next session (pickup spot)
STATE: Framework + Prone built and compiles clean. Debug 'P' toggles Prone (confirmed applying — log shows
"X is now Prone"). BUT no Prone adv/disadv tag appeared on any resolved attack across several tries.
AMBIGUITY (the exact thing to resolve first): can't tell from output whether (a) the Prone hook fires but
the tag prints to log lines lacking {rollTag} — a LOGGING gap (same issue that bit phase 2 + 2b twice), or
(b) the hook genuinely isn't firing — a LOGIC bug in AddAttackModifiers.
FIRST STEP TOMORROW: re-add the unconditional diagnostic in TryAttack, immediately after the
AttackResolver.Resolve(...) call:
    Debug.Log($"[roll] net={ctx.DescribeNet()} mode={res.rollMode} kept={res.attackRoll} dropped={res.otherRoll}");
Then attack a prone target once and read the [roll] line:
  - net=disadvantage/advantage (target prone ...) => hook WORKS, problem is logging → add {rollTag} to the
    plain hit + miss + crit log lines in TryAttack (the crit line gap is what hid advantage in phase 2b).
  - net=flat => hook NOT firing → debug the Prone block in AddAttackModifiers.
TEST-SETUP GOTCHA that muddied tonight's read: s4 still has the BOW (rangeFeet 80 = ranged), so every s4
attack on a prone target is the DISADVANTAGE (ranged) case — the advantage (melee) case CANNOT be tested
with s4. To see advantage, use a combatant holding a MELEE weapon (rangeFeet <=5) adjacent to a prone target.
LIKELY

## PROCESS CONVENTIONS (survive context resets — re-read at session start)
- Close every milestone/stopping point with explicit git add + commit + tag commands. Tag name
  encodes the state (e.g. phase-3-prone-unverified). Commit message records what's done + what's
  left (TODO next session).
- Every Design_Notes entry includes a "Files touched:" line (every script created/modified).
- Phased delivery within milestones: named phases, explicit handoff/verify condition before proceeding.
- Orient against real code (paste the actual files) before designing a new system.
- These conventions live in the conversation by default and DON'T survive a history clear — so they're
  recorded HERE. If a session starts fresh from Design_Notes, re-read this block.
  