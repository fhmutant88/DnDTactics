using System;
using System.Collections.Generic;
using System.Linq;
using DnDTactics.Characters;

namespace DnDTactics.Persistence
{
    // The current-run selection within a slot: which barracks members are deployed,
    // shared gold, and run state. References members by id (does NOT copy them).
    public class Party
    {
        public const int MaxSize = 4;

        public List<string> memberIds = new();  // GUIDs into the slot's barracks
        
        // Run state (used once runs are wired up).
        public int dungeonSeed;
        public int runDepth;       // how many encounters deep this run is
        public string difficulty;  // stored as string; maps to your Difficulty enum

        public int Size => memberIds.Count;
        public bool IsFull => memberIds.Count >= MaxSize;
        public string leaderId;  // the "active" character; gold rewards land here, pays fees

        // Ensures there's a valid leader among the deployed members; picks the first if needed.
        public string EnsureLeader(Barracks barracks)
        {
            bool valid = !string.IsNullOrEmpty(leaderId) &&
                         memberIds.Contains(leaderId) &&
                         barracks.GetById(leaderId) != null;
            if (!valid)
                leaderId = LivingMembers(barracks).Select(m => m.id).FirstOrDefault();
            return leaderId;
        }

        public bool SetLeader(string memberId)
        {
            if (!memberIds.Contains(memberId)) return false;
            leaderId = memberId;
            return true;
        }

        // ---- forming the party (operates against the slot's barracks) ----

        public bool Deploy(Barracks barracks, string memberId)
        {
            if (IsFull) return false;
            if (memberIds.Contains(memberId)) return false;

            var m = barracks.GetById(memberId);
            if (m == null || m.status != MemberStatus.Available) return false;

            m.status = MemberStatus.Deployed;
            memberIds.Add(memberId);
            return true;
        }

        public bool Recall(Barracks barracks, string memberId)
        {
            if (!memberIds.Remove(memberId)) return false;
            var m = barracks.GetById(memberId);
            // Only a living member returns to Available; the fallen keep Down/Dead.
            if (m != null && m.status == MemberStatus.Deployed)
                m.status = MemberStatus.Available;
            return true;
        }

        // Resolve ids to live members from the slot's barracks.
        public IEnumerable<BarracksMember> Members(Barracks barracks) =>
            memberIds.Select(barracks.GetById).Where(m => m != null);

        // Living deployed members (the ones who can actually fight).
        public IEnumerable<BarracksMember> LivingMembers(Barracks barracks) =>
            Members(barracks).Where(m => m.IsAlive);

        // ---- serialization ----

        public PartyData ToData() => new PartyData
        {
            memberIds = new List<string>(memberIds),
            leaderId = leaderId,
            dungeonSeed = dungeonSeed,
            runDepth = runDepth,
            difficulty = difficulty
        };

        public static Party FromData(PartyData d)
        {
            var p = new Party();
            if (d == null) return p;
            p.memberIds = d.memberIds != null ? new List<string>(d.memberIds) : new List<string>();
            p.leaderId = d.leaderId;
            p.dungeonSeed = d.dungeonSeed;
            p.runDepth = d.runDepth;
            p.difficulty = d.difficulty;
            return p;
        }
    }

    [Serializable]
    public class PartyData
    {
        public List<string> memberIds = new();
        public string leaderId;
        public int dungeonSeed;
        public int runDepth;
        public string difficulty;
    }
}