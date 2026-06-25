using UnityEngine;
using DnDTactics.Combat; // TacticalGrid, GridCoord

namespace DnDTactics.Procgen
{
    // Generates a dungeon, builds a walkable TacticalGrid from it, and renders tiles.
    public class DungeonVisualizer : MonoBehaviour
    {
        [Header("Map Size")]
        public int width = 32;
        public int height = 32;
        public float cellSize = 1f;

        [Header("Generation")]
        [Tooltip("0 = a fresh random dungeon each run. Any other value is reproducible.")]
        public int seed = 0;
        public int roomAttempts = 40;
        public int minRoomSize = 4;
        public int maxRoomSize = 9;

        [Header("Tile Look")]
        public float floorHeight = 0.1f;
        public float wallHeight = 1.2f;
        public Color floorColor = new Color(0.45f, 0.42f, 0.38f);
        public Color wallColor = new Color(0.18f, 0.17f, 0.20f);

        public TacticalGrid Grid { get; private set; }
        public DungeonMap Map { get; private set; }

        public event System.Action OnGenerated;

        private Material floorMat, wallMat;

        void Start() => Generate();

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) Generate(); // press R to reroll the dungeon
        }

        public void Generate()
        {
            // Clear previous tiles.
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            int useSeed = seed != 0 ? seed : System.Environment.TickCount;
            Map = DungeonGenerator.Generate(width, height, useSeed,
                                            roomAttempts, minRoomSize, maxRoomSize);

            // Build the gameplay grid: floor = walkable, wall = blocked.
            Grid = new TacticalGrid(width, height, cellSize);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    var cell = Grid.GetCell(new GridCoord(x, y));
                    if (cell != null) cell.walkable = Map.IsFloor(x, y);
                }

            EnsureMaterials();
            RenderTiles();
            Debug.Log($"Generated dungeon (seed {useSeed}) — {Map.Rooms.Count} rooms.");
            OnGenerated?.Invoke();
        }

        void RenderTiles()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool floor = Map.IsFloor(x, y);
                    // Only draw floors and the walls that border a floor (keeps it light + clean).
                    if (!floor && !IsBorderWall(x, y)) continue;

                    float h = floor ? floorHeight : wallHeight;
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = (floor ? "Floor_" : "Wall_") + x + "_" + y;
                    tile.transform.SetParent(transform);
                    tile.transform.position =
                        Grid.CoordToWorld(new GridCoord(x, y)) + Vector3.up * (h * 0.5f);
                    tile.transform.localScale = new Vector3(cellSize, h, cellSize);
                    tile.GetComponent<MeshRenderer>().sharedMaterial = floor ? floorMat : wallMat;
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

        void EnsureMaterials()
        {
            if (floorMat != null && wallMat != null) return;
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");
            floorMat = new Material(lit); floorMat.SetColor("_BaseColor", floorColor);
            wallMat = new Material(lit); wallMat.SetColor("_BaseColor", wallColor);
        }
    }
}