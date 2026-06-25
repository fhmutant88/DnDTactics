using System;
using UnityEngine;

namespace DnDTactics.Combat
{
    // Integer coordinates on the tactical grid. The grid lies on the world XZ plane
    // (X = columns, Z = rows). World Y is reserved for height/elevation.
    [Serializable]
    public struct GridCoord : IEquatable<GridCoord>
    {
        public int x;
        public int z;

        public GridCoord(int x, int z) { this.x = x; this.z = z; }

        // 5e basic grid rule: each square is 5 ft, and a diagonal is also 5 ft.
        // That makes grid distance the larger of the two axis distances.
        public int DistanceInSquares(GridCoord other) =>
            Mathf.Max(Mathf.Abs(x - other.x), Mathf.Abs(z - other.z));

        public int DistanceInFeet(GridCoord other) => DistanceInSquares(other) * 5;

        public bool Equals(GridCoord other) => x == other.x && z == other.z;
        public override bool Equals(object obj) => obj is GridCoord o && Equals(o);
        public override int GetHashCode() => (x * 397) ^ z;
        public override string ToString() => $"({x},{z})";

        public static bool operator ==(GridCoord a, GridCoord b) => a.Equals(b);
        public static bool operator !=(GridCoord a, GridCoord b) => !a.Equals(b);
    }
}