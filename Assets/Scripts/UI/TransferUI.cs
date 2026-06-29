using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DnDTactics.Core;
using DnDTactics.Data;
using DnDTactics.Characters;
using DnDTactics.Persistence;

namespace DnDTactics.UI
{
    // Dedicated transfer screen: deployed party side by side (incl. Down/Dead), move gold
    // via slider + quick buttons, push items one click each. Self-building in code.
    public class TransferUI : MonoBehaviour
    {
        Canvas canvas;
        TMP_Text headerText;
        Slider goldSlider;
        TMP_Text goldAmountText, centerHeader;
        GameObject centerPanel;
        readonly List<GameObject> dynamic = new(); // cards + center content, rebuilt each refresh

        SaveSlot Slot => GameSession.Instance != null ? GameSession.Instance.ActiveSlot : null;
        string sourceId, destId;

        List<BarracksMember> PartyMembers() =>
            Slot == null ? new List<BarracksMember>()
                         : Slot.party.Members(Slot.barracks).ToList();

        void Start()
        {
            EnsureEventSystem();
            BuildCanvas();
            BuildStatics();
            Refresh();
        }

        // ---------- refresh ----------

        void Refresh()
        {
            foreach (var g in dynamic) Destroy(g);
            dynamic.Clear();

            if (Slot == null) { headerText.text = "No active slot."; return; }
            headerText.text = $"{Slot.displayName} — Transfer  (Leader pays revival costs)";

            var members = PartyMembers();
            // Lay member cards across the top.
            float cardW = 300f, gap = 30f;
            float startX = -((members.Count * cardW + (members.Count - 1) * gap) / 2f) + cardW / 2f;
            for (int i = 0; i < members.Count; i++)
                MakeCard(members[i], startX + i * (cardW + gap));

            BuildCenter();
        }

        BarracksMember Get(string id) => id == null ? null : Slot.barracks.GetById(id);

        // ---------- member cards ----------

        void MakeCard(BarracksMember m, float x)
        {
            var card = new GameObject("Card", typeof(RectTransform));
            card.transform.SetParent(canvas.transform, false);
            var img = card.AddComponent<Image>();
            bool isSource = m.id == sourceId, isDest = m.id == destId;
            img.color = isSource ? new Color(0.2f, 0.45f, 0.3f, 0.9f)
                      : isDest ? new Color(0.2f, 0.35f, 0.55f, 0.9f)
                      : new Color(1, 1, 1, 0.08f);
            Anchor(card.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(x, -150), new Vector2(290, 420));

            // Clicking the card cycles its role: source → dest → clear.
            var btn = card.AddComponent<Button>();
            btn.onClick.AddListener(() => OnCardClicked(m.id));

            bool leader = Slot.party.leaderId == m.id;
            string statusTag = m.status == MemberStatus.Down ? "  [DOWN]"
                             : m.status == MemberStatus.Dead ? "  [DEAD]" : "";
            var title = MakeChildText(card.transform, (leader ? "* " : "") + m.character.characterName + statusTag,
                                      22, TextAlignmentOptions.Center, FontStyles.Bold);
            Place(title.rectTransform, 0, 185, 270, 30);

            var info = MakeChildText(card.transform,
                $"L{m.character.level} {(m.character.characterClass ? m.character.characterClass.className : "")}\n" +
                $"<b>Gold: {m.gold}</b>", 18, TextAlignmentOptions.Center);
            Place(info.rectTransform, 0, 140, 270, 50);

            // Item list (each row pushes one to the destination).
            float iy = 95;
            foreach (var (id, qty) in m.inventory.Entries())
            {
                var def = ContentDatabase.Instance.GetItem(id);
                string nm = def ? def.itemName : id;
                var row = MakeChildText(card.transform, $"{nm} x{qty}", 16, TextAlignmentOptions.Left);
                Place(row.rectTransform, -20, iy, 220, 26);

                // Push button only shown when THIS card is the source and a dest is chosen.
                if (m.id == sourceId && destId != null && destId != sourceId)
                {
                    var push = MakeChildButton(card.transform, "→", new Color(0.3f, 0.45f, 0.6f),
                        () => { Get(sourceId).inventory.TransferTo(Get(destId).inventory, id, 1); Save(); Refresh(); });
                    Place(push.GetComponent<RectTransform>(), 120, iy, 40, 26);
                }
                iy -= 30;
            }

            // Set-leader button.
            var lead = MakeChildButton(card.transform, leader ? "Leader" : "Make Leader",
                leader ? new Color(0.5f, 0.42f, 0.2f) : new Color(0.3f, 0.3f, 0.35f),
                () => { Slot.party.SetLeader(m.id); Save(); Refresh(); });
            Place(lead.GetComponent<RectTransform>(), 0, -180, 200, 36);

            dynamic.Add(card);
        }

