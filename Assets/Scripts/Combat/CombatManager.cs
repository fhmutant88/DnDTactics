using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using DnDTactics.Rules;
using DnDTactics.Data;
using DnDTactics.Characters;

namespace DnDTactics.Combat
{
    // Owns the battle: the grid reference, cell occupancy, and selection.
    // Runs AFTER GridVisualizer so the grid exists when we place combatants.
    [DefaultExecutionOrder(100)]
    public class CombatManager : MonoBehaviour
    {
        [Header("Setup")]
        public GridVisualizer gridVisualizer;
        public float tokenYOffset = 0.9f;

        [Header("Test content (drag assets to spawn combatants)")]
        public Species species;
        public CharacterClass playerClass;
        public CharacterClass enemyClass;
        public Background background;

        [Header("Procedural setup (optional — set by EncounterSetup)")]
        public bool useExternalSetup = false;   // when true, skip the hardcoded test spawns

        [Header("Weapons (drag assets)")]
        public Weapon playerWeapon;
        public Weapon enemyWeapon;

        [Header("Colors")]
        public Color playerColor = new Color(0.25f, 0.5f, 0.9f);
        public Color enemyColor = new Color(0.85f, 0.3f, 0.3f);

        [Header("Rewards")]
        public int encounterGoldBase = 0;   // set by EncounterSetup from the encounter's XP
        private bool rewardsGranted = false;

        [Header("Encounter lifecycle")]
        public bool startDormant = false;   // exploration sets this; combat waits to be triggered
        private bool encounterConcluded = false;
        private bool encounterRunning = false;   // ← add this line here
        private int defeatedEnemyXp = 0;

        // Fires once when the encounter resolves. bool = player victory.
        public event System.Action<bool> OnEncounterEnded;

        private TacticalGrid grid;
        private readonly Dictionary<GridCoord, Combatant> occupancy = new();
        private readonly List<Combatant> combatants = new();
        private Combatant selected;
        private TurnOrder turnOrder = new TurnOrder();
       
        [Header("Movement")]
        public CellHighlighter highlighter;
        private TurnResources resources = new TurnResources();
        private Dictionary<GridCoord, int> reachable = new();

        private readonly List<string> combatDeadMemberIds = new();  // heroes who died via death saves

        private bool awaitingProvokeConfirm = false;
        private GridCoord pendingMoveTarget;
        private int pendingMoveCost;
        private List<Combatant> pendingProvokers;

        // External setup: provide the grid the combat runs on.
        public void SetGrid(TacticalGrid externalGrid)
        {
            grid = externalGrid;
        }

        // External setup: spawn a player character at a coord (real Character, not test).
        public Combatant SpawnPlayer(Character character, GridCoord coord)
        {
            return SpawnExisting(character, Team.Player, coord, playerColor, playerWeapon);
        }

        // Spawn a deployed party hero, remembering which barracks member it is.
        public Combatant SpawnPartyHero(Character character, GridCoord coord, string memberId)
        {
            var c = SpawnExisting(character, Team.Player, coord, playerColor, playerWeapon);
            if (c != null) c.SetBarracksMemberId(memberId);
            return c;
        }

        // External setup: spawn a monster from MonsterStats at a coord.
        public Combatant SpawnMonster(DnDTactics.Data.MonsterStats stats, GridCoord coord)
        {
            var character = MonsterAdapter.ToCharacter(stats);
            var c = SpawnExisting(character, Team.Enemy, coord, enemyColor, enemyWeapon);
            if (c != null)
            {
                c.SetXpReward(stats.XpReward);
                if (stats.onHitAbility != null && stats.onHitAbility.enabled)
                    c.SetOnHitAbility(stats.onHitAbility);
            }
            return c;
        }

