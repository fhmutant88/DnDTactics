using System.Collections.Generic;
using UnityEngine;

namespace DnDTactics.Data
{
    // A registry of content assets, used to re-link a saved character's references
    // (saved as string ids) back to the real assets on load. Lives in Resources so
    // it can be loaded from anywhere without a scene reference.
    [CreateAssetMenu(fileName = "ContentDatabase", menuName = "DnD/Content Database")]
    public class ContentDatabase : ScriptableObject
    {
        public List<Species> species = new();
        public List<CharacterClass> classes = new();
        public List<Background> backgrounds = new();

        public Species GetSpecies(string id) => Find(species, id);
        public CharacterClass GetClass(string id) => Find(classes, id);
        public Background GetBackground(string id) => Find(backgrounds, id);

        // We key by the asset's filename (its .name).
        static T Find<T>(List<T> list, string id) where T : Object
        {
            foreach (var item in list)
                if (item != null && item.name == id) return item;
            Debug.LogWarning($"ContentDatabase: no asset with id '{id}'.");
            return null;
        }

        // ---- global access ----
        private static ContentDatabase _instance;
        public static ContentDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ContentDatabase>("ContentDatabase");
                    if (_instance == null)
                        Debug.LogError("ContentDatabase not found at Assets/Resources/ContentDatabase.asset");
                }
                return _instance;
            }
        }
    }
}