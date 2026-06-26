using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DnDTactics.Combat;

namespace DnDTactics.UI
{
    // A self-building combat HUD: banner, End Turn button, selected-unit panel, and
    // floating HP bars. Reads CombatManager each frame. Placeholder visuals (art pass later).
    public class CombatHUD : MonoBehaviour
    {
        public CombatManager combat;
        public float hpBarWorldYOffset = 2.2f; // height above each token

        private Camera cam;
        private Canvas canvas;
        private TMP_Text banner;
        private TMP_Text selectedPanel;
        private Button endTurnButton;

        private readonly Dictionary<Combatant, RectTransform> barRoots = new();
        private readonly Dictionary<Combatant, RectTransform> barFills = new();

        void Start()
        {
            if (combat == null) combat = FindFirstObjectByType<CombatManager>();
            cam = Camera.main;
            EnsureEventSystem();
            BuildCanvas();
            banner = MakeText("Banner", canvas.transform, 26, TextAlignmentOptions.Center);
            Anchor(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(800, 44));
            BuildSelectedPanel();
            BuildEndTurnButton();
        }

        void Update()
        {
            if (combat == null) return;
            if (cam == null) cam = Camera.main;
            UpdateBanner();
            UpdateSelectedPanel();
            endTurnButton.interactable = combat.IsPlayerTurn;
            UpdateHealthBars();
        }

        // ---------- builders ----------

        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        void BuildCanvas()
        {
            var go = new GameObject("CombatHUD_Canvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
        }

        TMP_Text MakeText(string name, Transform parent, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.raycastTarget = false; // text shouldn't eat clicks
            return t;
        }

        void Anchor(RectTransform rt, Vector2 anchorPivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorPivot;
            rt.anchorMax = anchorPivot;
            rt.pivot = anchorPivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        void BuildSelectedPanel()
        {
            var bg = new GameObject("SelectedPanel", typeof(RectTransform));
            bg.transform.SetParent(canvas.transform, false);
            var img = bg.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.55f);
            Anchor(bg.GetComponent<RectTransform>(), new Vector2(0f, 0f),
                   new Vector2(16, 16), new Vector2(260, 140));

            selectedPanel = MakeText("SelectedText", bg.transform, 20, TextAlignmentOptions.TopLeft);
            var rt = selectedPanel.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(10, 10); rt.offsetMax = new Vector2(-10, -10);
        }

        void BuildEndTurnButton()
        {
            var go = new GameObject("EndTurnButton", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.35f, 0.6f, 0.95f);
            endTurnButton = go.AddComponent<Button>();
            Anchor(go.GetComponent<RectTransform>(), new Vector2(1f, 0f),
                   new Vector2(-20, 20), new Vector2(150, 48));

            var label = MakeText("Label", go.transform, 22, TextAlignmentOptions.Center);
            label.text = "End Turn";
            var rt = label.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            endTurnButton.onClick.AddListener(() => combat.RequestEndTurn());
        }

        // ---------- per-frame updates ----------

        void UpdateBanner()
        {
            bool players = combat.Combatants.Any(c => c.Team == Team.Player);
            bool enemies = combat.Combatants.Any(c => c.Team == Team.Enemy);
            if (!players) { banner.text = "DEFEAT"; return; }
            if (!enemies) { banner.text = "VICTORY"; return; }

            var active = combat.ActiveCombatant;
            banner.text = active != null
                ? $"Round {combat.CurrentRound}  —  {active.Character.characterName}'s turn ({active.Team})"
                : "";
        }

        void UpdateSelectedPanel()
        {
            var sel = combat.Selected;
            if (sel == null) { selectedPanel.text = "No unit selected"; return; }

            var ch = sel.Character;
            string s = $"{ch.characterName}  ({sel.Team})\nHP {ch.currentHP}/{ch.MaxHP}\nAC {ch.ArmorClass}";
            if (sel == combat.ActiveCombatant)
                s += $"\nMove {combat.ActiveMovementRemaining} ft" +
                     $"\nAction: {(combat.ActiveActionAvailable ? "available" : "used")}";
            selectedPanel.text = s;
        }

        void UpdateHealthBars()
        {
            // Remove bars for combatants that are gone.
            var live = combat.Combatants;
            var stale = barRoots.Keys.Where(c => c == null || !live.Contains(c)).ToList();
            foreach (var c in stale)
            {
                if (barRoots[c] != null) Destroy(barRoots[c].gameObject);
                barRoots.Remove(c);
                barFills.Remove(c);
            }

            foreach (var c in live)
            {
                if (!barRoots.ContainsKey(c)) CreateBar(c);

                var root = barRoots[c];
                Vector3 sp = cam.WorldToScreenPoint(c.transform.position + Vector3.up * hpBarWorldYOffset);
                if (sp.z < 0f) { root.gameObject.SetActive(false); continue; }
                root.gameObject.SetActive(true);
                root.position = sp;

                float frac = c.Character.MaxHP > 0
                    ? Mathf.Clamp01((float)c.Character.currentHP / c.Character.MaxHP)
                    : 0f;
                barFills[c].sizeDelta = new Vector2(52f * frac, 7f);
            }
        }

        void CreateBar(Combatant c)
        {
            var bg = new GameObject($"HPBar", typeof(RectTransform));
            bg.transform.SetParent(canvas.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.6f);
            bgImg.raycastTarget = false;
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(54, 9);

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(bg.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = c.Team == Team.Player
                ? new Color(0.3f, 0.85f, 0.35f)
                : new Color(0.9f, 0.3f, 0.3f);
            fillImg.raycastTarget = false;
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0.5f);
            fillRect.anchorMax = new Vector2(0f, 0.5f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(1f, 0f);
            fillRect.sizeDelta = new Vector2(52f, 7f);

            barRoots[c] = bgRect;
            barFills[c] = fillRect;
        }
    }
}