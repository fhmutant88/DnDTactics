using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DnDTactics.Combat;
using DnDTactics.Core;
using DnDTactics.Persistence;
using DnDTactics.Characters;
using DnDTactics.Data;

namespace DnDTactics.Procgen
{
    // Bridges exploration and combat in ONE scene. Holds encounter markers; when the party
    // triggers one, spawns enemies on the shared grid and switches CombatManager on.
    // When combat ends, switches exploration back on.
    public class ExplorationEncounters : MonoBehaviour
    {
        [Header("References")]
        public ExplorationManager exploration;
        public DungeonVisualizer dungeon;
        public CombatManager combat;
        public DnDTactics.UI.CombatHUD combatHUD;

        [Header("Encounter content")]
        public List<MonsterStats> monsterPool = new();
       
        [Header("Placement")]
        [Tooltip("How many rooms (after the first) get an encounter.")]
        public int encounterCount = 3;
        [Tooltip("Trigger when the party gets this close (tiles) to an encounter's room center.")]
        public int triggerRadius = 2;

        [Tooltip("Every dungeon has at least this many chests (guaranteed reward).")]
        public int minChests = 1;
        [Tooltip("Roughly one chest per this many rooms (placeholder scaling — refine by difficulty later).")]
        public int roomsPerChest = 3;

        // A chest waiting in a room.
        class Chest { public GridCoord cell; public bool looted; public GameObject token; }
        private readonly List<Chest> chests = new();

        // An encounter waiting in a room.
        class Marker { public GridCoord cell; public bool triggered; }
        private readonly List<Marker> markers = new();
        private TacticalGrid grid;
        private bool inCombat = false;
        public bool InCombat => inCombat;

        void Awake()
        {
            if (combat == null) combat = FindFirstObjectByType<CombatManager>();
            if (combat != null) combat.startDormant = true;
            }

        void Start()
        {
            if (exploration == null) exploration = FindFirstObjectByType<ExplorationManager>();
            if (dungeon == null) dungeon = FindFirstObjectByType<DungeonVisualizer>();
            if (combat == null) combat = FindFirstObjectByType<CombatManager>();

            if (combat != null)
            {
                // startDormant already set in Awake(); just subscribe to the end event.
                combat.OnEncounterEnded += OnCombatEnded;
            }
            if (dungeon != null) dungeon.OnGenerated += PlaceMarkers;
            
            // ... existing Start code ...
            StartCoroutine(HideCombatHudNextFrame());
        }

        System.Collections.IEnumerator HideCombatHudNextFrame()
        {
            yield return null; // let CombatHUD.Start build its canvas first
            if (combatHUD == null) combatHUD = FindFirstObjectByType<DnDTactics.UI.CombatHUD>();
            if (combatHUD != null) combatHUD.SetVisible(false);

        }

        void OnDestroy()
        {
            if (combat != null) combat.OnEncounterEnded -= OnCombatEnded;
            if (dungeon != null) dungeon.OnGenerated -= PlaceMarkers;
        }

        Chest MakeChest(GridCoord cell)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Chest";
            go.transform.SetParent(transform);
            go.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
            go.transform.position = dungeon.Grid.CoordToWorld(cell) + Vector3.up * 0.25f;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.SetColor("_BaseColor", new Color(0.8f, 0.6f, 0.2f));
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.SetActive(false); // hidden until the selected character sees it
            return new Chest { cell = cell, token = go };
        }

