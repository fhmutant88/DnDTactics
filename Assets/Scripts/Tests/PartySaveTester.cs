using UnityEngine;
using System.Linq;
using DnDTactics.Rules;
using DnDTactics.Data;
using DnDTactics.Characters;
using DnDTactics.Persistence;


namespace DnDTactics.Tests
{
    public class PartySaveTester : MonoBehaviour
    {
        public Species species; public CharacterClass cls; public Background background;

        void Start()
        {
            foreach (var s in SaveManager.ListSlots())
                if (s.displayName.StartsWith("PTEST ")) SaveManager.Delete(s.fileId);

            var slot = SaveManager.CreateSlot("PTEST Party Run");
            var a = slot.barracks.Add(MakeHero("Aldric", 3));
            var b = slot.barracks.Add(MakeHero("Mira", 4));
            var c = slot.barracks.Add(MakeHero("Joss", 2)); // stays in reserve

            slot.party.Deploy(slot.barracks, a.id);
            slot.party.Deploy(slot.barracks, b.id);
            slot.party.gold = 150;

            SaveManager.Save(slot);

            var loaded = SaveManager.Load(slot.slotFileId);
            Debug.Log($"Loaded '{loaded.displayName}': gold={loaded.party.gold}, " +
                      $"party size={loaded.party.Size}, barracks={loaded.barracks.members.Count}");
            foreach (var m in loaded.party.Members(loaded.barracks))
                Debug.Log($"  Deployed: {m.character.characterName} (status={m.status})");

            bool ok = loaded.party.Size == 2 && loaded.party.gold == 150 &&
                      loaded.party.Members(loaded.barracks).All(m => m.status == MemberStatus.Deployed) &&
                      loaded.barracks.members.Count == 3;
            Debug.Log(ok ? "<color=green>PARTY ROUND-TRIP ✓</color>"
                         : "<color=red>PARTY ROUND-TRIP ✗</color>");
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