        void OnCardClicked(string id)
        {
            if (sourceId == null) sourceId = id;
            else if (sourceId == id) sourceId = null;            // click source again = clear
            else if (destId == id) destId = null;               // click dest again = clear
            else destId = id;                                   // pick destination
            Refresh();
        }

        // ---------- center transfer panel ----------

        void BuildCenter()
        {
            var src = Get(sourceId); var dst = Get(destId);
            bool ready = src != null && dst != null && src != dst;

            centerHeader.text = ready
                ? $"Transfer  {src.character.characterName}  →  {dst.character.characterName}"
                : (sourceId == null ? "Click a member to choose the SOURCE."
                                    : "Click another member to choose the DESTINATION.");

            // Gold slider only meaningful when ready and source has gold.
            int max = ready ? src.gold : 0;
            goldSlider.gameObject.SetActive(ready);
            goldAmountText.gameObject.SetActive(ready);
            if (ready)
            {
                goldSlider.minValue = 0; goldSlider.maxValue = Mathf.Max(0, max);
                goldSlider.value = Mathf.Min(goldSlider.value, max);
                UpdateGoldLabel();
            }
        }

        void UpdateGoldLabel()
        {
            var src = Get(sourceId);
            int amt = (int)goldSlider.value;
            goldAmountText.text = src != null ? $"Send {amt} gold  (of {src.gold})" : "";
        }

        void SendGold()
        {
            var src = Get(sourceId); var dst = Get(destId);
            if (src == null || dst == null || src == dst) return;
            int amt = Mathf.Min((int)goldSlider.value, src.gold);
            if (amt <= 0) return;
            src.gold -= amt; dst.gold += amt;
            Save(); Refresh();
        }

        void QuickGold(float frac) // 0, 0.5, 1
        {
            var src = Get(sourceId);
            if (src == null) return;
            goldSlider.value = Mathf.Round(src.gold * frac);
            UpdateGoldLabel();
        }

        // ---------- static UI ----------

        void BuildStatics()
        {
            var title = MakeText("Title", 38, TextAlignmentOptions.Center, FontStyles.Bold);
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -36), new Vector2(1400, 50));
            title.text = "Party Transfer";

