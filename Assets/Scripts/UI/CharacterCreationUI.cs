using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DnDTactics.Rules;
using DnDTactics.Data;
using DnDTactics.Core;
using DnDTactics.Persistence;
using DnDTactics.Characters;

namespace DnDTactics.UI
{
    // Self-building character creation screen. Uses cycle selectors (robust in code),
    // pulls content from ContentDatabase, and reuses CharacterBuilder for all rules.
    public class CharacterCreationUI : MonoBehaviour
    {
        // A simple left/right value picker built from buttons + text.
        class Cycler
        {
            public List<string> options = new();
            public int index;
            public TMP_Text valueText;
            public System.Action onChange;
            public string Value => options.Count > 0 ? options[Mathf.Clamp(index, 0, options.Count - 1)] : "";
            public void Step(int d)
            {
                if (options.Count == 0) return;
                index = (index + d + options.Count) % options.Count;
                Render(); onChange?.Invoke();
            }
            public void SetOptions(List<string> o, int keep)
            {
                options = o;
                index = Mathf.Clamp(keep, 0, Mathf.Max(0, o.Count - 1));
                Render();
            }
            public void Render() { if (valueText) valueText.text = Value; }
        }

        Canvas canvas;
        TMP_InputField nameInput;
        Cycler speciesCyc, classCyc, bgCyc, methodCyc, levelCyc;
        readonly Cycler[] scoreCyc = new Cycler[6];
        readonly TMP_Text[] scoreResult = new TMP_Text[6];
        readonly Cycler[] bonusCyc = new Cycler[3];
        readonly TMP_Text[] bonusLabel = new TMP_Text[3];
        Button createButton, rollButton;
        TMP_Text outputText, statusText;

        List<Species> speciesList; List<CharacterClass> classList; List<Background> bgList;
        int[] rollPool;
        int[] scorePool;   // the six values to assign (rolls, or the fixed array)

        void Start()
        {
            var db = ContentDatabase.Instance;
            if (db == null) { Debug.LogError("No ContentDatabase found."); return; }
            speciesList = db.species; classList = db.classes; bgList = db.backgrounds;

            EnsureEventSystem();
            BuildCanvas();
            BuildHeader();
            BuildLeftColumn();
            BuildRightColumn();
            BuildBottom();

            methodCyc.SetOptions(new List<string> { "Standard Array", "Point Buy", "Roll 4d6" }, 0);
            ReconfigureScorePool();
            ReconfigureForBackground();
            Refresh();
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
            var go = new GameObject("Creation_Canvas");
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
            var t = MakeText("Title", 44, TextAlignmentOptions.Center, FontStyles.Bold);
            Anchor(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(900, 60));
            t.text = "Create a Character";
        }

        void BuildLeftColumn()
        {
            float x = -440, y = -120, step = -64, w = 380;

            // Name input
            nameInput = MakeInput(new Vector2(x, y), new Vector2(w, 44), "Name...");
            y += step;

            speciesCyc = MakeCycler(new Vector2(x, y), w, "Species",
                speciesList.Select(s => s ? s.speciesName : "—").ToList(), Refresh);
            y += step;
            classCyc = MakeCycler(new Vector2(x, y), w, "Class",
                classList.Select(c => c ? c.className : "—").ToList(), Refresh);
            y += step;
            bgCyc = MakeCycler(new Vector2(x, y), w, "Backgrnd",
                bgList.Select(b => b ? b.backgroundName : "—").ToList(),
                () => { ReconfigureForBackground(); Refresh(); });
            y += step;
            levelCyc = MakeCycler(new Vector2(x, y), w, "Level",
                Enumerable.Range(1, 20).Select(n => n.ToString()).ToList(), Refresh);
        }

