using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DnDTactics.Core;
using DnDTactics.Persistence;
using DnDTactics.Characters;

namespace DnDTactics.UI
{
    // Self-building roster/hub: view barracks, deploy a party (1–4 living), dismiss
    // members, and launch a run. Reads the active slot from GameSession.
    public class RosterUI : MonoBehaviour
    {
        Canvas canvas;
        TMP_Text headerText, partyText;
        Button launchButton;
        readonly List<GameObject> rows = new();

        SaveSlot Slot => GameSession.Instance != null ? GameSession.Instance.ActiveSlot : null;

        void Start()
        {
            EnsureEventSystem();
            BuildCanvas();
            BuildStatics();

            if (Slot == null)
            {
                headerText.text = "No active save slot. Return to the Main Menu.";
                Refresh();
                return;
            }
            Refresh();
        }

        void Refresh()
        {
            foreach (var r in rows) Destroy(r);
            rows.Clear();

            if (Slot == null) { if (launchButton) launchButton.interactable = false; return; }

            var barracks = Slot.barracks;
            var party = Slot.party;

            headerText.text = $"{Slot.displayName} — Barracks ({barracks.members.Count})";

            float y = -150f;
            foreach (var m in barracks.members)
            {
                MakeMemberRow(m, y);
                y -= 64f;
            }

            int deployed = party.Size;
            int living = party.LivingMembers(barracks).Count();
            partyText.text = $"Party: {deployed}/{Party.MaxSize} deployed  ({living} living)";

            // Launch requires 1–4 living deployed members.
            launchButton.interactable = living >= 1 && living <= Party.MaxSize;
        }

        // ---------- actions ----------

        void Deploy(BarracksMember m)
        {
            if (Slot.party.Deploy(Slot.barracks, m.id)) { GameSession.Instance.SaveActive(); Refresh(); }
        }

        void Recall(BarracksMember m)
        {
            if (Slot.party.Recall(Slot.barracks, m.id)) { GameSession.Instance.SaveActive(); Refresh(); }
        }

        void Dismiss(BarracksMember m)
        {
            // Permanent: pull from party if deployed, then remove from barracks.
            Slot.party.Recall(Slot.barracks, m.id);
            Slot.barracks.Remove(m.id);
            GameSession.Instance.SaveActive();
            Refresh();
        }

        void ReviveMember(BarracksMember m)
        {
            var leaderId = Slot.party.EnsureLeader(Slot.barracks);
            var payer = leaderId != null ? Slot.barracks.GetById(leaderId) : null;
            var result = DnDTactics.Characters.RevivalService.TownRevive(m, payer);
            Debug.Log(result.message);

            if (result.success)
            {
                // If they're still listed in the party, return them straight to Deployed
                // (they never left the party when they fell). Otherwise leave Available.
                if (Slot.party.memberIds.Contains(m.id))
                    m.status = DnDTactics.Characters.MemberStatus.Deployed;
                GameSession.Instance.SaveActive();
            }
            Refresh();
        }

        void TakeLongRest()
        {
            var result = DnDTactics.Characters.RestService.LongRest(Slot);
            Debug.Log(result.message);
            GameSession.Instance.SaveActive();
            Refresh();
        }

        void Launch()
        {
            GameSession.Instance.SaveActive();
            SceneFlow.Go(SceneFlow.Encounter);
        }

        // ---------- building ----------

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
            var go = new GameObject("Roster_Canvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
        }

        void BuildStatics()
        {
            var title = MakeText("Title", 40, TextAlignmentOptions.Center, FontStyles.Bold);
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(1000, 56));
            title.text = "Roster";

            headerText = MakeText("Header", 24, TextAlignmentOptions.Center);
            Anchor(headerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -95), new Vector2(1000, 32));

            partyText = MakeText("Party", 22, TextAlignmentOptions.Center);
            Anchor(partyText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(1000, 30));

