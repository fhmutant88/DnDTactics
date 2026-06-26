using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using DnDTactics.Characters;

namespace DnDTactics.Persistence
{
    // Manages up to MaxSlots named save files, one file per slot.
    public static class SaveManager
    {
        public const int MaxSlots = 5;
        const string Prefix = "save_";
        const string Ext = ".json";

        public static string ActiveSlotFileId { get; private set; } // slot loaded in memory

        static string Dir => Application.persistentDataPath;
        static string PathFor(string fileId) => Path.Combine(Dir, Prefix + fileId + Ext);

        // Lightweight listing for a slot-select screen (no full barracks load needed).
        public struct SlotInfo { public string fileId; public string displayName; public string savedAtUtc; }

        public static List<SlotInfo> ListSlots()
        {
            var infos = new List<SlotInfo>();
            foreach (var path in Directory.GetFiles(Dir, Prefix + "*" + Ext))
            {
                try
                {
                    var d = JsonUtility.FromJson<SaveSlotData>(File.ReadAllText(path));
                    if (d != null)
                        infos.Add(new SlotInfo
                        {
                            fileId = d.slotFileId,
                            displayName = d.displayName,
                            savedAtUtc = d.savedAtUtc
                        });
                }
                catch { /* skip unreadable/corrupt file */ }
            }
            return infos.OrderByDescending(i => i.savedAtUtc).ToList();
        }

        public static bool CanCreateNewSlot() => ListSlots().Count < MaxSlots;

        public static SaveSlot CreateSlot(string displayName)
        {
            if (!CanCreateNewSlot())
            {
                Debug.LogWarning($"Slot limit reached ({MaxSlots}).");
                return null;
            }
            var slot = new SaveSlot(displayName);
            Save(slot);
            ActiveSlotFileId = slot.slotFileId;
            return slot;
        }

        public static void Save(SaveSlot slot)
        {
            string json = JsonUtility.ToJson(slot.ToData(), prettyPrint: true);
            File.WriteAllText(PathFor(slot.slotFileId), json);
            ActiveSlotFileId = slot.slotFileId;
            Debug.Log($"Saved slot '{slot.displayName}' ({slot.slotFileId}).");
        }

        public static SaveSlot Load(string fileId)
        {
            string path = PathFor(fileId);
            if (!File.Exists(path)) { Debug.LogWarning($"No slot file {path}"); return null; }
            var d = JsonUtility.FromJson<SaveSlotData>(File.ReadAllText(path));
            ActiveSlotFileId = fileId;
            return SaveSlot.FromData(d);
        }

        public static void Delete(string fileId)
        {
            string path = PathFor(fileId);
            if (File.Exists(path)) File.Delete(path);
            if (ActiveSlotFileId == fileId) ActiveSlotFileId = null;
        }
    }
}