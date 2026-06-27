using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DnDTactics.Core;
using DnDTactics.Persistence;
using DnDTactics.Characters;
using DnDTactics.Procgen;

namespace DnDTactics.UI
{
    // Minimal exploration HUD: shows the party's Portal Scroll count and a Use Portal button.
    // Using it banks the run and returns to town. Exploration-only (between fights).
    public class ExplorationHUD : MonoBehaviour
    {
        public ExplorationEncounters encounters; // to check if we're mid-combat
        public DnDTactics.Procgen.ExplorationManager exploration;
        TMP_Text selectedText;

        Canvas canvas;
        Button portalButton;
        TMP_Text portalLabel, infoText;

        Button modeButton;
        TMP_Text modeLabel;

        SaveSlot Slot => GameSession.Instance != null ? GameSession.Instance.ActiveSlot : null;

        void Start()
        {
            if (encounters == null) encounters = FindFirstObjectByType<ExplorationEncounters>();
            if (exploration == null) exploration = FindFirstObjectByType<ExplorationManager>();
            EnsureEventSystem();
            BuildCanvas();
            BuildUI();
        }

        void Update()
        {
            int scrolls = CountScrolls();
            bool inCombat = encounters != null && encounters.InCombat;
            portalLabel.text = $"Use Portal ({scrolls})";
            portalButton.interactable = scrolls > 0 && !inCombat;
            infoText.text = inCombat ? "In combat — can't portal."
                          : "Exploring. Portal to bank this run and return to town.";

            if (selectedText != null && exploration != null)
                selectedText.text = $"Selected: {exploration.SelectedName}";
            if (modeLabel != null && exploration != null)
                modeLabel.text = $"Mode: {exploration.ModeName}";
        }

        int CountScrolls()
        {
            if (Slot == null) return 0;
            return Slot.party.Members(Slot.barracks).Sum(m => m.inventory.CountOf("PortalScroll"));
        }

        void UsePortal()
        {
            if (Slot == null) return;
            // Consume one scroll from the first holder.
            var holder = Slot.party.Members(Slot.barracks)
                             .FirstOrDefault(m => m.inventory.Has("PortalScroll"));
            if (holder == null) return;
            holder.inventory.Remove("PortalScroll", 1);

            GameSession.Instance.SaveActive(); // bank the run state (gold/items/scroll spent)
            Debug.Log($"Portal used by {holder.character.characterName} — run banked, returning to town.");
            SceneFlow.Go(SceneFlow.Roster);
        }

        // ---- building ----
        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>();
            }
        }

        void BuildCanvas()
        {
            var go = new GameObject("Exploration_Canvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
        }

        void BuildUI()
        {
            infoText = MakeText("Info", 22, TextAlignmentOptions.Center);
            Anchor(infoText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(900, 30));

            var go = new GameObject("PortalButton", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            go.AddComponent<Image>().color = new Color(0.4f, 0.3f, 0.55f, 0.95f);
            portalButton = go.AddComponent<Button>();
            Anchor(go.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-20, 20), new Vector2(220, 54));
            portalButton.onClick.AddListener(UsePortal);

            portalLabel = MakeText("Label", 22, TextAlignmentOptions.Center);
            portalLabel.transform.SetParent(go.transform, false);
            var rt = portalLabel.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            selectedText = MakeText("Selected", 24, TextAlignmentOptions.Left);
            Anchor(selectedText.rectTransform, new Vector2(0f, 1f), new Vector2(20, -20), new Vector2(500, 32));

            var mgo = new GameObject("ModeButton", typeof(RectTransform));
            mgo.transform.SetParent(canvas.transform, false);
            mgo.AddComponent<Image>().color = new Color(0.3f, 0.45f, 0.4f, 0.95f);
            modeButton = mgo.AddComponent<Button>();
            Anchor(mgo.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(20, 20), new Vector2(220, 54));
            modeButton.onClick.AddListener(() => { if (exploration != null) exploration.ToggleMode(); });

            modeLabel = MakeText("ModeLabel", 22, TextAlignmentOptions.Center);
            modeLabel.transform.SetParent(mgo.transform, false);
            var mrt = modeLabel.rectTransform;
            mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one; mrt.offsetMin = Vector2.zero; mrt.offsetMax = Vector2.zero;
        }

        TMP_Text MakeText(string name, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size; t.alignment = align; t.color = Color.white; t.raycastTarget = false;
            return t;
        }

        void Anchor(RectTransform rt, Vector2 a, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = a; rt.anchorMax = a; rt.pivot = a; rt.anchoredPosition = pos; rt.sizeDelta = size;
        }
    }
}