        // Shared spawn for pre-built characters (vs. the test BuildTestCharacter path).
        Combatant SpawnExisting(Character character, Team team, GridCoord coord,
                                Color color, Weapon weapon)
        {
            if (grid == null || !grid.InBounds(coord) || occupancy.ContainsKey(coord))
            {
                Debug.LogWarning($"Can't spawn {character.characterName} at {coord}.");
                return null;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"Combatant_{character.characterName}";
            go.transform.SetParent(transform);
            go.transform.localScale = new Vector3(0.7f, 0.8f, 0.7f);

            var combatant = go.AddComponent<Combatant>();
            combatant.Initialize(character, team, coord, color);
            combatant.SetWeapon(weapon);
            combatant.SetCoord(coord, grid, tokenYOffset);

            occupancy[coord] = combatant;
            combatants.Add(combatant);
            return combatant;
        }

        // External setup: once all combatants are spawned, start the fight.
        public void StartExternalEncounter()
        {
            if (combatants.Count == 0) { Debug.LogError("No combatants to fight."); return; }
            encounterRunning = true;
            encounterConcluded = false;
            rewardsGranted = false;
            defeatedEnemyXp = 0;
            BeginEncounter();
        }

        void Start()
        {
            Debug.Log($"CombatManager.Start on '{gameObject.name}' — startDormant={startDormant}");
            if (startDormant) return; // exploration drives setup/start manually

            if (gridVisualizer != null) grid = gridVisualizer.Grid;
            if (grid == null)
            {
                Debug.LogError("CombatManager: no grid. Assign GridVisualizer in the Inspector.");
                return;
            }

            if (!useExternalSetup)
            {
                SpawnCombatant("Hero A", playerClass, Team.Player, new GridCoord(2, 1));
                SpawnCombatant("Hero B", playerClass, Team.Player, new GridCoord(3, 1));
                SpawnCombatant("Enemy A", enemyClass, Team.Enemy, new GridCoord(6, 8));
                SpawnCombatant("Enemy B", enemyClass, Team.Enemy, new GridCoord(7, 8));
                BeginEncounter();
            }
            // when useExternalSetup is true, EncounterSetup calls our public API instead
        }

        void Update()
        {
            if (!encounterRunning) return; // ignore combat input outside an active encounter
            if (awaitingProvokeConfirm)
            {
                if (Input.GetKeyDown(KeyCode.Y))
                {
                    awaitingProvokeConfirm = false;
                    var mover = turnOrder.Current?.combatant;
                    if (mover != null) CommitMove(mover, pendingMoveTarget, pendingMoveCost, pendingProvokers);
                }
                else if (Input.GetKeyDown(KeyCode.N))
                {
                    awaitingProvokeConfirm = false;
                    Debug.Log("Move cancelled.");
                }
                return;   // swallow other input while waiting
            }

            var active = turnOrder.Current?.combatant;
            bool playerTurn = active != null && active.Team == Team.Player;
            
            if (Input.GetMouseButtonDown(0) && !PointerOverUI()) HandleClick();
            if (playerTurn && Input.GetMouseButtonDown(1) && !PointerOverUI()) HandleAttackClick();
            if (playerTurn && Input.GetKeyDown(KeyCode.Space)) EndTurn();
            if (playerTurn && Input.GetKeyDown(KeyCode.D)) RequestDash();
            if (playerTurn && Input.GetKeyDown(KeyCode.G)) RequestDisengage();
            if (Input.GetKeyDown(KeyCode.P)) DebugToggleProne();
            if (Input.GetKeyDown(KeyCode.L)) DebugToggleParalyzed();
            if (Input.GetKeyDown(KeyCode.K)) DebugRollSave();


        }

        // True when the mouse is over a UI element, so game clicks don't bleed through it.
        bool PointerOverUI() =>
            UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        void HandleClick()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 200f)) return;

            // If we hit a combatant, select it (inspect). Otherwise treat as a ground click.
            var clicked = hit.collider.GetComponent<Combatant>();
            if (clicked != null) { Select(clicked); return; }