        void PlaceMarkers()
        {
            markers.Clear();
            grid = dungeon.Grid;
            var rooms = dungeon.Map.Rooms;
            // Skip room 0 (the party's start). Place encounters in subsequent rooms.
            int placed = 0;
            for (int i = 1; i < rooms.Count && placed < encounterCount; i++)
            {
                var c = new GridCoord(rooms[i].CenterX, rooms[i].CenterY);
                if (grid.IsWalkable(c)) { markers.Add(new Marker { cell = c }); placed++; }
            }
            Debug.Log($"Placed {markers.Count} encounter(s) in the dungeon.");

            // How many chests this dungeon gets. Guarantee a minimum (every dungeon has loot),
            // then scale loosely by room count. (PLACEHOLDER scaling — refine by difficulty/level later.)
            int desiredChests = Mathf.Max(minChests, rooms.Count / Mathf.Max(1, roomsPerChest));
            // Don't ask for more chests than there are non-start rooms to hold them.
            desiredChests = Mathf.Min(desiredChests, Mathf.Max(0, rooms.Count - 1));

            int chestsPlaced = 0;
            for (int i = rooms.Count - 1; i >= 1 && chestsPlaced < desiredChests; i--)
            {
                var cc = new GridCoord(rooms[i].CenterX, rooms[i].CenterY);
                if (grid.IsWalkable(cc)) { chests.Add(MakeChest(cc)); chestsPlaced++; }
            }

            // Hard guarantee: if (somehow) nothing placed but rooms exist, force one chest.
            if (chests.Count == 0 && rooms.Count > 1)
            {
                var cc = new GridCoord(rooms[1].CenterX, rooms[1].CenterY);
                if (grid.IsWalkable(cc)) chests.Add(MakeChest(cc));
            }
            Debug.Log($"Placed {chests.Count} chest(s) in the dungeon (min {minChests}).");
        }

        void Update()
        {
            if (inCombat || grid == null || exploration == null) return;

            foreach (var m in markers)
            {
                if (m.triggered) continue;
                if (exploration.CharacterCoords.Any(c => Distance(c, m.cell) <= triggerRadius))
                {
                    m.triggered = true;
                    TriggerEncounter(m);
                    break;
                }
            }
            // Chest proximity (looting, no combat).
            foreach (var ch in chests)
            {
                if (ch.looted) continue;
                if (exploration.CharacterCoords.Any(c => Distance(c, ch.cell) <= triggerRadius))
                {
                    ch.looted = true;
                    LootChest(ch);
                }
            }

            UpdateChestVisibility(); // show/hide chest tokens per the selected character's sight
        }

        void UpdateChestVisibility()
        {
            var sel = exploration.SelectedVisionData();
            HashSet<GridCoord> visible = null;
            if (sel.HasValue)
            {
                int radius = DnDTactics.Rules.Vision.SightRadiusTiles(sel.Value.darkvisionFeet);
                visible = DnDTactics.Rules.Vision.VisibleTiles(sel.Value.coord, radius, grid);
            }
            foreach (var ch in chests)
            {
                if (ch.token == null) continue;
                if (ch.looted) { ch.token.SetActive(false); continue; }
                ch.token.SetActive(visible != null && visible.Contains(ch.cell));
            }
        }

