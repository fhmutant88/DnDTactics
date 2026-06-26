using DnDTactics.Rules;

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
    }
}