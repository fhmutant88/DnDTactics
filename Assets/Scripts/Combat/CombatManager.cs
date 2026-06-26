using System.Collections.Generic;
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

        private TacticalGrid grid;
        private readonly Dictionary<GridCoord, Combatant> occupancy = new();
        private readonly List<Combatant> combatants = new();
        private Combatant selected;
        private TurnOrder turnOrder = new TurnOrder();
       
        [Header("Movement")]
        public CellHighlighter highlighter;
        private TurnResources resources = new TurnResources();
        private Dictionary<GridCoord, int> reachable = new();

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
            // Wrap the monster's stats in a lightweight Character-like setup.
            // For now we reuse Character via a simple adapter (see note below).
            var character = MonsterAdapter.ToCharacter(stats);
            var c = SpawnExisting(character, Team.Enemy, coord, enemyColor, enemyWeapon);
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
            BeginEncounter();
        }

        void Start()
        {
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
            var active = turnOrder.Current?.combatant;
            bool playerTurn = active != null && active.Team == Team.Player;

            if (Input.GetMouseButtonDown(0) && !PointerOverUI()) HandleClick();
            if (playerTurn && Input.GetMouseButtonDown(1) && !PointerOverUI()) HandleAttackClick();
            if (playerTurn && Input.GetKeyDown(KeyCode.Space)) EndTurn();
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
            RefreshMovementRange();

            Debug.Log($"--- Round {turnOrder.Round}: {entry.combatant.Character.characterName}'s turn " +
                      $"({entry.combatant.Team}) — Speed {entry.combatant.Character.Speed} ft ---");

            // Enemies act automatically; players wait for input.
            if (entry.combatant.Team == Team.Enemy)
                StartCoroutine(RunEnemyTurn(entry.combatant));
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
            if (!reachable.TryGetValue(target, out int cost)) return; // not a legal cell
            if (!resources.CanSpendMovement(cost)) return;

            occupancy.Remove(active.Coord);     // leave old cell
            active.SetCoord(target, grid, tokenYOffset);
            occupancy[active.Coord] = active;   // claim new cell
            resources.SpendMovement(cost);

            Debug.Log($"{active.Character.characterName} moved to {target} " +
                      $"({cost} ft). Movement left: {resources.MovementRemaining} ft.");
            RefreshMovementRange();             // recompute from the new spot
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

            AttackResult res = AttackResolver.Resolve(
                attacker.Character, target.Character, attacker.Weapon);
            resources.ActionAvailable = false; // attacking is your action

            string atkName = attacker.Character.characterName;
            string defName = target.Character.characterName;

            if (res.critMiss) { Debug.Log($"{atkName} attacks {defName}: natural 1 — miss!"); }
            else if (!res.hit)
            { Debug.Log($"{atkName} attacks {defName}: {res.attackTotal} vs AC {res.targetAC} — miss."); }
            else
            {
                target.Character.TakeDamage(res.damage);
                string critTag = res.crit ? " CRIT!" : "";
                Debug.Log($"{atkName} hits {defName}{critTag} for {res.damage} " +
                          $"({(res.crit ? "nat 20" : res.attackTotal + " vs AC " + res.targetAC)}). " +
                          $"{defName} HP: {target.Character.currentHP}/{target.Character.MaxHP}");

                if (target.Character.IsDown) DropCombatant(target);
            }

             RefreshMovementRange();
        }

        void DropCombatant(Combatant c)
        {
            Debug.Log($"*** {c.Character.characterName} is down! ***");
            occupancy.Remove(c.Coord);
            combatants.Remove(c);
            turnOrder.Remove(c);

            // If this was a deployed party hero, mark them Down in the active slot's barracks.
            if (!string.IsNullOrEmpty(c.BarracksMemberId))
            {
                var slot = DnDTactics.Core.GameSession.Instance != null
                    ? DnDTactics.Core.GameSession.Instance.ActiveSlot : null;
                var member = slot != null ? slot.barracks.GetById(c.BarracksMemberId) : null;
                if (member != null)
                {
                    member.status = DnDTactics.Characters.MemberStatus.Down;
                    member.character.TakeDamage(99999); // ensure their saved HP reflects downing
                    DnDTactics.Core.GameSession.Instance.SaveActive();
                    Debug.Log($"{member.character.characterName} marked Down in the barracks (saved).");
                }
            }

            Destroy(c.gameObject);
            CheckForVictory();
        }

        void CheckForVictory()
        {
            bool playersLeft = combatants.Exists(c => c.Team == Team.Player);
            bool enemiesLeft = combatants.Exists(c => c.Team == Team.Enemy);
            if (!playersLeft) Debug.Log("=== DEFEAT — all heroes are down. ===");
            else if (!enemiesLeft)
            {
                Debug.Log("=== VICTORY — all enemies defeated! ===");
                if (!rewardsGranted) { rewardsGranted = true; GrantVictoryRewards(); }
            }
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

            DnDTactics.Core.GameSession.Instance.SaveActive();
            Debug.Log($"VICTORY! {leader.character.characterName} (leader) earned {gold} gold " +
                      $"(their purse: {leader.gold})" +
                      (drop != null ? $", found 1x {drop}." : ".") +
                      " Redistribute via the transfer screen.");
        }
    }
}