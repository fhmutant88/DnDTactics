using System.Linq;
using DnDTactics.Rules;
using DnDTactics.Persistence;

namespace DnDTactics.Characters
{
    public static class RestService
    {
        public struct RestResult { public bool tookRest; public string message; }

        // A long rest: advances the clock, full-heals living members, and ages the fallen.
        // Returns deaths that occurred so the UI can report them.
        public static RestResult LongRest(SaveSlot slot)
        {
            if (slot == null) return new RestResult { tookRest = false, message = "No active slot." };

            slot.party.longRestsTaken++;
            int now = slot.party.longRestsTaken;
            int healed = 0, died = 0;

            foreach (var m in slot.barracks.members)
            {
                if (m.IsAlive)
                {
                    // Long rest restores HP to full for living members.
                    if (m.character.currentHP < m.character.MaxHP)
                    {
                        m.character.Heal(m.character.MaxHP - m.character.currentHP);
                        healed++;
                    }
                }
                else if (m.status == MemberStatus.Down)
                {
                    int age = now - m.fellAtLongRest;
                    if (!RevivalTiming.StillRevivable(age))
                    {
                        m.status = MemberStatus.Dead; // window closed → permanent death
                        died++;
                    }
                }
            }

            string msg = $"Long rest taken (total {now}). Healed {healed} member(s)." +
                         (died > 0 ? $" {died} fallen member(s) passed beyond revival." : "");
            return new RestResult { tookRest = true, message = msg };
        }
    }
}