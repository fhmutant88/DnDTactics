using System;

namespace DnDTactics.Characters
{
    // A plain, serializable snapshot of a Character for saving. Stores asset references
    // as string ids (filenames) so they survive a save/load cycle.
    [Serializable]
    public class CharacterData
    {
        public string characterName;
        public string speciesId;
        public string classId;
        public string backgroundId;
        public int level;
        public int currentXp;
        public int currentHP;
        public bool isDown;
        public int[] baseScores;
        public int[] backgroundBonuses;
    }
}