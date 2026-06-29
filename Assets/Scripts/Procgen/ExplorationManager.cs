using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DnDTactics.Combat;        // TacticalGrid, GridCoord
using DnDTactics.Core;          // GameSession
using DnDTactics.Characters;    // BarracksMember

namespace DnDTactics.Procgen
{
    // Individual-token exploration: one token per deployed party member. Group click-to-move
    // for now (leader to target, followers cluster). Selection / individual control come next.
    [DefaultExecutionOrder(200)]
    public class ExplorationManager : MonoBehaviour
    {
        [Header("References")]
        public DungeonVisualizer dungeon;

        [Header("Tokens")]
        public Color partyColor = new Color(0.3f, 0.6f, 0.95f);
        public Color leaderColor = new Color(0.95f, 0.8f, 0.3f); // leader stands out
        public float tokenYOffset = 0.6f;
        public float moveSpeed = 6f;

        private CharToken selected;
        private bool individualMode = false;
        public bool IndividualMode => individualMode;
        public void ToggleMode() => individualMode = !individualMode;
        public string ModeName => individualMode ? "Individual" : "Group";
        public FogOfWar fog;

        // The selected character's display name (for the HUD label). Empty if none/placeholder.
        public string SelectedName
        {
            get
            {
                if (selected == null || string.IsNullOrEmpty(selected.memberId)) return "—";
                var slot = GameSession.Instance != null ? GameSession.Instance.ActiveSlot : null;
                var m = slot != null ? slot.barracks.GetById(selected.memberId) : null;
                return m != null ? m.character.characterName : "—";
            }
        }

        // One token per deployed member.
        class CharToken
        {
            public string memberId;
            public GameObject go;
            public GridCoord coord;
            public Coroutine moving;
            public Vector3 baseScale;   // remember normal size for selection scaling
            public bool torchLit;       // is this character's torch currently held/lit?
            public int torchLights;     // remaining lights on the active torch (0 = none active)
        }

        private readonly List<CharToken> tokens = new();

        private TacticalGrid grid;
        private bool exploring = true;

        // Leader's coord, used as "the party position" for now (encounter/chest proximity).
        public GridCoord PartyCoord => tokens.Count > 0 ? tokens[0].coord : new GridCoord(0, 0);

        // All character positions (for "any character near a marker" triggering).
        public IEnumerable<GridCoord> CharacterCoords => tokens.Select(t => t.coord);

        // For fog/vision: each token's coord paired with that character's darkvision (feet).
        public IEnumerable<(GridCoord coord, int darkvisionFeet)> CharacterVisionData()
        {
            var slot = GameSession.Instance != null ? GameSession.Instance.ActiveSlot : null;
            foreach (var t in tokens)
            {
                int dv = 0;
                if (slot != null && !string.IsNullOrEmpty(t.memberId))
                {
                    var m = slot.barracks.GetById(t.memberId);
                    if (m != null && m.character != null && m.character.species != null)
                        dv = m.character.species.darkvisionRange;
                }
                yield return (t.coord, dv);
            }
        }

        // The selected character's vision data (coord + darkvision feet), or null if none.
        public (GridCoord coord, int darkvisionFeet)? SelectedVisionData()
        {
            if (selected == null) return null;
            int dv = 0;
            var slot = GameSession.Instance != null ? GameSession.Instance.ActiveSlot : null;
            if (slot != null && !string.IsNullOrEmpty(selected.memberId))
            {
                var m = slot.barracks.GetById(selected.memberId);
                if (m != null && m.character != null && m.character.species != null)
                    dv = m.character.species.darkvisionRange;
            }
            return (selected.coord, dv);
        }

        public void SetExploring(bool on)
        {
            exploring = on;
            if (!on) StopAllTokenMovement();
        }

        public void RespawnForNewDungeon()
        {
            grid = dungeon.Grid;
            GridCoord start = FindStartCell();
            var cells = NearbyWalkable(start, tokens.Count);
            for (int i = 0; i < tokens.Count; i++)
            {
                GridCoord cell = i < cells.Count ? cells[i] : start;
                tokens[i].coord = cell;
                tokens[i].go.transform.position = TokenWorld(cell);
            }
            if (tokens.Count > 0) SelectToken(tokens[0]);
        }

        IEnumerator Start()
        {
            if (dungeon == null) dungeon = FindFirstObjectByType<DungeonVisualizer>();
            if (dungeon == null) { Debug.LogError("ExplorationManager: no DungeonVisualizer."); yield break; }

            yield return null; // let the dungeon generate its grid + map
            grid = dungeon.Grid;
            if (grid == null) { Debug.LogError("ExplorationManager: dungeon grid not ready."); yield break; }

            SpawnPartyInFirstRoom();
            if (fog == null) fog = FindFirstObjectByType<FogOfWar>();
            if (fog != null) fog.Recompute(); // initial reveal around the spawn
        }

