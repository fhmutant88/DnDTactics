using System;

namespace DnDTactics.Procgen
{
    // Generates a rooms-and-corridors dungeon. Pure logic, no Unity, fully seeded so
    // the same seed always yields the same dungeon.
    public static class DungeonGenerator
    {
        public static DungeonMap Generate(
            int width, int height, int seed,
            int roomAttempts = 40, int minRoomSize = 4, int maxRoomSize = 9)
        {
            var map = new DungeonMap(width, height);
            var rng = new System.Random(seed);

            // 1. Try to place rooms; reject any that overlap an existing one (with a 1-tile gap).
            for (int attempt = 0; attempt < roomAttempts; attempt++)
            {
                int rw = rng.Next(minRoomSize, maxRoomSize + 1);
                int rh = rng.Next(minRoomSize, maxRoomSize + 1);
                int rx = rng.Next(1, Math.Max(2, width - rw - 1));
                int ry = rng.Next(1, Math.Max(2, height - rh - 1));
                var room = new Room(rx, ry, rw, rh);

                bool overlaps = false;
                foreach (var existing in map.Rooms)
                    if (room.Overlaps(existing, 1)) { overlaps = true; break; }
                if (overlaps) continue;

                CarveRoom(map, room);
                map.Rooms.Add(room);
            }

            // 2. Connect each room to the previous one — guarantees full connectivity.
            for (int i = 1; i < map.Rooms.Count; i++)
                CarveCorridor(map, map.Rooms[i - 1], map.Rooms[i], rng);

            return map;
        }

        static void CarveRoom(DungeonMap map, Room room)
        {
            for (int x = room.x; x < room.x + room.width; x++)
                for (int y = room.y; y < room.y + room.height; y++)
                    if (map.InBounds(x, y)) map.Tiles[x, y] = TileType.Floor;
        }

        // L-shaped: randomly go horizontal-then-vertical or the reverse.
        static void CarveCorridor(DungeonMap map, Room a, Room b, System.Random rng)
        {
            int x1 = a.CenterX, y1 = a.CenterY, x2 = b.CenterX, y2 = b.CenterY;
            if (rng.Next(2) == 0) { CarveH(map, x1, x2, y1); CarveV(map, y1, y2, x2); }
            else { CarveV(map, y1, y2, x1); CarveH(map, x1, x2, y2); }
        }

        static void CarveH(DungeonMap map, int xStart, int xEnd, int y)
        {
            int step = xStart < xEnd ? 1 : -1;
            for (int x = xStart; x != xEnd + step; x += step)
                if (map.InBounds(x, y)) map.Tiles[x, y] = TileType.Floor;
        }

        static void CarveV(DungeonMap map, int yStart, int yEnd, int x)
        {
            int step = yStart < yEnd ? 1 : -1;
            for (int y = yStart; y != yEnd + step; y += step)
                if (map.InBounds(x, y)) map.Tiles[x, y] = TileType.Floor;
        }
    }
}