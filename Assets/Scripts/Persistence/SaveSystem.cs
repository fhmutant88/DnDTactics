using System.IO;
using UnityEngine;

namespace DnDTactics.Characters
{
    // Reads/writes character snapshots to JSON files in the OS save location.
    public static class SaveSystem
    {
        static string PathFor(string slot) =>
            Path.Combine(Application.persistentDataPath, slot + ".json");

        public static void SaveCharacter(string slot, Character c)
        {
            CharacterData data = CharacterSerialization.ToData(c);
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(PathFor(slot), json);
            Debug.Log($"Saved '{c.characterName}' to {PathFor(slot)}");
        }

        public static Character LoadCharacter(string slot)
        {
            string path = PathFor(slot);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"No save at {path}");
                return null;
            }
            string json = File.ReadAllText(path);
            CharacterData data = JsonUtility.FromJson<CharacterData>(json);
            return CharacterSerialization.FromData(data);
        }

        public static bool Exists(string slot) => File.Exists(PathFor(slot));
        public static void Delete(string slot)
        {
            string path = PathFor(slot);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}