        void SpawnPartyInFirstRoom()
        {
            GridCoord start = FindStartCell();

            // Which members to spawn: the deployed living party (fallback: a single token).
            var slot = GameSession.Instance != null ? GameSession.Instance.ActiveSlot : null;
            var members = slot != null ? slot.party.LivingMembers(slot.barracks).ToList()
                                       : new List<BarracksMember>();

            if (members.Count == 0)
            {
                // Fallback (e.g. scene run directly): one generic token so exploration still works.
                var spot = start;
                tokens.Add(MakeToken(null, spot, true));
                if (tokens.Count > 0) SelectToken(tokens[0]); // leader pre-selected
                Debug.Log("Exploration: no deployed party — spawned a single placeholder token.");
                return;
            }

            // Spawn each member on a nearby walkable cell, leader first (token[0]).
            string leaderId = slot.party.EnsureLeader(slot.barracks);
            // Put the leader at the front of the list so it's token[0].
            members = members.OrderByDescending(m => m.id == leaderId).ToList();

            var cells = NearbyWalkable(start, members.Count);
            for (int i = 0; i < members.Count; i++)
            {
                GridCoord cell = i < cells.Count ? cells[i] : start;
                bool isLeader = members[i].id == leaderId;
                tokens.Add(MakeToken(members[i].id, cell, isLeader));
            }
            Debug.Log($"Party enters the dungeon: {tokens.Count} characters at the first room.");
        }

        CharToken MakeToken(string memberId, GridCoord cell, bool isLeader)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = isLeader ? "Token_Leader" : "Token_Member";
            go.transform.SetParent(transform);
            go.transform.localScale = new Vector3(0.7f, 0.8f, 0.7f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.SetColor("_BaseColor", isLeader ? leaderColor : partyColor);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var t = new CharToken { memberId = memberId, go = go, coord = cell };
            t.baseScale = go.transform.localScale;
            go.transform.position = TokenWorld(cell);
            return t;
        }

        GridCoord FindStartCell()
        {
            if (dungeon.Map != null && dungeon.Map.Rooms != null && dungeon.Map.Rooms.Count > 0)
            {
                var r = dungeon.Map.Rooms[0];
                var center = new GridCoord(r.CenterX, r.CenterY);
                if (grid.IsWalkable(center)) return center;
                for (int x = r.x; x < r.x + r.width; x++)
                    for (int y = r.y; y < r.y + r.height; y++)
                        if (grid.IsWalkable(new GridCoord(x, y))) return new GridCoord(x, y);
            }
            for (int x = 0; x < grid.Width; x++)
                for (int z = 0; z < grid.Depth; z++)
                    if (grid.IsWalkable(new GridCoord(x, z))) return new GridCoord(x, z);
            return new GridCoord(0, 0);
        }

        void SelectToken(CharToken t)
        {
            if (selected == t) return;
            if (selected != null && selected.go != null)
                selected.go.transform.localScale = selected.baseScale;   // unhighlight old
            selected = t;
            if (selected != null && selected.go != null)
                selected.go.transform.localScale = selected.baseScale * 1.15f; // highlight new
            Debug.Log($"Selected: {SelectedName}");
            if (fog != null) fog.Recompute();
            var camCtrl = FindFirstObjectByType<DnDTactics.Core.CameraController>();
            if (camCtrl != null && selected != null) camCtrl.recenterTarget = selected.go.transform;
        }

        // Find the token whose GameObject was clicked (via the hit collider).
        CharToken TokenFromGameObject(GameObject go) =>
            tokens.FirstOrDefault(t => t.go == go);

        void Update()
        {
            if (!exploring || grid == null || tokens.Count == 0) return;
            if (Input.GetMouseButtonDown(0)) HandleClick();
            
        }