            launchButton = MakeButton("Launch Run", new Vector2(0.5f, 0f), new Vector2(-120, 40),
                new Vector2(200, 52), new Color(0.2f, 0.5f, 0.3f), Launch);
            MakeButton("Add Character", new Vector2(0.5f, 0f), new Vector2(110, 40),
                new Vector2(200, 52), new Color(0.25f, 0.4f, 0.55f),
                () => SceneFlow.Go(SceneFlow.CharacterCreation));
            MakeButton("Main Menu", new Vector2(0.5f, 0f), new Vector2(330, 40),
                new Vector2(180, 52), new Color(0.4f, 0.4f, 0.45f),
                () => SceneFlow.Go(SceneFlow.MainMenu));
            MakeButton("Transfer", new Vector2(0.5f, 0f), new Vector2(-330, 40),
                new Vector2(180, 52), new Color(0.35f, 0.3f, 0.5f),
                () => SceneFlow.Go(SceneFlow.Transfer));
            MakeButton("Long Rest", new Vector2(0.5f, 0f), new Vector2(-540, 40),
                new Vector2(170, 52), new Color(0.3f, 0.45f, 0.55f), TakeLongRest);
        }

        void MakeMemberRow(BarracksMember m, float y)
        {
            var row = new GameObject("Row", typeof(RectTransform));
            row.transform.SetParent(canvas.transform, false);
            row.AddComponent<Image>().color = new Color(1, 1, 1, 0.07f);
            Anchor(row.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(960, 56));

            var c = m.character;
            string info = $"{c.characterName}  —  L{c.level} " +
                          $"{(c.species ? c.species.speciesName : "?")} " +
                          $"{(c.characterClass ? c.characterClass.className : "?")}   " +
                          $"[{m.status}]";
            var label = MakeChildText(row.transform, info, 22, TextAlignmentOptions.Left);
            var lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 1);
            lrt.offsetMin = new Vector2(20, 0); lrt.offsetMax = new Vector2(-360, 0);

            // Context buttons depend on status.
            if (m.status == MemberStatus.Available)
                MakeRowButton(row.transform, "Deploy", -250, new Color(0.2f, 0.5f, 0.3f), () => Deploy(m));
            else if (m.status == MemberStatus.Deployed)
                MakeRowButton(row.transform, "Recall", -250, new Color(0.45f, 0.4f, 0.2f), () => Recall(m));
            else if (m.status == MemberStatus.Down)
            {
                int cost = DnDTactics.Rules.Revival.TownHealerCost(m.character.level);
                MakeRowButton(row.transform, $"Revive {cost}g", -250, new Color(0.3f, 0.5f, 0.45f),
                    () => ReviveMember(m));
            }
            if (m.status == MemberStatus.Down)
            {
                int left = DnDTactics.Rules.RevivalTiming.RevivableWindowLongRests -
                           (Slot.party.longRestsTaken - m.fellAtLongRest);
                info += $"   ⚠ {Mathf.Max(0, left)} long rests to revive";
            }

            // Dead: no deploy/recall/revive (revival window passed).
            MakeRowButton(row.transform, "Dismiss", -120, new Color(0.55f, 0.25f, 0.25f), () => Dismiss(m));
            rows.Add(row);
        }

        // ---------- primitives ----------

        TMP_Text MakeText(string name, float size, TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size; t.alignment = align; t.fontStyle = style;
            t.color = Color.white; t.raycastTarget = false;
            return t;
        }

        TMP_Text MakeChildText(Transform parent, string text, float size, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.alignment = align;
            t.color = Color.white; t.raycastTarget = false;
            return t;
        }

        Button MakeButton(string text, Vector2 anchor, Vector2 pos, Vector2 size, Color color, System.Action onClick)
        {
            var go = new GameObject(text + "Btn", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            go.AddComponent<Image>().color = color;
            var btn = go.AddComponent<Button>();
            Anchor(go.GetComponent<RectTransform>(), anchor, pos, size);
            var label = MakeChildText(go.transform, text, 22, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            btn.onClick.AddListener(() => onClick());
            return btn;
        }

        void MakeRowButton(Transform parent, string text, float xFromRight, Color color, System.Action onClick)
        {
            var go = new GameObject(text + "Btn", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = color;
            var btn = go.AddComponent<Button>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f); rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(xFromRight, 0); rt.sizeDelta = new Vector2(120, 42);
            var label = MakeChildText(go.transform, text, 20, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            btn.onClick.AddListener(() => onClick());
        }

        void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        void Anchor(RectTransform rt, Vector2 a, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = a; rt.anchorMax = a; rt.pivot = a; rt.anchoredPosition = pos; rt.sizeDelta = size;
        }
    }
}