        int Distance(GridCoord a, GridCoord b) =>
            Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.z - b.z)); // Chebyshev (grid king-moves)

        void TriggerEncounter(Marker m)
        {
            inCombat = true;
            exploration.SetExploring(false);     // stop free movement
            combat.ClearEncounter();             // fresh slate on the shared grid
            if (combatHUD != null) combatHUD.SetVisible(false);
            combat.SetGrid(grid);

            // Spawn the deployed party at/near their current position.
            var slot = GameSession.Instance != null ? GameSession.Instance.ActiveSlot : null;
            var party = slot != null ? slot.party.LivingMembers(slot.barracks).ToList()
                                     : new List<BarracksMember>();

            GridCoord partyCell = exploration.PartyCoord;
            var partySpots = NearbyWalkable(partyCell, party.Count);
            for (int i = 0; i < party.Count && i < partySpots.Count; i++)
                combat.SpawnPartyHero(party[i].character, partySpots[i], party[i].id);

            // Spawn enemies in the encounter's room.
            int enemyCount = Mathf.Clamp(monsterPool.Count > 0 ? Random.Range(2, 5) : 0, 0, 8);
            var enemySpots = NearbyWalkable(m.cell, enemyCount);
            for (int i = 0; i < enemyCount && i < enemySpots.Count; i++)
            {
                var stats = monsterPool[Random.Range(0, monsterPool.Count)];
                combat.SpawnMonster(stats, enemySpots[i]);
            }

            combat.encounterGoldBase = 100; // simple flat base for now; can budget later
            combat.StartExternalEncounter();
            Debug.Log("Encounter triggered — combat begins on the dungeon grid.");
            if (combatHUD != null) combatHUD.SetVisible(true);
        }

        void OnCombatEnded(bool victory)
        {
            inCombat = false;
            combat.ClearEncounter();

            if (victory)
            {
                exploration.SetExploring(true);   // resume the crawl
                Debug.Log("Encounter cleared — back to exploring.");
                return;
            }

            // Defeat. Check whether ANY deployed member is still alive.
            var slot = GameSession.Instance != null ? GameSession.Instance.ActiveSlot : null;
            int living = slot != null ? slot.party.LivingMembers(slot.barracks).Count() : 0;

            if (living > 0)
            {
                // Partial loss — survivors remain, the crawl continues (they can revive fallen later).
                exploration.SetExploring(true);
                Debug.Log($"The party took losses but {living} survivor(s) press on.");
            }
            else
            {
                // TOTAL PARTY KILL — the run and everything carried is lost. Autosave already
                // persisted the wipe. Recourse: Load Manual Save (if one exists) from the menu.
                Debug.Log("=== YOUR PARTY HAS FALLEN. The run is lost. ===");
                if (GameSession.Instance != null) GameSession.Instance.SaveActive(); // persist the wipe
                StartCoroutine(ReturnToMenuAfterDelay());
            }
        }

        void LootChest(Chest ch)
        {
            var slot = GameSession.Instance != null ? GameSession.Instance.ActiveSlot : null;
            if (slot == null) { Debug.Log("Found a chest, but no active slot to receive loot."); return; }

            string leaderId = slot.party.EnsureLeader(slot.barracks);
            var leader = leaderId != null ? slot.barracks.GetById(leaderId) : null;
            if (leader == null) { Debug.Log("Found a chest, but no leader to receive loot."); return; }

            // Gold: a modest random haul.
            int gold = Random.Range(40, 121); // 40–120
            leader.gold += gold;

            // Item chance: ~40% something, weighted toward potions.
            string drop = null;
            float roll = Random.value;
            if (roll < 0.10f) drop = "RevivifyDiamond";
            else if (roll < 0.18f) drop = "PortalScroll";
            else if (roll < 0.40f) drop = "HealingPotion";
            if (drop != null) leader.inventory.Add(drop, 1);

            GameSession.Instance.SaveActive();
            Debug.Log($"Opened a chest! {leader.character.characterName} found {gold} gold" +
                      (drop != null ? $" and 1x {drop}." : "."));

            if (ch.token != null) ch.token.SetActive(false); // hide the looted chest token
        }

        System.Collections.IEnumerator ReturnToMenuAfterDelay()
        {
            // Brief pause so the player sees the defeat before routing out.
            yield return new WaitForSeconds(2.5f);
            DnDTactics.Core.SceneFlow.Go(DnDTactics.Core.SceneFlow.MainMenu);
        }

        // Find up to `count` walkable cells near a center (BFS ring outward).
        List<GridCoord> NearbyWalkable(GridCoord center, int count)
        {
            var result = new List<GridCoord>();
            var seen = new HashSet<GridCoord>();
            var q = new Queue<GridCoord>();
            q.Enqueue(center); seen.Add(center);
            GridCoord[] dirs = { new GridCoord(1,0), new GridCoord(-1,0),
                                 new GridCoord(0,1), new GridCoord(0,-1) };
            while (q.Count > 0 && result.Count < count)
            {
                var cur = q.Dequeue();
                if (grid.IsWalkable(cur) && !combat.IsOccupied(cur)) result.Add(cur);
                foreach (var d in dirs)
                {
                    var n = new GridCoord(cur.x + d.x, cur.z + d.z);
                    if (!seen.Contains(n) && grid.InBounds(n)) { seen.Add(n); q.Enqueue(n); }
                }
            }
            return result;
        }
    }
}