            headerText = MakeText("Header", 22, TextAlignmentOptions.Center);
            Anchor(headerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -88), new Vector2(1400, 30));

            // Center panel (gold controls live here).
            centerPanel = new GameObject("Center", typeof(RectTransform));
            centerPanel.transform.SetParent(canvas.transform, false);
            Anchor(centerPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0, 250), new Vector2(700, 220));

            centerHeader = MakeChildText(centerPanel.transform, "", 22, TextAlignmentOptions.Center);
            Place(centerHeader.rectTransform, 0, 80, 680, 30);

            goldSlider = MakeSlider(centerPanel.transform, new Vector2(0, 30), new Vector2(440, 24));
            goldSlider.onValueChanged.AddListener(_ => UpdateGoldLabel());

            goldAmountText = MakeChildText(centerPanel.transform, "", 20, TextAlignmentOptions.Center);
            Place(goldAmountText.rectTransform, 0, 0, 680, 28);

            MakeChildButton(centerPanel.transform, "0", new Color(0.3f, 0.3f, 0.35f), () => QuickGold(0f))
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, -40);
            MakeChildButton(centerPanel.transform, "Half", new Color(0.3f, 0.3f, 0.35f), () => QuickGold(0.5f))
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(-70, -40);
            MakeChildButton(centerPanel.transform, "All", new Color(0.3f, 0.3f, 0.35f), () => QuickGold(1f))
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(10, -40);
            var send = MakeChildButton(centerPanel.transform, "Send Gold", new Color(0.2f, 0.5f, 0.3f), SendGold);
            var sr = send.GetComponent<RectTransform>(); sr.anchoredPosition = new Vector2(120, -40); sr.sizeDelta = new Vector2(140, 36);

            // Position the three quick buttons' sizes.
            foreach (var t in new[] { "0Btn", "HalfBtn", "AllBtn" })
            { var go = centerPanel.transform.Find(t); if (go) go.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 36); }

            MakeButton("Back to Roster", new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(220, 50),
                new Color(0.4f, 0.4f, 0.45f), () => SceneFlow.Go(SceneFlow.Roster));
        }

        void Save() { if (GameSession.Instance != null) GameSession.Instance.SaveActive(); }

        // ---------- primitives ----------

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
            var go = new GameObject("Transfer_Canvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
        }

        TMP_Text MakeText(string name, float size, TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size; t.alignment = align; t.fontStyle = style; t.color = Color.white; t.raycastTarget = false;
            return t;
        }

        TMP_Text MakeChildText(Transform parent, string text, float size, TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.alignment = align; t.fontStyle = style;
            t.color = Color.white; t.raycastTarget = false; t.richText = true;
            return t;
        }

        Button MakeButton(string text, Vector2 anchor, Vector2 pos, Vector2 size, Color color, System.Action onClick)
        {
            var go = new GameObject(text + "Btn", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            go.AddComponent<Image>().color = color;
            var btn = go.AddComponent<Button>();
            Anchor(go.GetComponent<RectTransform>(), anchor, pos, size);
            var label = MakeChildText(go.transform, text, 20, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            btn.onClick.AddListener(() => onClick());
            return btn;
        }

        GameObject MakeChildButton(Transform parent, string text, Color color, System.Action onClick)
        {
            var go = new GameObject(text + "Btn", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = color;
            var btn = go.AddComponent<Button>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(70, 30);
            var label = MakeChildText(go.transform, text, 18, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            btn.onClick.AddListener(() => onClick());
            return go;
        }

        Slider MakeSlider(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("GoldSlider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var slider = go.AddComponent<Slider>();

            var bg = new GameObject("BG", typeof(RectTransform)); bg.transform.SetParent(go.transform, false);
            bg.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f); Stretch(bg.GetComponent<RectTransform>());

            var fillArea = new GameObject("FillArea", typeof(RectTransform)); fillArea.transform.SetParent(go.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>());
            var fill = new GameObject("Fill", typeof(RectTransform)); fill.transform.SetParent(fillArea.transform, false);
            fill.AddComponent<Image>().color = new Color(0.85f, 0.7f, 0.2f);
            var fr = fill.GetComponent<RectTransform>(); fr.anchorMin = new Vector2(0, 0); fr.anchorMax = new Vector2(0, 1); fr.sizeDelta = new Vector2(10, 0);

            var handle = new GameObject("Handle", typeof(RectTransform)); handle.transform.SetParent(go.transform, false);
            handle.AddComponent<Image>().color = Color.white;
            var hr = handle.GetComponent<RectTransform>(); hr.sizeDelta = new Vector2(18, 28);

            slider.fillRect = fr; slider.handleRect = hr; slider.wholeNumbers = true;
            slider.targetGraphic = handle.GetComponent<Image>();
            return slider;
        }

        // anchored helpers
        void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
        }
        void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        void Anchor(RectTransform rt, Vector2 a, Vector2 pos, Vector2 size)
        { rt.anchorMin = a; rt.anchorMax = a; rt.pivot = a; rt.anchoredPosition = pos; rt.sizeDelta = size; }
    }
}