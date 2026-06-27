using System.Collections.Generic;
using UnityEngine;
using DnDTactics.Combat; // TacticalGrid, GridCoord

namespace DnDTactics.Procgen
{
    // Generates a dungeon, builds a walkable TacticalGrid, and renders tiles.
    // Tiles start HIDDEN (fog of war) and are revealed by the FogOfWar component.
    public class DungeonVisualizer : MonoBehaviour
    {
        [Header("Map Size")]
        public int width = 32;
        public int height = 32;
        public float cellSize = 1f;

        [Header("Generation")]
        public int seed = 0;
        public int roomAttempts = 40;
        public int minRoomSize = 4;
        public int maxRoomSize = 9;

        [Header("Tile Look")]
        public float floorHeight = 0.1f;
        public float wallHeight = 1.2f;
        public Color floorColor = new Color(0.45f, 0.42f, 0.38f);
        public Color wallColor = new Color(0.18f, 0.17f, 0.20f);

        [Header("Fog")]
        [Tooltip("How much to dim explored-but-not-currently-visible tiles (0 = black, 1 = full).")]
        [Range(0f, 1f)] public float exploredDim = 0.35f;

        public TacticalGrid Grid { get; private set; }
        public DungeonMap Map { get; private set; }
        public event System.Action OnGenerated;

        // Per-tile rendering data, so fog can show/hide/tint each tile individually.
        class Tile
        {
            public GameObject go;
            public MeshRenderer mr;
            public Material mat;
            public Color baseColor;  // full-bright color (floor or wall)
        }
        private readonly Dictionary<GridCoord, Tile> tiles = new();

        void Start() => Generate();

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) Generate();
        }

        public void Generate()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            tiles.Clear();

            int useSeed = seed != 0 ? seed : System.Environment.TickCount;
            Map = DungeonGenerator.Generate(width, height, useSeed,
                                            roomAttempts, minRoomSize, maxRoomSize);

            Grid = new TacticalGrid(width, height, cellSize);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    var cell = Grid.GetCell(new GridCoord(x, y));
                    if (cell != null) cell.walkable = Map.IsFloor(x, y);
                }

            RenderTiles();
            Debug.Log($"Generated dungeon (seed {useSeed}) — {Map.Rooms.Count} rooms.");
            OnGenerated?.Invoke();
        }

        void RenderTiles()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool floor = Map.IsFloor(x, y);
                    if (!floor && !IsBorderWall(x, y)) continue;

                    float h = floor ? floorHeight : wallHeight;
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = (floor ? "Floor_" : "Wall_") + x + "_" + y;
                    go.transform.SetParent(transform);
                    go.transform.position =
                        Grid.CoordToWorld(new GridCoord(x, y)) + Vector3.up * (h * 0.5f);
                    go.transform.localScale = new Vector3(cellSize, h, cellSize);

                    var mr = go.GetComponent<MeshRenderer>();
                    var mat = new Material(lit);                 // own material per tile
                    Color baseColor = floor ? floorColor : wallColor;
                    mat.SetColor("_BaseColor", baseColor);
                    mr.sharedMaterial = mat;

                    var tile = new Tile { go = go, mr = mr, mat = mat, baseColor = baseColor };
                    tiles[new GridCoord(x, y)] = tile;

                    go.SetActive(false); // start hidden (fog) — FogOfWar reveals as explored
                }
            }
        }

        bool IsBorderWall(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (!(dx == 0 && dy == 0) && Map.IsFloor(x + dx, y + dy)) return true;
            return false;
        }

        // ---- Fog API: set a tile's visibility state ----

        public enum TileVisibility { Unseen, Explored, Visible }

        public void SetTileVisibility(GridCoord c, TileVisibility state)
        {
            if (!tiles.TryGetValue(c, out var t)) return;
            switch (state)
            {
                case TileVisibility.Unseen:
                    t.go.SetActive(false);
                    break;
                case TileVisibility.Explored:
                    t.go.SetActive(true);
                    t.mat.SetColor("_BaseColor", t.baseColor * exploredDim);
                    break;
                case TileVisibility.Visible:
                    t.go.SetActive(true);
                    t.mat.SetColor("_BaseColor", t.baseColor);
                    break;
            }
        }

        public bool HasTile(GridCoord c) => tiles.ContainsKey(c);
    }
}