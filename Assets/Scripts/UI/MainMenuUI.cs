using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DnDTactics.Core;
using DnDTactics.Persistence;

namespace DnDTactics.UI
{
    // Self-building slot-select menu: lists up to 5 named save slots; create / load / delete.
    public class MainMenuUI : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform listРarent;     // holds one row per slot
        private TMP_InputField nameInput;
        private TMP_Text statusText;
        private readonly List<GameObject> rows = new();

        void Start()
        {
            EnsureSession();
            EnsureEventSystem();
            BuildCanvas();
            BuildHeader();
            BuildCreateBar();
            BuildSlotList();
            Refresh();
        }

        // The main menu is the birthplace of the persistent GameSession.
        void EnsureSession()
        {
            if (GameSession.Instance == null)
            {
                var go = new GameObject("GameSession");
                go.AddComponent<GameSession>();
            }
        }

        void Refresh()
        {
            foreach (var r in rows) Destroy(r);
            rows.Clear();

            var slots = SaveManager.ListSlots();
            float y = -150f;
            foreach (var info in slots)
            {
                MakeSlotRow(info, y);
                y -= 70f;
            }

            statusText.text = $"{slots.Count} / {SaveManager.MaxSlots} slots used" +
                (SaveManager.CanCreateNewSlot() ? "" : "  (full — delete one to add)");
        }

        void CreateSlot()
        {
            string name = string.IsNullOrWhiteSpace(nameInput.text) ? "New Hero Party" : nameInput.text.Trim();
            if (!SaveManager.CanCreateNewSlot()) { statusText.text = "Slot limit reached."; return; }

            var slot = SaveManager.CreateSlot(name);
            GameSession.Instance.ActiveSlot = slot;
            // New slot has an empty barracks → go make a character.
            SceneFlow.Go(SceneFlow.CharacterCreation);
        }

        void LoadSlot(string fileId)
        {
            var slot = SaveManager.Load(fileId);
            if (slot == null) { statusText.text = "Failed to load slot."; return; }
            GameSession.Instance.ActiveSlot = slot;
            // Existing slot → go to the roster to form/launch a party.
            SceneFlow.Go(SceneFlow.Roster);
        }

        void LoadManualSlot(string fileId)
        {
            var slot = SaveManager.LoadManual(fileId);
            if (slot == null) { statusText.text = "No manual save to load."; return; }
            GameSession.Instance.ActiveSlot = slot;
            // The manual checkpoint is now the live state → go to the roster.
            SceneFlow.Go(SceneFlow.Roster);
        }

        void DeleteSlot(string fileId)
        {
            SaveManager.Delete(fileId);
            Refresh();
        }

        // ---------- UI building ----------

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
            var go = new GameObject("MainMenu_Canvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
        }

        void BuildHeader()
        {
            var title = MakeText("Title", canvas.transform, 48, TextAlignmentOptions.Center, FontStyles.Bold);
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(900, 70));
            title.text = "DnD Tactics";

            statusText = MakeText("Status", canvas.transform, 22, TextAlignmentOptions.Center);
            Anchor(statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -115), new Vector2(900, 30));
        }

        void BuildCreateBar()
        {
            // Name input
            var inGo = new GameObject("NameInput", typeof(RectTransform));
            inGo.transform.SetParent(canvas.transform, false);
            var inImg = inGo.AddComponent<Image>();
            inImg.color = new Color(1, 1, 1, 0.1f);
            nameInput = inGo.AddComponent<TMP_InputField>();
            Anchor(inGo.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(-120, -150), new Vector2(360, 44));

            var txt = MakeText("Text", inGo.transform, 22, TextAlignmentOptions.Left);
            var trt = txt.rectTransform; trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10, 6); trt.offsetMax = new Vector2(-10, -6);
            nameInput.textComponent = txt;
            nameInput.textViewport = trt;

            var ph = MakeText("Placeholder", inGo.transform, 22, TextAlignmentOptions.Left);
            ph.text = "Name your save…"; ph.color = new Color(1, 1, 1, 0.4f);
            var prt = ph.rectTransform; prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(10, 6); prt.offsetMax = new Vector2(-10, -6);
            nameInput.placeholder = ph;

            MakeButton("New Run", new Vector2(0.5f, 1f), new Vector2(150, -150), new Vector2(180, 44),
                       new Color(0.2f, 0.5f, 0.3f), CreateSlot);
        }

        void BuildSlotList()
        {
            var go = new GameObject("SlotList", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            listРarent = go.GetComponent<RectTransform>();
            Anchor(listРarent, new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(900, 600));
        }

        void MakeSlotRow(SaveManager.SlotInfo info, float y)
        {
            var row = new GameObject("SlotRow", typeof(RectTransform));
            row.transform.SetParent(canvas.transform, false);
            var img = row.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.08f);
            Anchor(row.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(900, 60));

            bool hasManual = SaveManager.HasManualSave(info.fileId);

            var label = MakeText("Label", row.transform, 24, TextAlignmentOptions.Left);
            label.text = info.displayName + (hasManual ? "   (checkpoint available)" : "");
            var lrt = label.rectTransform; lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 1);
            lrt.offsetMin = new Vector2(20, 0); lrt.offsetMax = new Vector2(-400, 0);

            // Buttons right-to-left: Delete, Load, then Load Manual (if a checkpoint exists).
            MakeButton("Delete", new Vector2(1f, 0.5f), new Vector2(-20, 0), new Vector2(110, 44),
                       new Color(0.6f, 0.25f, 0.25f), () => DeleteSlot(info.fileId), row.transform);
            MakeButton("Load", new Vector2(1f, 0.5f), new Vector2(-140, 0), new Vector2(110, 44),
                       new Color(0.2f, 0.4f, 0.6f), () => LoadSlot(info.fileId), row.transform);
            if (hasManual)
                MakeButton("Load Manual", new Vector2(1f, 0.5f), new Vector2(-275, 0), new Vector2(140, 44),
                           new Color(0.45f, 0.4f, 0.2f), () => LoadManualSlot(info.fileId), row.transform);

            rows.Add(row);
        }

        // ---------- small helpers ----------

        TMP_Text MakeText(string name, Transform parent, float size, TextAlignmentOptions align,
                          FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size; t.alignment = align; t.fontStyle = style;
            t.color = Color.white; t.raycastTarget = false;
            return t;
        }

        Button MakeButton(string text, Vector2 anchor, Vector2 pos, Vector2 size, Color color,
                          UnityEngine.Events.UnityAction onClick, Transform parent = null)
        {
            var go = new GameObject(text + "Button", typeof(RectTransform));
            go.transform.SetParent(parent != null ? parent : canvas.transform, false);
            var img = go.AddComponent<Image>(); img.color = color;
            var btn = go.AddComponent<Button>();
            Anchor(go.GetComponent<RectTransform>(), anchor, pos, size);

            var label = MakeText("Label", go.transform, 22, TextAlignmentOptions.Center);
            label.text = text;
            var rt = label.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            btn.onClick.AddListener(onClick);
            return btn;
        }

        void Anchor(RectTransform rt, Vector2 anchorPivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorPivot; rt.anchorMax = anchorPivot; rt.pivot = anchorPivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }
    }
}