using System.Collections.Generic;
using System.Linq;
using DnDTactics.Rules;

namespace DnDTactics.Combat
{
    // Rolls initiative, sorts the order, and tracks whose turn it is plus the round number.
    public class TurnOrder
    {
        public List<InitiativeEntry> Entries { get; private set; } = new();
        public int CurrentIndex { get; private set; }
        public int Round { get; private set; }

        public InitiativeEntry Current =>
            Entries.Count > 0 ? Entries[CurrentIndex] : null;

        // Roll d20 + Dex for each combatant and sort highest-first.
        public void Roll(IEnumerable<Combatant> combatants)
        {
            Entries.Clear();
            foreach (var c in combatants)
            {
                int dexMod = c.Character.AbilityModifier(Ability.Dexterity);
                int d20 = UnityEngine.Random.Range(1, 21); // 1..20
                Entries.Add(new InitiativeEntry(c, d20, dexMod));
            }

            // Sort by total desc, then Dex modifier desc as the tie-breaker.
            Entries = Entries
                .OrderByDescending(e => e.total)
                .ThenByDescending(e => e.dexModifier)
                .ToList();

            CurrentIndex = 0;
            Round = 1;
        }

        // Advance to the next combatant; wrap to the top and bump the round.
        public InitiativeEntry Advance()
        {
            if (Entries.Count == 0) return null;
            CurrentIndex++;
            if (CurrentIndex >= Entries.Count)
            {
                CurrentIndex = 0;
                Round++;
            }
            return Current;
        }

        // Skip a removed/dead combatant without breaking the index. (Used later.)
        public void Remove(Combatant c)
        {
            int idx = Entries.FindIndex(e => e.combatant == c);
            if (idx < 0) return;
            Entries.RemoveAt(idx);
            if (idx < CurrentIndex) CurrentIndex--;
            if (CurrentIndex >= Entries.Count) CurrentIndex = 0;
        }
    }
}