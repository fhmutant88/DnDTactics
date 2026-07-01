using System.Collections.Generic;

namespace DnDTactics.Combat
{
    // Collects advantage/disadvantage sources for one attack, then resolves the
    // 5e cancellation rule: ANY advantage + ANY disadvantage = flat roll (they don't
    // stack, they cancel). Callers add sources (with a reason for the log); the
    // resolver reads the net result. This is the ONE place adv/disadv is decided,
    // so every future source (unseen target, prone, Help, ranged-in-melee) is just
    // an AddAdvantage/AddDisadvantage call — never a change to AttackResolver.
    public class AttackContext
    {
        private readonly List<string> advReasons = new();
        private readonly List<string> disReasons = new();

        public void AddAdvantage(string reason) => advReasons.Add(reason);
        public void AddDisadvantage(string reason) => disReasons.Add(reason);

        public bool HasAdvantage => advReasons.Count > 0;
        public bool HasDisadvantage => disReasons.Count > 0;

        // Forced critical hit (e.g. melee vs. a paralyzed target): if the attack HITS, it crits,
        // regardless of the d20. Not a roll modifier — a result modifier. Collected like adv/disadv.
        private readonly List<string> forcedCritReasons = new();
        public void AddForcedCrit(string reason) => forcedCritReasons.Add(reason);
        public bool HasForcedCrit => forcedCritReasons.Count > 0;
        public string ForcedCritReason => forcedCritReasons.Count > 0 ? forcedCritReasons[0] : null;

        // 5e cancellation: one of each cancels to flat, regardless of counts.
        public RollMode NetRollMode
        {
            get
            {
                if (HasAdvantage && !HasDisadvantage) return RollMode.Advantage;
                if (HasDisadvantage && !HasAdvantage) return RollMode.Disadvantage;
                return RollMode.Flat;
            }
        }

        // For the combat log: a short description of why, e.g. "advantage (prone target)".
        public string DescribeNet()
        {
            var mode = NetRollMode;
            if (mode == RollMode.Flat)
            {
                // Flat could mean "no sources" or "sources cancelled" — distinguish for clarity.
                if (HasAdvantage && HasDisadvantage)
                    return "flat (advantage & disadvantage cancel)";
                return "flat";
            }
            var reasons = mode == RollMode.Advantage ? advReasons : disReasons;
            return $"{(mode == RollMode.Advantage ? "advantage" : "disadvantage")} ({string.Join(", ", reasons)})";
        }
    }

    public enum RollMode { Flat, Advantage, Disadvantage }
}