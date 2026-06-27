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

        [Header("Encounter content")]
        public List<MonsterStats> monsterPool = new();
       
        [Header("Placement")]
        [Tooltip("How many rooms (after the first) get an encounter.")]
        public int encounterCount = 3;
        [Tooltip("Trigger when the party gets this close (tiles) to an encounter's room center.")]
        public int triggerRadius = 2;

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
        }

        void OnDestroy()
        {
            if (combat != null) combat.OnEncounterEnded -= OnCombatEnded;
            if (dungeon != null) dungeon.OnGenerated -= PlaceMarkers;
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
        }

        void Update()
        {
            if (inCombat || grid == null || exploration == null) return;

            GridCoord party = exploration.PartyCoord;
            foreach (var m in markers)
            {
                if (m.triggered) continue;
                if (Distance(party, m.cell) <= triggerRadius)
                {
                    m.triggered = true;
                    TriggerEncounter(m);
                    break;
                }
            }
        }

        int Distance(GridCoord a, GridCoord b) =>
            Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.z - b.z)); // Chebyshev (grid king-moves)

        void TriggerEncounter(Marker m)
        {
            inCombat = true;
            exploration.SetExploring(false);     // stop free movement
            combat.ClearEncounter();             // fresh slate on the shared grid
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