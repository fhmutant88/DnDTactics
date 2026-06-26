using UnityEngine;
using DnDTactics.Rules;
using DnDTactics.Data;
using DnDTactics.Characters;
using DnDTactics.Persistence;

namespace DnDTactics.Tests
{
    public class BarracksSaveTester : MonoBehaviour
    {
        public Species species; public CharacterClass cls; public Background background;

        void Start()
        {
            // Clean any prior test slots so reruns start fresh.
            foreach (var s in SaveManager.ListSlots())
                if (s.displayName.StartsWith("TEST ")) SaveManager.Delete(s.fileId);

            var slot = SaveManager.CreateSlot("TEST Grim's Run");

            // Add two characters; mark one as Down (permadeath check).
            var a = slot.barracks.Add(MakeHero("Aldric", 3));
            var b = slot.barracks.Add(MakeHero("Mira", 5));
            b.character.TakeDamage(9999);     // drop Mira
            b.status = MemberStatus.Down;

            SaveManager.Save(slot);
            Debug.Log($"Saved {slot.barracks.members.Count} members. Slots used: {SaveManager.ListSlots().Count}/{SaveManager.MaxSlots}");

            // Reload from disk and verify.
            var loaded = SaveManager.Load(slot.slotFileId);
            Debug.Log($"Loaded '{loaded.displayName}' with {loaded.barracks.members.Count} members:");
            foreach (var m in loaded.barracks.members)
                Debug.Log($"  [{m.id.Substring(0, 8)}] {m.character.characterName} " +
                          $"L{m.character.level} HP {m.character.currentHP}/{m.character.MaxHP} " +
                          $"status={m.status} down={m.character.IsDown}");

            var mira = loaded.barracks.members.Find(m => m.character.characterName == "Mira");
            Debug.Log(mira != null && mira.status == MemberStatus.Down && mira.character.IsDown
                ? "<color=green>BARRACKS ROUND-TRIP ✓ (Mira reloaded as Down)</color>"
                : "<color=red>BARRACKS ROUND-TRIP ✗</color>");
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