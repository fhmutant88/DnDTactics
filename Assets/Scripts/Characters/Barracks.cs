using System;
using System.Collections.Generic;
using System.Linq;

namespace DnDTactics.Characters
{
    // The per-slot stable of all characters in one playthrough.
    public class Barracks
    {
        public List<BarracksMember> members = new();

        public BarracksMember Add(Character character)
        {
            var m = new BarracksMember(character);
            members.Add(m);
            return m;
        }

        public BarracksMember GetById(string id) => members.FirstOrDefault(m => m.id == id);

        public IEnumerable<BarracksMember> Available =>
            members.Where(m => m.status == MemberStatus.Available);

        public IEnumerable<BarracksMember> Deployed =>
            members.Where(m => m.status == MemberStatus.Deployed);

        public void Remove(string id)
        {
            var m = GetById(id);
            if (m != null) members.Remove(m);
        }

        // ---- serialization ----

        public BarracksData ToData()
        {
            var data = new BarracksData();
            foreach (var m in members)
            {
                data.members.Add(new BarracksMemberData
                {
                    id = m.id,
                    status = (int)m.status,
                    gold = m.gold,
                    inventory = m.inventory,
                    fellAtLongRest = m.fellAtLongRest,
                    character = CharacterSerialization.ToData(m.character)
                });
            }
            return data;
        }

        public static Barracks FromData(BarracksData data)
        {
            var b = new Barracks();
            if (data?.members == null) return b;
            foreach (var md in data.members)
            {
                var character = CharacterSerialization.FromData(md.character);
                var member = new BarracksMember(character, md.id)
                {
                    status = (MemberStatus)md.status
                };
                member.gold = md.gold;
                member.inventory = md.inventory ?? new Inventory();
                member.fellAtLongRest = md.fellAtLongRest;
                b.members.Add(member);
            }
            return b;
        }
    }

    [Serializable]
    public class BarracksData
    {
        public List<BarracksMemberData> members = new();
    }
}