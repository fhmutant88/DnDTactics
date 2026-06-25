using System.Collections.Generic;
using DnDTactics.Combat; // GridCoord

namespace DnDTactics.Procgen
{
    // The result of placing an encounter: where party and enemies spawn.
    public class EncounterPlacement
    {
        public List<GridCoord> partySpawns = new();
        public List<GridCoord> enemySpawns = new();
        public Room partyRoom;
        public Room enemyRoom;
    }

    // Chooses spawn cells for party and enemies from a generated dungeon. Pure logic.
    public static class EncounterPlacer
    {
        public static EncounterPlacement Place(
            DungeonMap map, int partySize, int enemyCount, int seed)
        {
            var result = new EncounterPlacement();
            if (map.Rooms.Count == 0) return result;

            var rng = new System.Random(seed);

            // Party starts in a random room.
            int partyIdx = rng.Next(map.Rooms.Count);
            result.partyRoom = map.Rooms[partyIdx];

            // Enemies go in the room farthest from the party — natural approach distance.
            Room enemyRoom = result.partyRoom;
            int bestDistSq = -1;
            for (int i = 0; i < map.Rooms.Count; i++)
            {
                if (i == partyIdx) continue;
                var r = map.Rooms[i];
                int dx = r.CenterX - result.partyRoom.CenterX;
                int dy = r.CenterY - result.partyRoom.CenterY;
                int distSq = dx * dx + dy * dy;
                if (distSq > bestDistSq) { bestDistSq = distSq; enemyRoom = r; }
            }
            result.enemyRoom = enemyRoom;

            result.partySpawns = PickCells(map, result.partyRoom, partySize, rng);
            result.enemySpawns = PickCells(map, enemyRoom, enemyCount, rng);
            return result;
        }

        // Pick `count` distinct floor cells from a room, shuffled so placement varies.
        static List<GridCoord> PickCells(DungeonMap map, Room room, int count, System.Random rng)
        {
            var cells = new List<GridCoord>();
            for (int x = room.x; x < room.x + room.width; x++)
                for (int y = room.y; y < room.y + room.height; y++)
                    if (map.IsFloor(x, y)) cells.Add(new GridCoord(x, y));

            // Fisher–Yates shuffle.
            for (int i = cells.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (cells[i], cells[j]) = (cells[j], cells[i]);
            }

            var picked = new List<GridCoord>();
            for (int i = 0; i < count && i < cells.Count; i++) picked.Add(cells[i]);
            return picked;
        }
    }
}