        void HandleClick()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f)) return;

            // Clicked a character token? Select it (don't move).
            var clickedToken = TokenFromGameObject(hit.collider.gameObject);
            if (clickedToken != null) { SelectToken(clickedToken); return; }

            // Otherwise it's a ground click → move, per the current mode.
            GridCoord target = grid.WorldToCoord(hit.point);
            if (!grid.InBounds(target) || !grid.IsWalkable(target)) return;

            if (individualMode)
                MoveSelectedTo(target);
            else
                MoveGroupTo(target);
        }

        public const int TorchRadiusTiles = 4;   // ~20 ft bright radius
        public const int TorchLightsPerTorch = 10; // how many times a torch can be lit

        // Toggle the selected character's torch. Lighting consumes a use (and a Torch from inventory
        // when starting a fresh one). Stowing is free.
        public void ToggleSelectedTorch()
        {
            if (selected == null) return;
            var slot = GameSession.Instance != null ? GameSession.Instance.ActiveSlot : null;
            if (slot == null || string.IsNullOrEmpty(selected.memberId)) return;
            var m = slot.barracks.GetById(selected.memberId);
            if (m == null) return;

            if (selected.torchLit)
            {
                // Stow — free, no use consumed.
                selected.torchLit = false;
                Debug.Log($"{m.character.characterName} stows the torch ({selected.torchLights} lights left).");
            }
            else
            {
                // Lighting it.
                if (selected.torchLights <= 0)
                {
                    // Need to start a fresh torch from inventory.
                    if (!m.inventory.Has("Torch")) { Debug.Log("No torch to light."); return; }
                    m.inventory.Remove("Torch", 1);
                    selected.torchLights = TorchLightsPerTorch;
                    GameSession.Instance.SaveActive();
                }
                // Consume one light (one use) when lighting.
                selected.torchLights--;
                selected.torchLit = true;
                Debug.Log($"{m.character.characterName} lights a torch ({selected.torchLights} lights left).");
                if (selected.torchLights < 0) selected.torchLights = 0;
            }
            if (fog != null) fog.Recompute();
        }

        // For the HUD.
        public bool SelectedHasTorchAvailable()
        {
            if (selected == null) return false;
            if (selected.torchLights > 0) return true; // an active torch still has lights
            var slot = GameSession.Instance != null ? GameSession.Instance.ActiveSlot : null;
            if (slot == null || string.IsNullOrEmpty(selected.memberId)) return false;
            var m = slot.barracks.GetById(selected.memberId);
            return m != null && m.inventory.Has("Torch");
        }

        public bool SelectedTorchLit() => selected != null && selected.torchLit;
        public int SelectedTorchLights() => selected != null ? selected.torchLights : 0;

        // Positions of all currently-lit torches (for lighting computation).
        public IEnumerable<GridCoord> LitTorchPositions()
        {
            foreach (var t in tokens)
                if (t.torchLit) yield return t.coord;
        }

        // Group move: leader (token[0]) goes to target; followers cluster around it.
        void MoveGroupTo(GridCoord target)
        {
            StopAllTokenMovement();

            // Leader path to the clicked target.
            var leader = tokens[0];
            var leaderPath = FindPath(leader.coord, target);
            if (leaderPath == null) return;
            leader.moving = StartCoroutine(WalkToken(leader, leaderPath));

            // Followers path to walkable cells around the target.
            if (tokens.Count > 1)
            {
                var dests = NearbyWalkable(target, tokens.Count); // includes target as first
                int di = 1; // skip [0] (the leader's target)
                for (int i = 1; i < tokens.Count; i++)
                {
                    GridCoord dest = di < dests.Count ? dests[di++] : target;
                    var path = FindPath(tokens[i].coord, dest);
                    if (path != null)
                        tokens[i].moving = StartCoroutine(WalkToken(tokens[i], path));
                }
            }
        }

        // Individual move: only the selected token walks to the target; others hold position.
        void MoveSelectedTo(GridCoord target)
        {
            if (selected == null) return;
            if (selected.moving != null) { StopCoroutine(selected.moving); selected.moving = null; }
            var path = FindPath(selected.coord, target);
            if (path == null) return;
            selected.moving = StartCoroutine(WalkToken(selected, path));
        }

        void StopAllTokenMovement()
        {
            foreach (var t in tokens)
                if (t.moving != null) { StopCoroutine(t.moving); t.moving = null; }
        }

        IEnumerator WalkToken(CharToken token, List<GridCoord> path)
        {
            foreach (var step in path)
            {
                Vector3 from = TokenWorld(token.coord);
                Vector3 to = TokenWorld(step);
                float t = 0f;
                float dur = 1f / Mathf.Max(0.01f, moveSpeed);
                while (t < 1f)
                {
                    t += Time.deltaTime / dur;
                    token.go.transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(t));
                    yield return null;
                }
                token.coord = step;
                if (fog != null) fog.Recompute(); // update fog as the party advances
            }
            token.moving = null;
        }

        // Up to `count` walkable cells near a center (BFS ring outward).
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
                if (grid.IsWalkable(cur)) result.Add(cur);
                foreach (var d in dirs)
                {
                    var n = new GridCoord(cur.x + d.x, cur.z + d.z);
                    if (!seen.Contains(n) && grid.InBounds(n) && grid.IsWalkable(n))
                    {
                        seen.Add(n); q.Enqueue(n);
                    }
                }
            }
            return result;
        }

        List<GridCoord> FindPath(GridCoord start, GridCoord goal)
        {
            if (!grid.IsWalkable(goal)) return null;
            var frontier = new Queue<GridCoord>();
            var cameFrom = new Dictionary<GridCoord, GridCoord>();
            frontier.Enqueue(start);
            cameFrom[start] = start;
            GridCoord[] dirs = {
                new GridCoord(1, 0), new GridCoord(-1, 0),
                new GridCoord(0, 1), new GridCoord(0, -1)
            };
            bool found = false;
            while (frontier.Count > 0)
            {
                var cur = frontier.Dequeue();
                if (cur.Equals(goal)) { found = true; break; }
                foreach (var d in dirs)
                {
                    var next = new GridCoord(cur.x + d.x, cur.z + d.z);
                    if (!grid.InBounds(next) || !grid.IsWalkable(next)) continue;
                    if (cameFrom.ContainsKey(next)) continue;
                    cameFrom[next] = cur;
                    frontier.Enqueue(next);
                }
            }
            if (!found) return null;
            var path = new List<GridCoord>();
            var node = goal;
            while (!node.Equals(start)) { path.Add(node); node = cameFrom[node]; }
            path.Reverse();
            return path;
        }

        Vector3 TokenWorld(GridCoord c) => grid.CoordToWorld(c) + Vector3.up * tokenYOffset;
    }
}