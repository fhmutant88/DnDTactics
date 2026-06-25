using System.Collections.Generic;

namespace DnDTactics.Procgen
{
    public enum TileType { Wall, Floor }

    // A rectangular room in tile coordinates.
    public struct Room
    {
        public int x, y, width, height;
        public Room(int x, int y, int width, int height)
        {
            this.x = x; this.y = y; this.width = width; this.height = height;
        }
        public int CenterX => x + width / 2;
        public int CenterY => y + height / 2;

        // True if this room overlaps another, expanded by `padding` tiles on all sides.
        public bool Overlaps(Room other, int padding) =>
            x - padding < other.x + other.width &&
            x + width + padding > other.x &&
            y - padding < other.y + other.height &&
            y + height + padding > other.y;
    }

    // The generated dungeon as pure data: a grid of tiles plus the room list.
    public class DungeonMap
    {
        public int Width { get; }
        public int Height { get; }
        public TileType[,] Tiles { get; }
        public List<Room> Rooms { get; } = new();

        public DungeonMap(int width, int height)
        {
            Width = width; Height = height;
            Tiles = new TileType[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    Tiles[x, y] = TileType.Wall; // everything starts solid; we carve floors
        }

        public bool InBounds(int x, int y) =>
            x >= 0 && x < Width && y >= 0 && y < Height;

        public bool IsFloor(int x, int y) =>
            InBounds(x, y) && Tiles[x, y] == TileType.Floor;
    }
}