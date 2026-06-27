using System.Linq;
using DnDTactics.Rules;
using DnDTactics.Persistence;

namespace DnDTactics.Characters
{
    public struct RevivalResult
    {
        public bool success;
        public string message;
    }

    public static class RevivalService
    {
        // Town healer: payer (usually the leader) funds reviving a Down member.
        public static RevivalResult TownRevive(BarracksMember target, BarracksMember payer)
        {
            if (target == null) return Fail("No target selected.");
            if (target.status != MemberStatus.Down)
                return Fail($"{target.character.characterName} is not Down (can't be revived).");
            if (payer == null) return Fail("No payer (set a party leader).");

            int cost = Revival.TownHealerCost(target.character.level);
            if (payer.gold < cost)
                return Fail($"Need {cost} gold; {payer.character.characterName} has {payer.gold}.");

            payer.gold -= cost;
            int hp = Revival.RevivedHP(target.character.MaxHP);
            target.character.Revive(hp);
            // Returning member is alive again. If they were deployed, they remain deployed;
            // otherwise available.
            target.status = MemberStatus.Available;

            return new RevivalResult
            {
                success = true,
                message = $"{target.character.characterName} revived for {cost} gold " +
                          $"(HP {hp}/{target.character.MaxHP}). " +
                          $"{payer.character.characterName}'s purse: {payer.gold}."
            };
        }

        static RevivalResult Fail(string msg) => new RevivalResult { success = false, message = msg };


        // Field revival: a living party member's diamond revives a Down ally.
        // Path is chosen by the window the clock enforces. Returns what happened.
        public static RevivalResult FieldRevive(
            BarracksMember target, Party party, Barracks barracks)
        {
            if (target == null) return Fail("No target selected.");
            if (target.status != MemberStatus.Down)
                return Fail($"{target.character.characterName} is not Down.");

            int age = party.longRestsTaken - target.fellAtLongRest;
            if (!RevivalTiming.StillRevivable(age))
                return Fail($"{target.character.characterName} is beyond revival ({age} long rests).");

            bool fastOpen = RevivalTiming.FastPathOpen(age);

            // Try the fast path first if its window is open and a Revivify Diamond exists.
            if (fastOpen && FindHolder(party, barracks, "RevivifyDiamond", out var fastHolder))
            {
                fastHolder.inventory.Remove("RevivifyDiamond", 1);
                target.character.Revive(Revival.RevivedHP(target.character.MaxHP));
                target.status = party.memberIds.Contains(target.id)
                    ? MemberStatus.Deployed : MemberStatus.Available;
                return Ok($"{target.character.characterName} revived (Revivify) by " +
                          $"{fastHolder.character.characterName}'s diamond — no penalty.");
            }

            // Otherwise the slow path: Raise Dead Diamond, within the 10-rest window, with penalty.
            if (FindHolder(party, barracks, "RaiseDeadDiamond", out var slowHolder))
            {
                slowHolder.inventory.Remove("RaiseDeadDiamond", 1);
                target.character.Revive(1); // returns at 1 HP
                target.character.ApplyRaiseDeadPenalty(); // 4 long rests to recover
                target.status = party.memberIds.Contains(target.id)
                    ? MemberStatus.Deployed : MemberStatus.Available;
                return Ok($"{target.character.characterName} raised (Raise Dead) by " +
                          $"{slowHolder.character.characterName}'s diamond — returns at 1 HP, " +
                          $"weakened for 4 long rests.");
            }

            // No usable diamond.
            return Fail(fastOpen
                ? "Need a Revivify or Raise Dead Diamond in the party."
                : "Fast window passed — need a Raise Dead Diamond in the party.");
        }

        // Finds a LIVING party member holding the given item. 
        static bool FindHolder(Party party, Barracks barracks, string itemId, out BarracksMember holder)
        {
            holder = party.LivingMembers(barracks).FirstOrDefault(m => m.inventory.Has(itemId));
            return holder != null;
        }

        static RevivalResult Ok(string msg) => new RevivalResult { success = true, message = msg };

        }   
     
    }