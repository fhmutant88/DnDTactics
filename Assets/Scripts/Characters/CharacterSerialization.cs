using DnDTactics.Rules;
using DnDTactics.Data;

namespace DnDTactics.Characters
{
    // Converts between a live Character and its serializable CharacterData snapshot.
    public static class CharacterSerialization
    {
        public static CharacterData ToData(Character c)
        {
            return new CharacterData
            {
                characterName = c.characterName,
                speciesId = c.species != null ? c.species.name : "",
                classId = c.characterClass != null ? c.characterClass.name : "",
                backgroundId = c.background != null ? c.background.name : "",
                level = c.level,
                currentXp = c.currentXp,
                currentHP = c.currentHP,
                isDown = c.IsDown,
                raiseDeadPenalty = c.raiseDeadPenalty,
                baseScores = (int[])c.abilities.baseScores.Clone(),
                backgroundBonuses = (int[])c.abilities.backgroundBonuses.Clone()
            };
        }

        public static Character FromData(CharacterData d)
        {
            var db = ContentDatabase.Instance;
            var species = string.IsNullOrEmpty(d.speciesId) ? null : db.GetSpecies(d.speciesId);
            var cls = string.IsNullOrEmpty(d.classId) ? null : db.GetClass(d.classId);
            var bg = string.IsNullOrEmpty(d.backgroundId) ? null : db.GetBackground(d.backgroundId);

            var abilities = new AbilityScoreSet();
            if (d.baseScores != null)
                for (int i = 0; i < 6 && i < d.baseScores.Length; i++)
                    abilities.baseScores[i] = d.baseScores[i];
            if (d.backgroundBonuses != null)
                for (int i = 0; i < 6 && i < d.backgroundBonuses.Length; i++)
                    abilities.backgroundBonuses[i] = d.backgroundBonuses[i];

            // Construct, then restore the exact saved state (the constructor would
            // otherwise reset HP to full and XP to the level floor).
            var c = new Character(d.characterName, species, cls, bg, abilities, d.level);
            c.RestoreState(d.currentXp, d.currentHP, d.isDown);
            c.raiseDeadPenalty = d.raiseDeadPenalty;
            return c;
        }
    }
}