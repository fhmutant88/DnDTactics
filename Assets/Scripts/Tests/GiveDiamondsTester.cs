using UnityEngine;
using DnDTactics.Core;
using DnDTactics.Characters;

namespace DnDTactics.Tests
{
    // Throwaway: drops diamonds on the party leader of the active slot so you can test revival.
    public class GiveDiamondsTester : MonoBehaviour
    {
        public int revivifyCount = 2;
        public int raiseDeadCount = 2;

        void Start()
        {
            var slot = GameSession.Instance != null ? GameSession.Instance.ActiveSlot : null;
            if (slot == null) { Debug.LogWarning("No active slot — load a slot first."); return; }

            string leaderId = slot.party.EnsureLeader(slot.barracks);
            var leader = leaderId != null ? slot.barracks.GetById(leaderId) : null;
            if (leader == null) { Debug.LogWarning("No leader in party."); return; }

            leader.inventory.Add("RevivifyDiamond", revivifyCount);
            leader.inventory.Add("RaiseDeadDiamond", raiseDeadCount);
            GameSession.Instance.SaveActive();
            Debug.Log($"Gave {leader.character.characterName} {revivifyCount}x Revivify + " +
                      $"{raiseDeadCount}x Raise Dead diamonds. Saved.");
        }
    }
}