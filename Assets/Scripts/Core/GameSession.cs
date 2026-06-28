using UnityEngine;
using DnDTactics.Persistence;

namespace DnDTactics.Core
{
    // The single persistent object that survives scene loads and carries the active
    // save slot (and which barracks member is being edited, etc.) between screens.
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }
        public int RunDepth { get; set; } = 1; // which dungeon of the current run (1-based)

        // The save slot currently being played. Null at the main menu before one is chosen.
        public SaveSlot ActiveSlot { get; set; }

        void Awake()
        {
            // Enforce a single instance that persists across all scenes.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Convenience: save the active slot to disk.
        public void SaveActive()
        {
            if (ActiveSlot != null) SaveManager.Save(ActiveSlot);
        }

        public bool HasActiveSlot => ActiveSlot != null;
    }
}