        void BuildRightColumn()
        {
            float x = 280, y = -110, step = -52, w = 300;

            var h = MakeText("ScoresHdr", 26, TextAlignmentOptions.Left, FontStyles.Bold);
            Anchor(h.rectTransform, new Vector2(0.5f, 1f), new Vector2(x - 70, y), new Vector2(300, 32));
            h.text = "Ability Scores";
            y += step;

            methodCyc = MakeCycler(new Vector2(x, y), w, "Method", new List<string> { "—" },
                () => { ReconfigureScorePool(); Refresh(); });
            y += step;

            rollButton = MakeButton("Roll", new Vector2(x - 90, y), new Vector2(120, 40),
                new Color(0.4f, 0.35f, 0.2f), RollScores);
            y += step;

            string[] abbr = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
            for (int i = 0; i < 6; i++)
            {
                scoreCyc[i] = MakeCycler(new Vector2(x - 30, y), 240, abbr[i],
                    new List<string> { "—" }, OnScoreCyclerChanged);
                scoreResult[i] = MakeText("Res" + i, 20, TextAlignmentOptions.Left);
                Anchor(scoreResult[i].rectTransform, new Vector2(0.5f, 1f),
                    new Vector2(x + 150, y), new Vector2(120, 32));
                y += step;
            }

            var bh = MakeText("BonusHdr", 24, TextAlignmentOptions.Left, FontStyles.Bold);
            Anchor(bh.rectTransform, new Vector2(0.5f, 1f), new Vector2(x - 70, y), new Vector2(300, 32));
            bh.text = "Background Bonus (+2/+1 or +1/+1/+1)";
            y += step;

            for (int i = 0; i < 3; i++)
            {
                bonusCyc[i] = MakeCycler(new Vector2(x, y), w, "—",
                    new List<string> { "+0", "+1", "+2" }, Refresh);
                bonusLabel[i] = bonusCyc[i].valueText; // not used; label handled below
                y += step;
            }
        }

        void OnScoreCyclerChanged()
        {
            // Only the pool-based modes need cross-cycler rebuilding.
            if (scorePool != null) RebuildPoolOptions();
            Refresh();
        }

        void BuildBottom()
        {
            createButton = MakeButton("Create", new Vector2(-440, -560), new Vector2(180, 48),
                new Color(0.2f, 0.5f, 0.3f), OnCreate);
            MakeButton("Back to Menu", new Vector2(-230, -560), new Vector2(200, 48),
                new Color(0.4f, 0.4f, 0.45f), () => SceneFlow.Go(SceneFlow.MainMenu));

            outputText = MakeText("Output", 20, TextAlignmentOptions.TopLeft);
            Anchor(outputText.rectTransform, new Vector2(0.5f, 1f), new Vector2(-330, -620), new Vector2(620, 160));

            statusText = MakeText("Status", 20, TextAlignmentOptions.Center);
            Anchor(statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(900, 30));
        }

        // ---------- reconfigure ----------

        Background CurrentBg() =>
            bgList.Count > 0 ? bgList[Mathf.Clamp(bgCyc.index, 0, bgList.Count - 1)] : null;

        void ReconfigureScorePool()
        {
            int m = methodCyc.index; // 0=array 1=pointbuy 2=roll
            rollButton.gameObject.SetActive(m == 2);

            if (m == 1) // Point Buy: independent 8..15 per ability, no shared pool
            {
                scorePool = null;
                var opts = Enumerable.Range(8, 8).Select(v => v.ToString()).ToList(); // 8..15
                for (int i = 0; i < 6; i++) scoreCyc[i].SetOptions(new List<string>(opts), 0);
                Refresh();
                return;
            }

            // Array or Roll: build the six-value pool once.
            if (m == 0)
                scorePool = AbilityScoreGeneration.StandardArray();
            else // roll
            {
                if (scorePool == null) scorePool = AbilityScoreGeneration.RollSet();
            }

            // Clear all assignments and rebuild each cycler's available options.
            for (int i = 0; i < 6; i++) scoreCyc[i].SetOptions(new List<string> { "—" }, 0);
            RebuildPoolOptions();
            Refresh();
        }

        // Rebuilds every ability cycler to offer "—" plus the pool values not used elsewhere.
        void RebuildPoolOptions()
        {
            if (scorePool == null) return; // point buy doesn't use the pool

            // What each ability currently holds (its selected value, or null for "—").
            var assigned = new string[6];
            for (int i = 0; i < 6; i++)
                assigned[i] = scoreCyc[i].Value == "—" ? null : scoreCyc[i].Value;

            // Count how many of each pool value are "taken" by assignments.
            for (int i = 0; i < 6; i++)
            {
                // Build this ability's option list: "—", its own current value, plus any
                // pool value still available (accounting for duplicates in the pool).
                var poolList = scorePool.Select(v => v.ToString()).ToList();

                // Remove values used by OTHER abilities (one instance each).
                for (int j = 0; j < 6; j++)
                {
                    if (j == i) continue;
                    if (assigned[j] != null) poolList.Remove(assigned[j]);
                }

                var opts = new List<string> { "—" };
                opts.AddRange(poolList);

                // Keep this cycler on its current value if still valid.
                string keep = assigned[i] ?? "—";
                int keepIdx = opts.IndexOf(keep);
                scoreCyc[i].SetOptions(opts, keepIdx < 0 ? 0 : keepIdx);
            }
        }

