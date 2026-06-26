using UnityEngine;
using DnDTactics.Core;
using DnDTactics.Persistence;

namespace DnDTactics.Tests
{
    public class SessionPersistenceTester : MonoBehaviour
    {
        void Start()
        {
            var session = GameSession.Instance;
            if (session == null)
            {
                Debug.LogError("No GameSession in scene. Add a GameSession object first.");
                return;
            }

            if (session.ActiveSlot == null)
            {
                // First scene: create/stash a slot, then load another scene.
                session.ActiveSlot = new SaveSlot("Session Test Slot");
                Debug.Log("Stashed a slot in GameSession. Loading CharacterCreation scene...");
                SceneFlow.Go(SceneFlow.CharacterCreation);
            }
            else
            {
                // We arrived in the second scene WITH the slot still present = success.
                Debug.Log($"<color=green>SESSION PERSISTED ✓ — ActiveSlot '{session.ActiveSlot.displayName}' " +
                          $"survived the scene load.</color>");
            }
        }
    }
}