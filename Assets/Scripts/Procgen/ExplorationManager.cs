using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DnDTactics.Combat;   // TacticalGrid, GridCoord

namespace DnDTactics.Procgen
{
    // Free-movement exploration: drop a party token in the dungeon and click walkable
    // tiles to walk there (pathfinding around walls). No turn order. Foundation for
    // discoveries/resting/descending, which layer on later.
    [DefaultExecutionOrder(200)] // after DungeonVisualizer.Start()
    public class ExplorationManager : MonoBehaviour
    {
        [Header("References")]
        public DungeonVisualizer dungeon;

        [Header("Party token")]
        public Color partyColor = new Color(0.3f, 0.6f, 0.95f);
        public float tokenYOffset = 0.6f;
        public float moveSpeed = 6f; // tiles per second along the path

        private TacticalGrid grid;
        private GameObject partyToken;
        private GridCoord partyCoord;
        private Coroutine moving;

        IEnumerator Start()
        {
            if (dungeon == null) dungeon = FindFirstObjectByType<DungeonVisualizer>();
            if (dungeon == null) { Debug.LogError("ExplorationManager: no DungeonVisualizer."); yield break; }

            // Wait a frame so the dungeon has generated its grid + map.
            yield return null;
            grid = dungeon.Grid;
            if (grid == null) { Debug.LogError("ExplorationManager: dungeon grid not ready."); yield break; }

            SpawnPartyInFirstRoom();
        }

        void SpawnPartyInFirstRoom()
        {
            // Pick a starting cell: center of the first room if available, else any floor.
            GridCoord start = FindStartCell();
            partyCoord = start;

            partyToken = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            partyToken.name = "PartyToken";
            partyToken.transform.SetParent(transform);
            partyToken.transform.localScale = new Vector3(0.7f, 0.8f, 0.7f);
            var mr = partyToken.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.SetColor("_BaseColor", partyColor);
            mr.sharedMaterial = mat;

            PlaceTokenAt(partyCoord);
            Debug.Log($"Party enters the dungeon at {partyCoord}.");
        }

        GridCoord FindStartCell()
        {
            // Start in the first room's center — as if just entering the dungeon.
            if (dungeon.Map != null && dungeon.Map.Rooms != null && dungeon.Map.Rooms.Count > 0)
            {
                var r = dungeon.Map.Rooms[0];
                var center = new GridCoord(r.CenterX, r.CenterY);
                if (grid.IsWalkable(center)) return center;

                // If the exact center isn't walkable for some reason, scan within the room.
                for (int x = r.x; x < r.x + r.width; x++)
                    for (int y = r.y; y < r.y + r.height; y++)
                        if (grid.IsWalkable(new GridCoord(x, y))) return new GridCoord(x, y);
            }

            // Fallback: first walkable cell anywhere.
            for (int x = 0; x < grid.Width; x++)
                for (int z = 0; z < grid.Depth; z++)
                    if (grid.IsWalkable(new GridCoord(x, z))) return new GridCoord(x, z);
            return new GridCoord(0, 0);
        }

                void Update()
        {
            if (grid == null || partyToken == null) return;
            if (Input.GetMouseButtonDown(0)) HandleClick();
        }

        void HandleClick()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f)) return;

            GridCoord target = grid.WorldToCoord(hit.point);
            if (!grid.InBounds(target) || !grid.IsWalkable(target)) return;
            if (target.Equals(partyCoord)) return;

            var path = FindPath(partyCoord, target);
            if (path == null || path.Count == 0) return;

            if (moving != null) StopCoroutine(moving);
            moving = StartCoroutine(WalkPath(path));
        }

        IEnumerator WalkPath(List<GridCoord> path)
        {
            foreach (var step in path)
            {
                Vector3 from = TokenWorld(partyCoord);
                Vector3 to = TokenWorld(step);
                float t = 0f;
                float dur = 1f / Mathf.Max(0.01f, moveSpeed);
                while (t < 1f)
                {
                    t += Time.deltaTime / dur;
                    partyToken.transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(t));
                    yield return null;
                }
                partyCoord = step;
            }
            moving = null;
            // (Later: this is where we check for proximity triggers — encounters/chests/traps.)
        }

        // ---- BFS pathfinding over walkable cells (4-directional) ----
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

            // Reconstruct path (excluding the start cell).
            var path = new List<GridCoord>();
            var node = goal;
            while (!node.Equals(start))
            {
                path.Add(node);
                node = cameFrom[node];
            }
            path.Reverse();
            return path;
        }

        Vector3 TokenWorld(GridCoord c) => grid.CoordToWorld(c) + Vector3.up * tokenYOffset;
        void PlaceTokenAt(GridCoord c) => partyToken.transform.position = TokenWorld(c);
    }
}