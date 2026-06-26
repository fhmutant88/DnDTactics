using System;
using DnDTactics.Characters;

namespace DnDTactics.Persistence
{
    // One complete, independent playthrough. (Party/gold/run-state get added next step.)
    public class SaveSlot
    {
        public string slotFileId;   // the filename id (stable, generated once)
        public string displayName;  // player-chosen name, shown in the UI
        public Barracks barracks = new();
        public Party party = new();

        public SaveSlot(string displayName, string slotFileId = null)
        {
            this.displayName = displayName;
            this.slotFileId = string.IsNullOrEmpty(slotFileId)
                ? Guid.NewGuid().ToString() : slotFileId;
        }

        public SaveSlotData ToData() => new SaveSlotData
        {
            slotFileId = slotFileId,
            displayName = displayName,
            savedAtUtc = DateTime.UtcNow.ToString("o"),
            barracks = barracks.ToData(),
            party = party.ToData()
        };

        public static SaveSlot FromData(SaveSlotData d)
        {
            var slot = new SaveSlot(d.displayName, d.slotFileId);
            slot.barracks = Barracks.FromData(d.barracks);
            slot.party = Party.FromData(d.party);
            return slot;
        }
    }

    [Serializable]
    public class SaveSlotData
    {
        public string slotFileId;
        public string displayName;
        public string savedAtUtc;   // for "last played" display + sorting
        public BarracksData barracks;
        public PartyData party;
    }
}