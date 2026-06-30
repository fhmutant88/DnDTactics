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