        void RollScores()
        {
            scorePool = AbilityScoreGeneration.RollSet(); // fresh six values
            for (int i = 0; i < 6; i++) scoreCyc[i].SetOptions(new List<string> { "—" }, 0);
            RebuildPoolOptions();
            Refresh();
        }

        void ReconfigureForBackground()
        {
            var bg = CurrentBg();
            for (int i = 0; i < 3; i++)
            {
                bool has = bg != null && i < bg.abilityOptions.Count;
                // bonusCyc[i] sits in a row whose label we set via its container's first child:
                SetCyclerLabel(bonusCyc[i], has ? bg.abilityOptions[i].ToString() : "—");
                bonusCyc[i].SetOptions(new List<string> { "+0", "+1", "+2" }, 0);
                bonusCyc[i].valueText.transform.parent.gameObject.SetActive(has);
            }
        }

        // ---------- read + validate ----------

        CharacterBuilder ReadBuilder()
        {
            var b = new CharacterBuilder
            {
                characterName = nameInput.text,
                startingLevel = int.TryParse(levelCyc.Value, out int lv) ? lv : 1
            };
            if (speciesList.Count > 0) b.species = speciesList[speciesCyc.index];
            if (classList.Count > 0) b.characterClass = classList[classCyc.index];
            var bg = CurrentBg(); b.background = bg;

            b.method = (ScoreMethod)methodCyc.index;
            for (int i = 0; i < 6; i++)
                b.baseScores[i] = int.TryParse(scoreCyc[i].Value, out int v) ? v : 0; // "—" → 0 (invalid)

            if (bg != null)
                for (int i = 0; i < 3 && i < bg.abilityOptions.Count; i++)
                    b.backgroundBonuses[(int)bg.abilityOptions[i]] = bonusCyc[i].index;
            return b;
        }

        void Refresh()
        {
            var bg = CurrentBg();
            Ability[] all = (Ability[])System.Enum.GetValues(typeof(Ability));
            for (int i = 0; i < 6; i++)
            {
                bool assigned = int.TryParse(scoreCyc[i].Value, out int v);
                int baseV = assigned ? v : 0;
                int bonus = BonusFor(all[i], bg);
                int final = baseV + bonus;
                int mod = AbilityScores.Modifier(final);
                scoreResult[i].text = assigned ? $"= {final} ({(mod >= 0 ? "+" : "")}{mod})" : "—";
            }

            if (scorePool != null)
            {
                var assignedVals = new List<string>();
                for (int i = 0; i < 6; i++) if (scoreCyc[i].Value != "—") assignedVals.Add(scoreCyc[i].Value);
                var remaining = scorePool.Select(v => v.ToString()).ToList();
                foreach (var a in assignedVals) remaining.Remove(a);
                string poolMsg = remaining.Count > 0
                    ? "Unassigned values: " + string.Join(", ", remaining)
                    : "All values assigned.";
                // Append to whatever status/output line you use:
                outputText.text = (createButton.interactable ? "Ready to create." : outputText.text)
                                  + "\n" + poolMsg;
            }

            var problems = ReadBuilder().Validate();
            createButton.interactable = problems.Count == 0;
            outputText.text = problems.Count == 0
                ? "Ready to create."
                : "Fix:\n- " + string.Join("\n- ", problems);
        }

        int BonusFor(Ability a, Background bg)
        {
            if (bg == null) return 0;
            for (int i = 0; i < 3 && i < bg.abilityOptions.Count; i++)
                if (bg.abilityOptions[i] == a) return bonusCyc[i].index;
            return 0;
        }

