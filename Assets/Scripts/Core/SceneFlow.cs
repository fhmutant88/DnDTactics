using UnityEngine;
using UnityEngine.SceneManagement;

namespace DnDTactics.Core
{
    // Central place for scene names + navigation, so screens don't hardcode strings.
    public static class SceneFlow
    {
        // These MUST match your scene file names in Assets/Scenes exactly.
        public const string MainMenu = "MainMenu";
        public const string CharacterCreation = "CharacterCreation";
        public const string Roster = "Roster";
        public const string Transfer = "Transfer";
        public const string Encounter = "Encounter";
        public const string Exploration = "Exploration";

        public static void Go(string sceneName) => SceneManager.LoadScene(sceneName);
    }
}