            GridCoord cell = grid.WorldToCoord(hit.point);
            if (grid.InBounds(cell)) TryMoveTo(cell);
        }

        void Select(Combatant c)
        {
            if (selected == c) return;
            if (selected != null) selected.SetSelected(false);
            selected = c;
            if (selected != null)
            {
                selected.SetSelected(true);
                Character ch = selected.Character;
                Debug.Log($"Selected {ch.characterName} ({selected.Team}) at {selected.Coord} — " +
                          $"HP {ch.currentHP}/{ch.MaxHP}, AC {ch.ArmorClass}");
            }
        }

        void SpawnCombatant(string name, CharacterClass cls, Team team, GridCoord coord)
        {
            if (!grid.InBounds(coord) || occupancy.ContainsKey(coord))
            {
                Debug.LogWarning($"Can't spawn {name} at {coord} (out of bounds or occupied).");
                return;
            }

            Character character = BuildTestCharacter(name, cls);

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"Combatant_{name}";
            go.transform.SetParent(transform);
            go.transform.localScale = new Vector3(0.7f, 0.8f, 0.7f);

            var combatant = go.AddComponent<Combatant>();
            combatant.Initialize(character, team, coord, team == Team.Player ? playerColor : enemyColor);
            combatant.SetWeapon(team == Team.Player ? playerWeapon : enemyWeapon);
            combatant.SetCoord(coord, grid, tokenYOffset);

            occupancy[coord] = combatant;
            combatants.Add(combatant);
        }

        Character BuildTestCharacter(string name, CharacterClass cls)
        {
            var abilities = new AbilityScoreSet();
            int[] array = AbilityScoreGeneration.StandardArray();
            Ability[] all = (Ability[])System.Enum.GetValues(typeof(Ability));
            for (int i = 0; i < all.Length; i++) abilities.SetBaseScore(all[i], array[i]);
            return new Character(name, species, cls, background, abilities, 1);
        }

        // Occupancy queries — movement (next pieces) will lean on these.
        public bool IsOccupied(GridCoord c) => occupancy.ContainsKey(c);
        public Combatant GetOccupant(GridCoord c) => occupancy.TryGetValue(c, out var x) ? x : null;

        // ---- HUD read access (additive) ----
        public IReadOnlyList<Combatant> Combatants => combatants;
        public Combatant Selected => selected;
        public Combatant ActiveCombatant => turnOrder?.Current?.combatant;
        public int CurrentRound => turnOrder != null ? turnOrder.Round : 0;
        public bool IsPlayerTurn => ActiveCombatant != null && ActiveCombatant.Team == Team.Player;
        public int ActiveMovementRemaining => resources != null ? resources.MovementRemaining : 0;
        public bool ActiveActionAvailable => resources != null && resources.ActionAvailable;

        // The End Turn button calls this; enemies still end their own turns.
        public void RequestEndTurn()
        {
            if (IsPlayerTurn) EndTurn();
        }

        // Dash: spend your action to gain extra movement equal to your Speed (5e).
        // First "named action" — proves the action→resource routing the menu will reuse.
        public void RequestDash()
        {
            if (!IsPlayerTurn) return;
            var active = turnOrder.Current?.combatant;
            if (active == null) return;
            if (!resources.TrySpendAction()) { Debug.Log("No action left to Dash."); return; }

            int speed = active.Character.Speed;
            resources.MovementRemaining += speed;
            Debug.Log($"{active.Character.characterName} Dashes (+{speed} ft). " +
                      $"Movement: {resources.MovementRemaining} ft.");
            RefreshMovementRange();
        }

        // Disengage: spend your action so your movement doesn't provoke opportunity attacks this turn.
        public void RequestDisengage()
        {
            if (!IsPlayerTurn) return;
            var active = turnOrder.Current?.combatant;
            if (active == null) return;
            if (active.DisengagedThisTurn) { Debug.Log("Already Disengaged this turn."); return; }
            if (!resources.TrySpendAction()) { Debug.Log("No action left to Disengage."); return; }
            active.SetDisengaged(true);
            Debug.Log($"{active.Character.characterName} Disengages — movement won't provoke this turn.");
        }

        // DEBUG: toggle Prone on the selected combatant, to exercise the conditions→attack
        // hooks until a real applier (Shove / drop-prone action) exists. Remove when those land.
        void DebugToggleProne()
        {
            if (selected == null) { Debug.Log("No combatant selected to toggle Prone."); return; }
            if (selected.HasCondition(ConditionType.Prone))
            {
                selected.RemoveCondition(ConditionType.Prone);
                Debug.Log($"{selected.Character.characterName} is no longer Prone.");
            }
            else
            {
                selected.AddCondition(ConditionType.Prone);
                Debug.Log($"{selected.Character.characterName} is now Prone.");
            }
        }

        // Names the condition removing a creature's turn, for the skip log.
        string DescribeIncapacitation(Combatant c)
        {
            if (c.HasCondition(ConditionType.Paralyzed)) return "Paralyzed";
            if (c.HasCondition(ConditionType.Stunned)) return "Stunned";
            if (c.HasCondition(ConditionType.Unconscious)) return "Unconscious";
            return "incapacitated";
        }

        // DEBUG: toggle Paralyzed on the selected combatant (until Ghoul's attack applies it for real).
        void DebugToggleParalyzed()
        {
            if (selected == null) { Debug.Log("No combatant selected to toggle Paralyzed."); return; }
            if (selected.HasCondition(ConditionType.Paralyzed))
            {
                selected.RemoveCondition(ConditionType.Paralyzed);
                Debug.Log($"{selected.Character.characterName} is no longer Paralyzed.");
            }
            else
            {
                selected.AddCondition(ConditionType.Paralyzed);
                Debug.Log($"{selected.Character.characterName} is now Paralyzed.");
            }
        }

        // DEBUG: roll a Dexterity save (DC 13) for the selected combatant, to exercise SaveResolver
        // until real ability-imposed saves exist. Shows proficiency + the paralyzed auto-fail.
        void DebugRollSave()
        {
            if (selected == null) { Debug.Log("No combatant selected to roll a save."); return; }
            const int dc = 13;
            var result = SaveResolver.Resolve(selected, DnDTactics.Rules.Ability.Dexterity, dc);
            string name = selected.Character.characterName;
            if (result.autoFailed)
                Debug.Log($"{name} auto-FAILS the DC {dc} Dex save (incapacitated).");
            else
                Debug.Log($"{name} {(result.success ? "SUCCEEDS" : "FAILS")} the DC {dc} Dex save " +
                          $"(rolled {result.roll} + mods = {result.total} vs DC {dc}).");
        }

        // Additive HUD reads for the new resources.
        public bool ActiveBonusActionAvailable => resources != null && resources.BonusActionAvailable;
        public bool ActiveFreeInteractionAvailable => resources != null && resources.FreeInteractionAvailable;
        public bool ActiveReactionAvailable => ActiveCombatant != null && ActiveCombatant.ReactionAvailable;

        void LogInitiative()
        {
            Debug.Log("=== Initiative Order ===");
            int i = 1;
            foreach (var e in turnOrder.Entries)
                Debug.Log($"{i++}. {e.combatant.Character.characterName} " +
                          $"({e.combatant.Team}) — {e.total} (rolled {e.roll}, Dex {e.dexModifier:+0;-0;0})");
        }

        void BeginEncounter()
        {
            turnOrder.Roll(combatants);
            LogInitiative();
            BeginTurn();
        }

        void BeginTurn()
        {
            var entry = turnOrder.Current;
            if (entry == null) return;

            foreach (var c in combatants) c.SetSelected(c == entry.combatant);
            selected = entry.combatant;

            resources.ResetForTurn(entry.combatant.Character.Speed);
            entry.combatant.ResetReaction();      // reaction refreshes at the start of your own turn
            entry.combatant.SetDisengaged(false);   // Disengage lasts only your own turn
            entry.combatant.TickConditions();     // durationed conditions count down (Prone is permanent)
            // Dying hero rolls a death save at the start of their turn (before the incap skip).
            if (entry.combatant.Death.IsDying)
            {
                RollDeathSave(entry.combatant);
                // If the save killed them or left them down, the incapacitation check below
                // (Unconscious) skips the rest of the turn. If a nat 20 revived them, they're
                // conscious now and take a normal turn.
            }

            // Repeating-save conditions (e.g. Ghoul paralysis): the afflicted creature re-rolls at
            // its turn to shake them off. Runs even though a paralyzed creature is then skipped —
            // the save still happens. Success ends the condition (may un-skip the turn).
            foreach (var (type, escaped) in entry.combatant.ProcessEscapeSaves())
                Debug.Log($"{entry.combatant.Character.characterName} " +
                          (escaped ? $"shakes off {type}!" : $"is still {type} (failed the escape save)."));
            RefreshMovementRange();

            // Incapacitated (Paralyzed/Stunned/Unconscious) → no actions or movement; skip the turn.
            if (entry.combatant.IsIncapacitated)
            {
                Debug.Log($"{entry.combatant.Character.characterName} is incapacitated " +
                          $"({DescribeIncapacitation(entry.combatant)}) — turn skipped.");
                EndTurn();
                return;
            }

            Debug.Log($"--- Round {turnOrder.Round}: {entry.combatant.Character.characterName}'s turn " +
                      $"({entry.combatant.Team}) — Speed {entry.combatant.Character.Speed} ft ---");

            // Enemies act automatically; players wait for input.
            if (entry.combatant.Team == Team.Enemy)
                StartCoroutine(RunEnemyTurn(entry.combatant));
        }

        // Each provoked attacker spends its reaction for one melee attack against the mover.
        // Called AFTER the mover reaches `to` (5e: the OA happens as you leave, target still adjacent
        // at the moment of the attack — we resolve from the attacker's cell against the mover).
        void ResolveOpportunityAttacks(List<Combatant> attackers, Combatant mover)
        {
            foreach (var oa in attackers)
            {
                if (oa == null || !oa.ReactionAvailable) continue;
                if (!oa.TrySpendReaction()) continue;
                Debug.Log($"*** {oa.Character.characterName} takes an opportunity attack " +
                          $"on {mover.Character.characterName}! ***");
                TryAttackAsReaction(oa, mover);
                if (mover.Character.IsDown) break;   // mover dropped mid-flight; stop
            }
        }

        // An attack made as a reaction (opportunity attack): no action cost (the reaction was already
        // spent), no range/team re-validation beyond what the OA check guaranteed. Reuses the full
        // AttackResolver pipeline so adv/disadv (incl. mover's conditions) all apply.
        void TryAttackAsReaction(Combatant attacker, Combatant target)
        {
            if (attacker.Weapon == null) return;

            var ctx = new AttackContext();
            int distFeet = attacker.Coord.DistanceInFeet(target.Coord);
            AddAttackModifiers(ctx, attacker, target, distFeet);

            AttackResult res = AttackResolver.Resolve(
                attacker.Character, target.Character, attacker.Weapon, ctx);

            string rollTag = res.rollMode == RollMode.Flat ? "" : $" [{ctx.DescribeNet()}; kept {res.attackRoll}, dropped {res.otherRoll}]";
            string atkName = attacker.Character.characterName;
            string defName = target.Character.characterName;

            if (res.critMiss) Debug.Log($"OA: {atkName} rolls a natural 1 — miss!{rollTag}");
            else if (!res.hit) Debug.Log($"OA: {atkName} misses {defName} ({res.attackTotal} vs AC {res.targetAC}).{rollTag}");
            else
            {
                // Damage to a DYING hero doesn't reduce HP (already 0) — it's a death-save
                // failure instead (a crit = two). The "monster finishes the fallen" rule.
                if (target.Death.IsDying)
                {
                    int fails = res.crit ? 2 : 1;
                    target.Death.AddFailure(fails);
                    Debug.Log($"{atkName} strikes the dying {defName} — " +
                              $"{fails} death-save failure(s)! ({target.Death.Failures}/3)");
                    if (target.Death.IsDead)
                    {
                        Debug.Log($"*** {defName} has DIED. ***");
                        KillDyingHero(target);
                    }
                    RefreshMovementRange();
                    return;
                }

                target.Character.TakeDamage(res.damage);
                string critTag = res.crit ? " CRIT!" : "";
                Debug.Log($"OA: {atkName} hits {defName}{critTag} for {res.damage}. " +
                          $"{defName} HP: {target.Character.currentHP}/{target.Character.MaxHP}{rollTag}");
                if (target.Character.IsDown) DropCombatant(target);
                else TryApplyOnHitAbility(attacker, target);
            }
        }

        IEnumerator RunEnemyTurn(Combatant enemy)
        {
            yield return new WaitForSeconds(0.5f); // brief pause so you can see the turn begin

            // Enemy may have been removed before acting.
            if (enemy == null || enemy.Character.IsDown) { EndTurn(); yield break; }

            var plan = EnemyAI.Decide(
                enemy, combatants, resources.MovementRemaining, grid,
                coord => IsOccupied(coord));

            // Move toward the chosen cell (if the plan says to move).
            if (plan.moveTo != enemy.Coord)
            {
                TryMoveTo(plan.moveTo);
                yield return new WaitForSeconds(0.4f);
            }

            // Attack if a target is now within reach and the action is still available.
            if (plan.target != null && resources.ActionAvailable)
            {
                int dist = enemy.Coord.DistanceInFeet(plan.target.Coord);
                int reach = enemy.Weapon != null ? enemy.Weapon.rangeFeet : 5;
                if (dist <= reach)
                {
                    TryAttack(enemy, plan.target);
                    yield return new WaitForSeconds(0.4f);
                }
            }

            yield return new WaitForSeconds(0.3f);
            EndTurn(); // enemy hands the turn back automatically
        }

        void RefreshMovementRange()
        {
            var active = turnOrder.Current?.combatant;
            if (active == null) { if (highlighter) highlighter.Clear(); return; }

            reachable = MovementRange.Reachable(
                active.Coord,
                resources.MovementRemaining,
                grid,
                coord => IsOccupied(coord));   // live occupancy as the block test

            if (highlighter) highlighter.Show(reachable.Keys, grid);
        }

        void TryMoveTo(GridCoord target)
        {
            var active = turnOrder.Current?.combatant;
            if (active == null) return;
            if (!reachable.TryGetValue(target, out int cost)) return;
            if (!resources.CanSpendMovement(cost)) return;

            var provokers = ProvokedAttackers(active, active.Coord, target);

            // Player moves that provoke → warn and wait for confirmation (fair-DM: informed choice).
            // Enemy moves → the AI already decided; resolve without a prompt.
            if (provokers.Count > 0 && active.Team == Team.Player && !awaitingProvokeConfirm)
            {
                pendingMoveTarget = target;
                pendingMoveCost = cost;
                pendingProvokers = provokers;
                awaitingProvokeConfirm = true;
                string who = string.Join(", ", provokers.ConvertAll(p => p.Character.characterName));
                Debug.Log($"⚠ Moving there provokes an opportunity attack from: {who}. " +
                          $"Press Y to proceed, N to cancel.");
                return;   // wait for the player's Y/N (handled in Update)
            }

            CommitMove(active, target, cost, provokers);
        }

        // Actually perform the move and resolve any opportunity attacks it provokes.
        void CommitMove(Combatant active, GridCoord target, int cost, List<Combatant> provokers)
        {
            occupancy.Remove(active.Coord);
            active.SetCoord(target, grid, tokenYOffset);
            occupancy[active.Coord] = active;
            resources.SpendMovement(cost);

            Debug.Log($"{active.Character.characterName} moved to {target} ({cost} ft). " +
                      $"Movement left: {resources.MovementRemaining} ft.");

            if (provokers != null && provokers.Count > 0)
                ResolveOpportunityAttacks(provokers, active);

            RefreshMovementRange();
        }

        void EndTurn()
        {
            turnOrder.Advance();
            BeginTurn();
        }

        // Right-click to attack the combatant under the cursor.
        void HandleAttackClick()
        {
            var active = turnOrder.Current?.combatant;
            if (active == null) return;
            if (!resources.ActionAvailable) { Debug.Log("No action left this turn."); return; }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 200f)) return;
            var targetCombatant = hit.collider.GetComponent<Combatant>();
            if (targetCombatant == null) return;

            TryAttack(active, targetCombatant);
        }

        void TryAttack(Combatant attacker, Combatant target)
        {
            if (target == null || target.Team == attacker.Team)
            {
                Debug.Log("You can only attack enemies.");
                return;
            }

            if (!resources.ActionAvailable)
            {
                Debug.Log("No action left this turn.");
                return;
            }

            int distFeet = attacker.Coord.DistanceInFeet(target.Coord);
            int reach = attacker.Weapon != null ? attacker.Weapon.rangeFeet : 5;
            if (distFeet > reach)
            {
                Debug.Log($"Target out of range ({distFeet} ft > {reach} ft reach).");
                return;
            }

            if (attacker.Weapon == null) { Debug.Log("No weapon equipped."); return; }

            /// Commit the action only now that the attack is valid (target/range/weapon all OK) —
            // never spend your action on an illegal attack. The early guards above already
            // confirmed an action was available.
            resources.TrySpendAction();

            // Collect advantage/disadvantage sources for this attack. Each source is added with
            // a reason for the log; AttackContext resolves the 5e cancellation rule. Future sources
            // (unseen target, prone, Help, attack-from-darkness) plug in here as more Add* calls.
            var ctx = new AttackContext();
            AddAttackModifiers(ctx, attacker, target, distFeet);

            AttackResult res = AttackResolver.Resolve(
                attacker.Character, target.Character, attacker.Weapon, ctx);
            
            string atkName = attacker.Character.characterName;
            string defName = target.Character.characterName;

            string rollTag = res.rollMode == RollMode.Flat ? "" : $" [{ctx.DescribeNet()}; kept {res.attackRoll}, dropped {res.otherRoll}]";
            if (res.critMiss) { Debug.Log($"{atkName} attacks {defName}: natural 1 — miss!{rollTag}"); }
            else if (!res.hit)
            { Debug.Log($"{atkName} attacks {defName}: {res.attackTotal} vs AC {res.targetAC} — miss.{rollTag}"); }
            else
            {
                if (target.Death.IsDying)
                {
                    int fails = res.crit ? 2 : 1;
                    target.Death.AddFailure(fails);
                    Debug.Log($"OA: {atkName} strikes the dying {defName} — {fails} failure(s)! " +
                              $"({target.Death.Failures}/3)");
                    if (target.Death.IsDead) { Debug.Log($"*** {defName} has DIED. ***"); KillDyingHero(target); }
                    return;
                }

                target.Character.TakeDamage(res.damage);
                string critTag = res.crit ? " CRIT!" : "";
                Debug.Log($"{atkName} hits {defName}{critTag} for {res.damage} " +
                          $"({(res.crit ? "nat 20" : res.attackTotal + " vs AC " + res.targetAC)}). " +
                          $"{defName} HP: {target.Character.currentHP}/{target.Character.MaxHP}{rollTag}");

                if (target.Character.IsDown) DropCombatant(target);
                else TryApplyOnHitAbility(attacker, target);   // rider only matters on a survivor
            }

            RefreshMovementRange();
        }

        // Gathers the advantage/disadvantage sources that apply to one attack. The ONLY place
        // game state decides adv/disadv; AttackResolver stays pure rules. Grows as combat depth
        // lands — each new rule is one Add* line here, never a resolver change.
        void AddAttackModifiers(AttackContext ctx, Combatant attacker, Combatant target, int distFeet)
        {
            // RULE 1 — ranged-in-melee (vision-independent): firing a RANGED weapon while any
            // hostile is within 5 ft = disadvantage. ("Don't shoot a bow in someone's face.")
            bool ranged = attacker.Weapon != null && attacker.Weapon.rangeFeet > 5;
            
            if (ranged && EnemyWithinReach(attacker, 5))
                ctx.AddDisadvantage("ranged with enemy adjacent");

            /// RULE 2 — unseen target / attacking from darkness (vision-driven).
            if (!CanSee(attacker, target))
                ctx.AddDisadvantage("target unseen");
            if (!CanSee(target, attacker))
                ctx.AddAdvantage("attacker unseen");

            // CONDITIONS — Prone (phase 3, first conditions instance).
            // Prone attacker: disadvantage on all its attacks.
            if (attacker.HasCondition(ConditionType.Prone))
                ctx.AddDisadvantage("attacker prone");
            // Prone target: melee attackers get advantage, ranged get disadvantage.
            if (target.HasCondition(ConditionType.Prone))
            {
                if (ranged) ctx.AddDisadvantage("target prone (ranged)");
                else ctx.AddAdvantage("target prone (melee)");
            }

            // CONDITIONS — Paralyzed (target). Attacks against it have advantage; a melee hit
            // from within 5 ft is an automatic crit. ("Can't act" is handled at turn start;
            // auto-fail STR/DEX saves is stubbed until the save system exists.)
            if (target.HasCondition(ConditionType.Paralyzed))
            {
                ctx.AddAdvantage("target paralyzed");
                if (!ranged)  // melee within reach → auto-crit on hit
                    ctx.AddForcedCrit("target paralyzed (melee)");
            }
        }

        // If the attacker is a monster with an on-hit ability, the target rolls the ability's save;
        // on failure, the condition is applied with its authored clear-rule + DC. First real consumer
        // of the save system. (Only heroes can currently suffer these — monsters don't carry a
        // barracks/dying path — but the code is team-agnostic.)
        void TryApplyOnHitAbility(Combatant attacker, Combatant target)
        {
            var ability = attacker.OnHitAbility;
            if (ability == null || !ability.enabled) return;
            if (target.HasCondition(MapCondition(ability.appliesCondition))) return; // already afflicted

            var save = SaveResolver.Resolve(target, ability.saveAbility, ability.saveDC);
            string tName = target.Character.characterName;
            string aName = attacker.Character.characterName;

            if (save.autoFailed || !save.success)
            {
                var type = MapCondition(ability.appliesCondition);
                var clear = MapClearRule(ability.clearRule);
                target.AddCondition(type, clear, ability.durationRounds, aName,
                                    ability.saveAbility, ability.saveDC);
                string how = save.autoFailed
                    ? $"auto-fails the DC {ability.saveDC} {ability.saveAbility} save"
                    : $"fails the DC {ability.saveDC} {ability.saveAbility} save ({save.total})";
                Debug.Log($"{tName} {how} — {type}! (from {aName})");
            }
            else
            {
                Debug.Log($"{tName} resists {aName}'s {MapCondition(ability.appliesCondition)} " +
                          $"(DC {ability.saveDC} {ability.saveAbility} save: {save.total}).");
            }
        }

        // Single translation point: Data-side authoring enums → Combat runtime enums.
        // If the mirror enums ever drift, this fails to compile here (not silently elsewhere).
        static ConditionType MapCondition(DnDTactics.Data.ConditionTypeData d) => d switch
        {
            DnDTactics.Data.ConditionTypeData.Paralyzed => ConditionType.Paralyzed,
            DnDTactics.Data.ConditionTypeData.Prone => ConditionType.Prone,
            DnDTactics.Data.ConditionTypeData.Stunned => ConditionType.Stunned,
            DnDTactics.Data.ConditionTypeData.Restrained => ConditionType.Restrained,
            DnDTactics.Data.ConditionTypeData.Blinded => ConditionType.Blinded,
            DnDTactics.Data.ConditionTypeData.Frightened => ConditionType.Frightened,
            DnDTactics.Data.ConditionTypeData.Poisoned => ConditionType.Poisoned,
            _ => ConditionType.Prone,
        };

        static ClearRule MapClearRule(DnDTactics.Data.ConditionClearData d) => d switch
        {
            DnDTactics.Data.ConditionClearData.RepeatingSave => ClearRule.RepeatingSave,
            DnDTactics.Data.ConditionClearData.DurationRounds => ClearRule.DurationRounds,
            DnDTactics.Data.ConditionClearData.UntilRemoved => ClearRule.UntilRemoved,
            _ => ClearRule.UntilRemoved,
        };

        // True if any hostile (relative to 'self') sits within 'feet' of self. Used for
        // ranged-in-melee; reused later by reaction/positioning rules.
        bool EnemyWithinReach(Combatant self, int feet)
        {
            foreach (var c in combatants)
            {
                if (c == null || c.Team == self.Team) continue;
                if (self.Coord.DistanceInFeet(c.Coord) <= feet) return true;
            }
            return false;
        }

        // Enemies who have an opportunity attack against `mover` for a move from->to:
        // those adjacent (within reach) at the start cell but NOT at the destination —
        // i.e. the mover LEAVES their reach. 5e OA rule, computed by endpoint comparison
        // (no path needed: provoking depends on entering/leaving reach, a start/end property).
        // Returns empty if the mover Disengaged this turn.
        List<Combatant> ProvokedAttackers(Combatant mover, GridCoord from, GridCoord to)
        {
            var result = new List<Combatant>();
            if (mover == null || mover.DisengagedThisTurn) return result;

            foreach (var c in combatants)
            {
                if (c == null || c == mover || c.Team == mover.Team) continue;
                if (c.Character.IsDown) continue;
                if (!c.ReactionAvailable) continue;             // no reaction left → can't OA
                if (c.IsIncapacitated) continue;                // paralyzed/stunned can't react

                int reach = c.Weapon != null ? c.Weapon.rangeFeet : 5;
                // Only melee reach provokes OAs (ranged weapons don't threaten reach).
                if (reach > 5) reach = 5;                        // OA is a melee reaction; cap at melee reach
                bool reachedStart = from.DistanceInFeet(c.Coord) <= reach;
                bool reachesEnd = to.DistanceInFeet(c.Coord) <= reach;
                if (reachedStart && !reachesEnd)
                    result.Add(c);                              // adjacent at start, left at end → provoked
            }
            return result;
        }

        // Can viewer see subject's tile right now? Darkvision + LOS (Vision rules class).
        // LIGHTING SEAM: darkvision/baseline radius only — lit-tile data isn't available in
        // combat yet, so a lit target in the dark still reads as unseen. Deferred.
        bool CanSee(Combatant viewer, Combatant subject)
        {
            if (viewer == null || subject == null || grid == null) return true; // fail open
            int radius = DnDTactics.Rules.Vision.SightRadiusTiles(ViewerDarkvisionFeet(viewer));
            if (!DnDTactics.Rules.Vision.HasLineOfSight(viewer.Coord, subject.Coord, grid))
                return false;
            return ChebyshevTiles(viewer.Coord, subject.Coord) <= radius;
        }

        // Darkvision range (feet) from the combatant's species.
        int ViewerDarkvisionFeet(Combatant c)
        {
            var species = c.Character != null ? c.Character.species : null;
            return species != null ? species.darkvisionRange : 0;
        }

        int ChebyshevTiles(GridCoord a, GridCoord b)
        {
            int dx = a.x - b.x; if (dx < 0) dx = -dx;
            int dz = a.z - b.z; if (dz < 0) dz = -dz;
            return dx > dz ? dx : dz;
        }

        void DropCombatant(Combatant c)
        {
            // Player heroes don't die instantly at 0 HP — they enter the DYING state and linger
            // on the field, rolling death saves. Enemies die immediately as before.
            if (c.Team == Team.Player && !c.Death.IsDead)
            {
                if (!c.Death.IsDying && !c.Death.IsStable)
                {
                    c.Death.BeginDying();
                    c.AddCondition(ConditionType.Unconscious);  // incapacitated → turn-skip hook
                    Debug.Log($"*** {c.Character.characterName} drops to 0 HP — DYING! " +
                              $"(death saves begin) ***");
                }
                return;  // stays in combatants / on their tile; no barracks write yet
            }

            // --- Enemy death (unchanged behavior) ---
            Debug.Log($"*** {c.Character.characterName} is down! ***");
            occupancy.Remove(c.Coord);
            combatants.Remove(c);
            turnOrder.Remove(c);

            if (c.Team == Team.Enemy) defeatedEnemyXp += c.XpReward;
            Destroy(c.gameObject);
            CheckForVictory();
        }

        // Roll one death save for a dying hero (5e): 10+ success, <10 failure.
        // Nat 20 → revive at 1 HP; nat 1 → two failures. 3 successes → stable; 3 failures → dead.
        void RollDeathSave(Combatant c)
        {
            int roll = DnDTactics.Rules.Dice.Roll(20);
            string name = c.Character.characterName;

            if (roll == 20)
            {
                c.Death.Revive();
                c.RemoveCondition(ConditionType.Unconscious);
                c.Character.Revive(1);   // back up at 1 HP
                Debug.Log($"{name} rolls a natural 20 on their death save — back up at 1 HP!");
                return;
            }
            if (roll == 1)
            {
                c.Death.AddFailure(2);
                Debug.Log($"{name} rolls a natural 1 — two death-save failures! " +
                          $"({c.Death.Failures}/3)");
            }
            else if (roll >= 10)
            {
                c.Death.AddSuccess();
                Debug.Log($"{name} succeeds a death save ({c.Death.Successes}/3 successes).");
            }
            else
            {
                c.Death.AddFailure();
                Debug.Log($"{name} fails a death save ({c.Death.Failures}/3 failures).");
            }

            if (c.Death.IsStable)
                Debug.Log($"*** {name} is STABLE (unconscious but no longer dying). ***");
            if (c.Death.IsDead)
            {
                Debug.Log($"*** {name} has DIED (three death-save failures). ***");
                KillDyingHero(c);
            }
        }

        // A dying hero who hit 3 failures: remove from the field (barracks write happens at
        // encounter resolution, reading Death.IsDead).
        void KillDyingHero(Combatant c)
        {
            if (c.Team == Team.Player && !string.IsNullOrEmpty(c.BarracksMemberId))
                combatDeadMemberIds.Add(c.BarracksMemberId);
            occupancy.Remove(c.Coord);
            combatants.Remove(c);
            turnOrder.Remove(c);
            Destroy(c.gameObject);
            CheckForVictory();
        }

        void CheckForVictory()
        {
            // A dying/stable hero is still ON the field but not fighting. Defeat = no hero is
            // still up (all remaining players are out of the fight). Victory = no enemies left.
            bool playersUp = combatants.Exists(c => c.Team == Team.Player && !c.Death.IsOutOfFight);
            bool enemiesLeft = combatants.Exists(c => c.Team == Team.Enemy);
            bool playersLeft = playersUp;

            if (!playersLeft)
            {
                Debug.Log("=== DEFEAT — all heroes are down. ===");
                ConcludeEncounter(false);
            }
            else if (!enemiesLeft)
            {
                Debug.Log("=== VICTORY — all enemies defeated! ===");
                if (!rewardsGranted) { rewardsGranted = true; GrantVictoryRewards(); }
                ConcludeEncounter(true);
            }
        }

        // Resolve the encounter once: clear state, notify listeners (exploration).
        void ConcludeEncounter(bool victory)
        {
            if (encounterConcluded) return;
            encounterConcluded = true;
            encounterRunning = false;
            if (highlighter) highlighter.Clear();
            ResolveFallenToBarracks();
            OnEncounterEnded?.Invoke(victory);
        }

        // Persist each fallen hero's death-save state to the barracks (option B — resolve by
        // actual state, no mercy stabilize): 3-failures → Dead; dying/stable → Down (revivable).
        // Heroes still up are untouched. This is where combat-only death state becomes persistent.
        void ResolveFallenToBarracks()
        {
            var slot = DnDTactics.Core.GameSession.Instance != null
                ? DnDTactics.Core.GameSession.Instance.ActiveSlot : null;
            if (slot == null) return;

            bool anyChange = false;

            // Heroes still on the field who are dying or stable → Down (revivable).
            foreach (var c in combatants)
            {
                if (c == null || c.Team != Team.Player) continue;
                if (string.IsNullOrEmpty(c.BarracksMemberId)) continue;
                if (!c.Death.IsOutOfFight) continue;

                var member = slot.barracks.GetById(c.BarracksMemberId);
                if (member == null) continue;

                member.status = DnDTactics.Characters.MemberStatus.Down;
                member.fellAtLongRest = slot.party.longRestsTaken;
                member.character.TakeDamage(99999);
                Debug.Log($"{member.character.characterName} is Down (revivable).");
                anyChange = true;
            }

            // Heroes who hit 3 failures (already removed from the field) → Dead.
            foreach (var id in combatDeadMemberIds)
            {
                var member = slot.barracks.GetById(id);
                if (member == null) continue;
                member.status = DnDTactics.Characters.MemberStatus.Dead;
                member.character.TakeDamage(99999);
                Debug.Log($"{member.character.characterName} has died in combat.");
                anyChange = true;
            }

            if (anyChange) DnDTactics.Core.GameSession.Instance.SaveActive();
        }

        void GrantVictoryRewards()
        {
            var slot = DnDTactics.Core.GameSession.Instance != null
                ? DnDTactics.Core.GameSession.Instance.ActiveSlot : null;
            if (slot == null) { Debug.Log("Victory! (No active slot — no rewards saved.)"); return; }

            // Determine the leader (the active character who receives rewards).
            string leaderId = slot.party.EnsureLeader(slot.barracks);
            var leader = leaderId != null ? slot.barracks.GetById(leaderId) : null;
            if (leader == null) { Debug.Log("Victory! (No living leader to receive rewards.)"); return; }

            // Gold: scaled to the fight, ±20% variance, to the LEADER's purse.
            int baseGold = Mathf.Max(10, encounterGoldBase);
            int gold = Mathf.RoundToInt(baseGold * UnityEngine.Random.Range(0.8f, 1.2f));
            leader.gold += gold;

            // Item drop to the leader's inventory.
            string drop = null;
            float roll = UnityEngine.Random.value;
            if (roll < 0.15f) drop = "RevivifyDiamond";
            else if (roll < 0.50f) drop = "HealingPotion";
            if (drop != null) leader.inventory.Add(drop, 1);

            // Award XP from defeated enemies, divided among deployed members (downed included).
            if (defeatedEnemyXp > 0)
            {
                // All party members earn XP, including those currently Down (per design).
                // Only the truly Dead are excluded — their share is lost with them.
                var earners = slot.party.Members(slot.barracks)
                                  .Where(m => m.status != DnDTactics.Characters.MemberStatus.Dead)
                                  .ToList();
                if (earners.Count > 0)
                {
                    int share = Mathf.Max(1, defeatedEnemyXp / earners.Count);
                    foreach (var m in earners)
                        m.character.AddXp(share);
                    Debug.Log($"Awarded {share} XP each to {earners.Count} members ({defeatedEnemyXp} total).");
                }
            }

            DnDTactics.Core.GameSession.Instance.SaveActive();
            Debug.Log($"VICTORY! {leader.character.characterName} (leader) earned {gold} gold " +
                      $"(their purse: {leader.gold})" +
                      (drop != null ? $", found 1x {drop}." : ".") +
                      " Redistribute via the transfer screen.");

        }

        // Remove all combatants/occupancy so the manager is ready for the next encounter.
        public void ClearEncounter()
        {
            foreach (var c in combatants)
                if (c != null) Destroy(c.gameObject);
            combatants.Clear();
            combatDeadMemberIds.Clear();
            occupancy.Clear();
            turnOrder = new TurnOrder();
            selected = null;
            encounterRunning = false;
            if (highlighter) highlighter.Clear();
        }
    }
}