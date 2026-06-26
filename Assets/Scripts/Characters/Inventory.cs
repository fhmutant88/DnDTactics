using System;
using System.Collections.Generic;
using System.Linq;

namespace DnDTactics.Characters
{
    // A per-character inventory: item id (the ItemDefinition asset name) -> quantity.
    // Serializable via parallel lists (Unity's JsonUtility can't serialize a Dictionary).
    [Serializable]
    public class Inventory
    {
        public List<string> itemIds = new();
        public List<int> quantities = new();

        public void Add(string itemId, int count = 1)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return;
            int idx = itemIds.IndexOf(itemId);
            if (idx >= 0) quantities[idx] += count;
            else { itemIds.Add(itemId); quantities.Add(count); }
        }

        public bool Remove(string itemId, int count = 1)
        {
            int idx = itemIds.IndexOf(itemId);
            if (idx < 0 || quantities[idx] < count) return false;
            quantities[idx] -= count;
            if (quantities[idx] <= 0) { itemIds.RemoveAt(idx); quantities.RemoveAt(idx); }
            return true;
        }

        public int CountOf(string itemId)
        {
            int idx = itemIds.IndexOf(itemId);
            return idx >= 0 ? quantities[idx] : 0;
        }

        public bool Has(string itemId) => CountOf(itemId) > 0;

        public int TotalItems => quantities.Sum();

        public IEnumerable<(string id, int qty)> Entries() =>
            itemIds.Select((id, i) => (id, quantities[i]));

        // Move `count` of an item from this inventory into another. Returns true on success.
        public bool TransferTo(Inventory other, string itemId, int count = 1)
        {
            if (other == null || !Remove(itemId, count)) return false;
            other.Add(itemId, count);
            return true;
        }
    }
}