using System.Collections.Generic;
using UnityEngine;
using DnDTactics.Combat;

namespace DnDTactics.Procgen
{
    // Test harness: shows where party (blue) and enemies (red) would spawn in the dungeon.
    public class EncounterPlacementTester : MonoBehaviour
    {
        public DungeonVisualizer dungeon;
        public int partySize = 4;
        public int enemyCount = 3;
        [Tooltip("0 = random placement each generation.")]
        public int placementSeed = 0;
        public float markerHeight = 0.9f;
        public Color partyColor = new Color(0.25f, 0.5f, 0.9f);
        public Color enemyColor = new Color(0.85f, 0.3f, 0.3f);

        private readonly List<GameObject> markers = new();
        private Material partyMat, enemyMat;

        void OnEnable()
        {
            if (dungeon != null) dungeon.OnGenerated += PlaceMarkers;
        }

        void OnDisable()
        {
            if (dungeon != null) dungeon.OnGenerated -= PlaceMarkers;
        }

        void PlaceMarkers()
        {
            ClearMarkers();
            if (dungeon == null || dungeon.Map == null || dungeon.Grid == null) return;

            int seed = placementSeed != 0 ? placementSeed : System.Environment.TickCount;
            var placement = EncounterPlacer.Place(dungeon.Map, partySize, enemyCount, seed);

            EnsureMaterials();
            foreach (var c in placement.partySpawns) Spawn(c, partyMat);
            foreach (var c in placement.enemySpawns) Spawn(c, enemyMat);

            Debug.Log($"Placement: {placement.partySpawns.Count} party, " +
                      $"{placement.enemySpawns.Count} enemies.");
        }

        void Spawn(GridCoord c, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.SetParent(transform);
            go.transform.localScale = new Vector3(0.6f, 0.7f, 0.6f);
            go.transform.position = dungeon.Grid.CoordToWorld(c) + Vector3.up * markerHeight;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            markers.Add(go);
        }

        void ClearMarkers()
        {
            foreach (var m in markers) if (m != null) Destroy(m);
            markers.Clear();
        }

        void EnsureMaterials()
        {
            if (partyMat != null && enemyMat != null) return;
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");
            partyMat = new Material(lit); partyMat.SetColor("_BaseColor", partyColor);
            enemyMat = new Material(lit); enemyMat.SetColor("_BaseColor", enemyColor);
        }
    }
}