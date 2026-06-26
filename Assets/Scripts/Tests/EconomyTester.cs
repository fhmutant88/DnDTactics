using UnityEngine;
using System.Linq;
using DnDTactics.Rules;
using DnDTactics.Data;
using DnDTactics.Characters;
using DnDTactics.Persistence;

namespace DnDTactics.Tests
{
    public class EconomyTester : MonoBehaviour
    {
        public Species species; public CharacterClass cls; public Background background;

        void Start()
        {
            foreach (var s in SaveManager.ListSlots())
                if (s.displayName.StartsWith("ETEST ")) SaveManager.Delete(s.fileId);

            var slot = SaveManager.CreateSlot("ETEST Economy");
            var a = slot.barracks.Add(MakeHero("Aldric", 3));
            var b = slot.barracks.Add(MakeHero("Mira", 3));
            slot.party.Deploy(slot.barracks, a.id);
            slot.party.Deploy(slot.barracks, b.id);
            slot.party.SetLeader(a.id);

            // Give the leader some gold and an item.
            a.gold = 250;
            a.inventory.Add("HealingPotion", 2);
            a.inventory.Add("RevivifyDiamond", 1);

            SaveManager.Save(slot);
            var loaded = SaveManager.Load(slot.slotFileId);

            var la = loaded.barracks.GetById(a.id);
            var lb = loaded.barracks.GetById(b.id);
            Debug.Log($"Leader id matches: {loaded.party.leaderId == a.id}");
            Debug.Log($"Aldric gold={la.gold}, potions={la.inventory.CountOf("HealingPotion")}, " +
                      $"diamonds={la.inventory.CountOf("RevivifyDiamond")}");
            Debug.Log($"Mira gold={lb.gold} (should be 0 — gold is per-character)");

            bool ok = loaded.party.leaderId == a.id &&
                      la.gold == 250 && la.inventory.CountOf("HealingPotion") == 2 &&
                      la.inventory.CountOf("RevivifyDiamond") == 1 && lb.gold == 0;
            Debug.Log(ok ? "<color=green>ECONOMY ROUND-TRIP ✓</color>"
                         : "<color=red>ECONOMY ROUND-TRIP ✗</color>");
        }

        Character MakeHero(string name, int level)
        {
            var ab = new AbilityScoreSet();
            int[] arr = AbilityScoreGeneration.StandardArray();
            Ability[] all = (Ability[])System.Enum.GetValues(typeof(Ability));
            for (int i = 0; i < all.Length; i++) ab.SetBaseScore(all[i], arr[i]);
            return new Character(name, species, cls, background, ab, level);
        }
    }
}