        void OnCreate()
        {
            var b = ReadBuilder();
            if (!b.IsComplete()) { Refresh(); return; }
            Character c = b.Build();

            var session = GameSession.Instance;
            if (session != null && session.ActiveSlot != null)
            {
                var member = session.ActiveSlot.barracks.Add(c);
                member.inventory.Add("PortalScroll", 1);   // every new hero starts with one
                member.inventory.Add("Torch", 1);          // a torch to manage light
                session.SaveActive();
                int n = session.ActiveSlot.barracks.members.Count;
                statusText.text = $"Added {c.characterName}!  Barracks now holds {n}. " +
                                  "Create another, or Back to Menu.";
                nameInput.text = ""; // reset for the next hero
                Refresh();
            }
            else
            {
                statusText.text = "No active save slot — start from the Main Menu to save.";
            }
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

        Button MakeButton(string text, Vector2 pos, Vector2 size, Color color, System.Action onClick)
        {
            var go = new GameObject(text + "Btn", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            go.AddComponent<Image>().color = color;
            var btn = go.AddComponent<Button>();
            Anchor(go.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), pos, size);
            var label = MakeTextChild(go.transform, text, 22, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            btn.onClick.AddListener(() => onClick());
            return btn;
        }

        TMP_InputField MakeInput(Vector2 pos, Vector2 size, string placeholder)
        {
            var go = new GameObject("NameInput", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            go.AddComponent<Image>().color = new Color(1, 1, 1, 0.12f);
            var input = go.AddComponent<TMP_InputField>();
            Anchor(go.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), pos, size);

            var txt = MakeTextChild(go.transform, "", 22, TextAlignmentOptions.Left);
            Pad(txt.rectTransform, 10, 6);
            var ph = MakeTextChild(go.transform, placeholder, 22, TextAlignmentOptions.Left);
            ph.color = new Color(1, 1, 1, 0.4f); Pad(ph.rectTransform, 10, 6);

            input.textComponent = txt; input.placeholder = ph;
            input.textViewport = go.GetComponent<RectTransform>();
            input.onValueChanged.AddListener(_ => Refresh());
            return input;
        }

        Cycler MakeCycler(Vector2 pos, float width, string label, List<string> options, System.Action onChange)
        {
            var container = new GameObject("Cyc_" + label, typeof(RectTransform));
            container.transform.SetParent(canvas.transform, false);
            Anchor(container.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), pos, new Vector2(width, 38));
            float half = width / 2f;

            var lbl = MakeTextChild(container.transform, label, 20, TextAlignmentOptions.Left);
            Place(lbl.rectTransform, -half + 37, 66);
            lbl.name = "RowLabel";

            var prev = MakeArrow(container.transform, "<", -half + 89);
            var valGo = MakeTextChild(container.transform, "", 22, TextAlignmentOptions.Center);
            Place(valGo.rectTransform, 35, width - 138);
            var next = MakeArrow(container.transform, ">", half - 19);

            var cyc = new Cycler { valueText = valGo, onChange = onChange };
            prev.onClick.AddListener(() => cyc.Step(-1));
            next.onClick.AddListener(() => cyc.Step(1));
            cyc.SetOptions(options, 0);
            return cyc;
        }

        void SetCyclerLabel(Cycler c, string text)
        {
            var lbl = c.valueText.transform.parent.Find("RowLabel");
            if (lbl != null) lbl.GetComponent<TMP_Text>().text = text;
        }

        Button MakeArrow(Transform parent, string glyph, float x)
        {
            var go = new GameObject("Arrow", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.25f, 0.3f, 0.4f);
            var btn = go.AddComponent<Button>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0); rt.sizeDelta = new Vector2(30, 32);
            var label = MakeTextChild(go.transform, glyph, 24, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            return btn;
        }

        TMP_Text MakeTextChild(Transform parent, string text, float size, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.alignment = align;
            t.color = Color.white; t.raycastTarget = false;
            return t;
        }

        // anchored helpers (children center-anchored within their container)
        void Place(RectTransform rt, float x, float w)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0); rt.sizeDelta = new Vector2(w, 32);
        }
        void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        void Pad(RectTransform rt, float px, float py) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(px, py); rt.offsetMax = new Vector2(-px, -py); }
        void Anchor(RectTransform rt, Vector2 a, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = a; rt.anchorMax = a; rt.pivot = a; rt.anchoredPosition = pos; rt.sizeDelta = size;
        }
    }
}