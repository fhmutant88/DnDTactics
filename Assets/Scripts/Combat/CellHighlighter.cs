using System.Collections.Generic;
using UnityEngine;

namespace DnDTactics.Combat
{
    // Spawns flat colored quads over cells to show movement range. Pooled & reusable.
    public class CellHighlighter : MonoBehaviour
    {
        public Color moveColor = new Color(0.3f, 0.7f, 1f, 0.45f);
        public float yHeight = 0.12f;

        private readonly List<GameObject> pool = new();
        private Material mat;

        void Awake()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Unlit/Color");
            mat = new Material(s);
            mat.SetColor("_BaseColor", moveColor);
        }

        public void Show(IEnumerable<GridCoord> coords, TacticalGrid grid)
        {
            Clear();
            foreach (var c in coords)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.transform.SetParent(transform);
                go.transform.position = grid.CoordToWorld(c) + Vector3.up * yHeight;
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // lay flat
                go.transform.localScale = Vector3.one * (grid.CellSize * 0.9f);
                Destroy(go.GetComponent<Collider>());                  // don't block clicks
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
                pool.Add(go);
            }
        }

        public void Clear()
        {
            foreach (var go in pool) if (go != null) Destroy(go);
            pool.Clear();
        }
    }
}