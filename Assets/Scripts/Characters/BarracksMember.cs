using System;

namespace DnDTactics.Characters
{
    public enum MemberStatus
    {
        Available,  // alive, in the barracks, not currently deployed
        Deployed,   // alive and part of the active party for the current run
        Down,       // dropped to 0 HP, awaiting revival (still recoverable)
        Dead        // permanently dead (revival window passed / buried)
    }

    // A character's entry in the barracks: the live Character plus roster bookkeeping.
    // The 'id' is a stable GUID so the Party can reference this member across save/load.
    public class BarracksMember
    {
        public string id;
        public Character character;
        public MemberStatus status;

        public BarracksMember(Character character, string id = null)
        {
            this.character = character;
            this.id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
            this.status = MemberStatus.Available;
        }

        public bool IsAlive => status == MemberStatus.Available || status == MemberStatus.Deployed;
    }

    // Serializable snapshot of a barracks member (id + status + the character snapshot).
    [Serializable]
    public class BarracksMemberData
    {
        public string id;
        public int status;          // (int)MemberStatus
        public CharacterData character;
    }
}