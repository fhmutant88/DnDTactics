using UnityEngine;

namespace DnDTactics.Combat
{
    // Builds the TacticalGrid and spawns a placeholder tile per cell so you can see it.
    // Placeholder visuals only; the art pass (Milestone 7) replaces these.
    public class GridVisualizer : MonoBehaviour
    {
        [Header("Grid Size")]
        public int width = 10;
        public int depth = 10;
        public float cellSize = 1f;

        [Header("Tile Look")]
        [Range(0.5f, 1f)] public float tileFill = 0.92f; // < 1 leaves gaps = visible gridlines
        public float tileHeight = 0.1f;
        public Color colorA = new Color(0.55f, 0.58f, 0.62f);
        public Color colorB = new Color(0.42f, 0.45f, 0.50f);

        public TacticalGrid Grid { get; private set; }

        private Material matA, matB;

        void Start() => BuildGrid();

        public void BuildGrid()
        {
            Grid = new TacticalGrid(width, depth, cellSize);
            CreateMaterials();

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    var coord = new GridCoord(x, z);
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Tile_{x}_{z}";
                    tile.transform.SetParent(transform);                 // tidy Hierarchy
                    tile.transform.position = Grid.CoordToWorld(coord);   // sits at Y = 0
                    tile.transform.localScale =
                        new Vector3(cellSize * tileFill, tileHeight, cellSize * tileFill);
                    tile.GetComponent<MeshRenderer>().sharedMaterial =
                        ((x + z) % 2 == 0) ? matA : matB;                 // checkerboard
                }
            }
        }

        void CreateMaterials()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard"); // fallback if not URP
            matA = new Material(lit);
            matB = new Material(lit);
            matA.SetColor("_BaseColor", colorA); // URP Lit's color property is _BaseColor
            matB.SetColor("_BaseColor", colorB);
        }
    }
}