// UN-HUMANITY — builds the investigation layer: clue nodes on their
// blueprint marks, the CaseController, the HUD (sight meter, prompt,
// toast), and the TAB case-log dossier panel. Re-runnable.
//   unity command eval "return CaseSetup.Build();"

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnHumanity.Case;

public static class CaseSetup
{
    const string kScene = "Assets/Scenes/SC_02_StreetBlock.unity";

    static readonly Color Void_ = new Color32(0x0B, 0x0C, 0x0F, 0xF2);
    static readonly Color VoidSolid = new Color32(0x0B, 0x0C, 0x0F, 0xFF);  // pixel HUD: fully opaque
    static readonly Color Concrete = new Color32(0x1C, 0x1F, 0x25, 0xFF);
    static readonly Color Steel = new Color32(0x3A, 0x3F, 0x48, 0xFF);
    static readonly Color Fog = new Color32(0x97, 0x9D, 0xA8, 0xFF);
    static readonly Color Paper = new Color32(0xE7, 0xE9, 0xEC, 0xFF);
    static readonly Color Anomaly = new Color32(0xE4, 0x56, 0x8A, 0xFF);
    static Font UiFont
    {
        get
        {
            // BoldPixels (16 px native) — the pixel identity; sizes snap to
            // multiples of 16 in Label() so glyphs stay on the pixel grid
            var px = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/UI/Fonts/BoldPixels.otf");
            return px != null ? px : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects()
                 .Where(g => g.name == "CASE_Nodes" || g.name == "UI_Case" || g.name == "CaseController"))
            Object.DestroyImmediate(g);

        // ── marker materials (unlit, palette-locked) ──
        var fogMat = MarkerMat("Assets/Art/Evidence/M_Marker_Fog.mat", new Color32(0xE7, 0xE9, 0xEC, 0xFF)); // paper - reads at diorama distance
        var roseMat = MarkerMat("Assets/Art/Evidence/M_Marker_Rose.mat", new Color32(0xFB, 0x8F, 0xBC, 0xFF)); // lightened rose - must read against the anomaly pool

        // ── clue nodes on their street marks (blueprint §1), spread so no
        //    two prompts fight; each carries a floating diamond marker ──
        var nodesRoot = new GameObject("CASE_Nodes").transform;
        var madeNodes = new List<ClueNode>();
        void Node(ClueId id, string label, Vector3 pos, float radius = 1.8f, bool talks = false)
        {
            var go = new GameObject($"Clue_{label}");
            go.transform.SetParent(nodesRoot, false);
            go.transform.position = pos;
            var n = go.AddComponent<ClueNode>();
            n.clueId = id;
            n.radius = radius;
            n.talks = talks;

            var markGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            markGo.name = "Marker";
            Object.DestroyImmediate(markGo.GetComponent<MeshCollider>());
            markGo.transform.SetParent(go.transform, false);
            markGo.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            markGo.transform.localScale = new Vector3(0.34f, 0.34f, 1f);
            var mr = markGo.GetComponent<MeshRenderer>();
            mr.sharedMaterial = fogMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var marker = markGo.AddComponent<ClueMarker>();
            marker.node = n;
            marker.quad = mr;
            marker.fogMat = fogMat;
            marker.roseMat = roseMat;
            madeNodes.Add(n);
        }
        // Spread down the whole block, every node OUTSIDE the anomaly
        // field (r=5.5 around Z 41.5) — investigating must never
        // force-start THE QUEUE. The stop evidence is read from vantage
        // points at the field's edge.
        // (the stop now manifests mid-road at the far end, ~Z 46.4; the
        // inner field is r=3.0 — all vantage nodes stay well outside it)
        Node(ClueId.Witnesses, "06_WitnessA", new Vector3(-6.4f, 0.5f, 14f), 2.4f, talks: true);
        Node(ClueId.Witnesses, "09_WitnessB", new Vector3(6.3f, 0.5f, 24f), 2.4f, talks: true);
        Node(ClueId.Archive, "16_Archive", new Vector3(-6.8f, 0.5f, 28f), 2.0f);
        Node(ClueId.Sediment, "15_Sediment", new Vector3(-7.8f, 0.5f, 32f), 1.8f);    // strata drift down the block
        Node(ClueId.TheStop, "01_TheStop", new Vector3(5.8f, 0.5f, 37f), 2.0f);       // vantage from the north sidewalk
        Node(ClueId.Bench, "08_Bench", new Vector3(-4.6f, 0.5f, 41f), 1.8f);          // kerb edge, looking up the road
        Node(ClueId.Victim, "04_Victim", new Vector3(3.9f, 0.5f, 42f), 1.8f);         // close enough to see her, not to join

        // ── controller ──
        var ctrlGo = new GameObject("CaseController");
        var ctrl = ctrlGo.AddComponent<CaseController>();
        ctrl.sightState = Object.FindFirstObjectByType<SightState>();

        // ── UI canvas ──
        var canvasGo = new GameObject("UI_Case");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;  // native res - text stays crisp
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 1.4f;   // in front of the combat canvas
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        // pixel font law: never rescale the UI non-integer — constant pixel
        // size + pixelPerfect keeps BoldPixels on the grid at any window size
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        canvasGo.AddComponent<GraphicRaycaster>();
        var ui = canvasGo.AddComponent<CaseLogUI>();

        // ── HUD, solid pixel style: opaque fills, chunky 3 px borders, ──
        // ── BoldPixels type on the 16 px grid, real key sprites        ──

        // sight meter (top-left)
        var meter = PixelPanel(canvasGo.transform, "SightMeter", VoidSolid, Steel);
        Anchor(meter, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        meter.sizeDelta = new Vector2(288, 64);
        meter.anchoredPosition = new Vector2(16, -16);
        Label(meter, "Cap", "SIGHT", 16, Fog, TextAnchor.UpperLeft, new Vector2(12, -7), new Vector2(200, 18));
        var cells = new List<Image>();
        for (int i = 0; i < 10; i++)
        {
            var c = Panel(meter, $"c{i}", Fog);
            Anchor(c, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            c.sizeDelta = new Vector2(20, 12);
            c.anchoredPosition = new Vector2(12 + i * 26, 9);
            cells.Add(c.GetComponent<Image>());
        }
        ui.sightCells = cells.ToArray();
        ui.sightMeterRoot = meter.gameObject;   // hidden while THE QUEUE runs

        // interact prompt — [F key sprite] + label, bottom-center
        var promptRoot = PixelPanel(canvasGo.transform, "PromptChip", VoidSolid, Steel);
        Anchor(promptRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        promptRoot.sizeDelta = new Vector2(560, 64);
        promptRoot.anchoredPosition = new Vector2(0, 88);
        var keyImg = KeySprite(promptRoot, "KeyF", "key_F", 48);   // 16 px art × 3
        keyImg.anchoredPosition = new Vector2(34, 0);
        ui.promptRoot = promptRoot.gameObject;
        ui.promptText = Label(promptRoot, "Txt", "", 16, Paper, TextAnchor.MiddleLeft, new Vector2(68, 0), new Vector2(470, 56));
        promptRoot.gameObject.SetActive(false);

        // toast (evidence logged + what it UNLOCKS) — top-center, wide
        var toastRoot = PixelPanel(canvasGo.transform, "ToastChip", VoidSolid, Steel);
        Anchor(toastRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        toastRoot.sizeDelta = new Vector2(820, 48);
        toastRoot.anchoredPosition = new Vector2(0, -16);
        ui.toastRoot = toastRoot.gameObject;
        ui.toastText = Label(toastRoot, "Txt", "", 16, Fog, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(800, 44));
        toastRoot.gameObject.SetActive(false);

        // hint chips (bottom-left): real key sprites, not bare letters
        var hintRoot = new GameObject("HintChips", typeof(RectTransform));
        var hintRt = (RectTransform)hintRoot.transform;
        hintRt.SetParent(canvasGo.transform, false);
        Anchor(hintRt, Vector2.zero, Vector2.zero, Vector2.zero);
        hintRt.anchoredPosition = new Vector2(16, 8);
        hintRt.sizeDelta = new Vector2(700, 28);
        void HintChip(string key, string label, float x, float labelWidth)
        {
            var k = KeySprite(hintRt, $"K_{key}", $"key_{key}", 24);
            if (k != null) k.anchoredPosition = new Vector2(x + 12, 0);
            Label(hintRt, $"L_{key}", label, 16, Fog, TextAnchor.MiddleLeft, new Vector2(x + 28, 0), new Vector2(labelWidth, 24));
        }
        HintChip("TAB", "CASE FILE", 0, 120);
        HintChip("E", "HOLD SIGHT", 160, 130);
        HintChip("F", "EXAMINE", 330, 110);
        ui.hintRoot = hintRoot;

        // ── the dossier panel ──
        // 688 tall — must fit a 720 window under ConstantPixelSize
        var panel = Panel(canvasGo.transform, "CasePanel", Void_);
        Anchor(panel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
        panel.sizeDelta = new Vector2(540, 688);
        panel.anchoredPosition = new Vector2(-30, 0);
        Outline(panel);
        Strip(panel, "Accent", 2, 342, Anomaly);
        Label(panel, "Head", "CASE FILE UH-001", 26, Paper, TextAnchor.UpperLeft, new Vector2(24, -22), new Vector2(480, 32));
        Label(panel, "Sub", "ROUTE 9 NORTHBOUND · THE BUS STOP WAITER", 13, Fog, TextAnchor.UpperLeft, new Vector2(24, -58), new Vector2(480, 18));
        ui.counter = Label(panel, "Count", "EVIDENCE 0/6", 16, Anomaly, TextAnchor.UpperLeft, new Vector2(24, -86), new Vector2(480, 22));

        var titles = new List<Text>();
        var tags = new List<Text>();
        string[] rowIcons = { "clue_thestop", "clue_witnesses", "clue_bench", "clue_sediment", "clue_archive", "clue_victim" };
        for (int i = 0; i < 6; i++)
        {
            float y = -112 - i * 44;   // tight pitch: six rows + the ANALYSIS chain fit 688
            var row = Panel(panel, $"Row{i}", Concrete);
            Anchor(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            row.sizeDelta = new Vector2(492, 40);
            row.anchoredPosition = new Vector2(0, y);
            Outline(row);
            var ic = IconSprite(row, "Ic", rowIcons[i], 20);
            if (ic != null) ic.anchoredPosition = new Vector2(18, 0);
            titles.Add(Label(row, "T", "· · · · · ·", 18, Steel, TextAnchor.MiddleLeft, new Vector2(36, 0), new Vector2(340, 36)));
            tags.Add(Label(row, "Tag", "", 12, Fog, TextAnchor.MiddleRight, new Vector2(-12, 0), new Vector2(90, 36), pivotRight: true));
        }
        ui.rowTitles = titles.ToArray();
        ui.rowTags = tags.ToArray();

        // ANALYSIS — the running deduction under the evidence rows
        Label(panel, "AnCap", "ANALYSIS", 14, Fog, TextAnchor.UpperLeft, new Vector2(24, -364), new Vector2(300, 18));
        var an = new List<Text>
        {
            Label(panel, "An0", "", 14, Fog, TextAnchor.UpperLeft, new Vector2(24, -384), new Vector2(492, 18)),
            Label(panel, "An1", "", 14, Fog, TextAnchor.UpperLeft, new Vector2(24, -402), new Vector2(492, 18)),
        };
        ui.analysisLines = an.ToArray();

        // classification
        var cls = Panel(panel, "Classify", new Color(0, 0, 0, 0));
        Anchor(cls, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        cls.sizeDelta = new Vector2(492, 180);
        cls.anchoredPosition = new Vector2(0, 88);
        Label(cls, "Cap", "CLASSIFICATION", 14, Fog, TextAnchor.UpperLeft, new Vector2(0, -2), new Vector2(300, 18));
        // three doctrine lines — the file teaches the player HOW to judge
        ui.classifyNote = Label(cls, "Note", "", 14, Fog, TextAnchor.UpperLeft, new Vector2(0, -22), new Vector2(486, 62));
        ui.classifyNote.horizontalOverflow = HorizontalWrapMode.Wrap;
        var btnRow = new GameObject("Buttons", typeof(RectTransform)).GetComponent<RectTransform>();
        btnRow.SetParent(cls, false);
        btnRow.anchoredPosition = new Vector2(0, -88);   // buttons clear of the doctrine
        void ClsButton(string txt, string sub, float x, bool unhumanity)
        {
            var b = Panel(btnRow, "Btn_" + txt, Concrete);
            Anchor(b, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            b.sizeDelta = new Vector2(232, 62);
            b.anchoredPosition = new Vector2(x, -28);
            Outline(b);
            var btn = b.gameObject.AddComponent<Button>();
            btn.targetGraphic = b.GetComponent<Image>();
            if (unhumanity) UnityEventTools.AddPersistentListener(btn.onClick, ui.OnClassifyUnHumanity);
            else UnityEventTools.AddPersistentListener(btn.onClick, ui.OnClassifyRemnant);
            // each verdict states its reading AND its consequence
            Label(b, "T", $"{txt}\n<color=#979DA8><size=16>{sub}</size></color>", 17,
                unhumanity ? Anomaly : Paper, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(225, 58));
        }
        ClsButton("UN-HUMANITY", "it hunts. destroy it", -125, true);
        ClsButton("REMNANT", "it grieves. end the wait", 125, false);
        ui.doctrine = "Does it hunt and take — or was it never told to stop?\n" +
                      "Both readings fit this file. The verdict is permanent — and it decides how the encounter can end.";
        ui.classifyButtons = btnRow.gameObject;
        ui.classifySection = cls.gameObject;
        ui.verdictStamp = Label(cls, "Stamp", "", 22, Anomaly, TextAnchor.LowerCenter, new Vector2(0, 8), new Vector2(480, 30));

        ui.panelRoot = panel.gameObject;

        // ── the evidence record (reading panel) ──
        var reading = canvasGo.AddComponent<ReadingUI>();
        var rp = Panel(canvasGo.transform, "ReadingPanel", Void_);
        Anchor(rp, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        rp.sizeDelta = new Vector2(720, 620);
        rp.anchoredPosition = new Vector2(-140, 10);
        Outline(rp);
        Strip(rp, "Accent", 2, 308, Anomaly);
        var photoGo = new GameObject("Photo", typeof(RectTransform), typeof(RawImage));
        var photoRt = (RectTransform)photoGo.transform;
        photoRt.SetParent(rp, false);
        Anchor(photoRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        photoRt.sizeDelta = new Vector2(656, 330);
        photoRt.anchoredPosition = new Vector2(0, -20);
        reading.photo = photoGo.GetComponent<RawImage>();
        reading.title = Label(rp, "Title", "", 24, Paper, TextAnchor.UpperLeft, new Vector2(32, -366), new Vector2(500, 36));
        reading.title.verticalOverflow = VerticalWrapMode.Overflow;   // 32 px BoldPixels must never truncate to blank
        reading.stateTag = Label(rp, "Tag", "", 13, Fog, TextAnchor.UpperLeft, new Vector2(32, -398), new Vector2(400, 18));
        reading.body = Label(rp, "Body", "", 18, Fog, TextAnchor.UpperLeft, new Vector2(32, -426), new Vector2(656, 92));
        reading.body.horizontalOverflow = HorizontalWrapMode.Wrap;
        reading.body.color = Paper;
        // legibility law: every record states WHAT IT MEANS and what it UNLOCKS
        reading.meaning = Label(rp, "Meaning", "", 16, Fog, TextAnchor.UpperLeft, new Vector2(32, -524), new Vector2(656, 40));
        reading.meaning.horizontalOverflow = HorizontalWrapMode.Wrap;
        reading.unlocks = Label(rp, "Unlocks", "", 16, Paper, TextAnchor.UpperLeft, new Vector2(32, -572), new Vector2(656, 20));
        var closeHint = Label(rp, "CloseHint", "CLOSE RECORD", 13, Fog, TextAnchor.LowerRight, new Vector2(-24, 14), new Vector2(200, 18), pivotRight: true);
        // pivotRight anchors middle-right; this hint belongs bottom-right,
        // clear of the photograph
        var chRt = (RectTransform)closeHint.transform;
        chRt.anchorMin = chRt.anchorMax = new Vector2(1f, 0f);
        chRt.pivot = new Vector2(1f, 0f);
        chRt.anchoredPosition = new Vector2(-24, 14);
        var closeKey = KeySprite(rp, "CloseKey", "key_F", 24);
        if (closeKey != null)
        {
            // hugging the label: [F] CLOSE RECORD reads as one chip
            closeKey.anchorMin = closeKey.anchorMax = new Vector2(1f, 0f);
            closeKey.pivot = new Vector2(1f, 0f);
            closeKey.anchoredPosition = new Vector2(-158, 12);
        }
        reading.panelRoot = rp.gameObject;
        rp.gameObject.SetActive(false);
        // transient chrome always wins the sibling war: the toast must draw
        // over the dossier and record panels, never under them
        toastRoot.SetAsLastSibling();

        // evidence textures, catalog order (TheStop, Witnesses, Bench, Sediment, Archive, Victim)
        var evidence = new[]
        {
            AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Evidence/EV_TheStop.png"),
            AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Evidence/EV_Witnesses.png"),
            AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Evidence/EV_Bench.png"),
            AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Evidence/EV_Sediment.png"),
            AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Evidence/EV_Archive.png"),
            AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Evidence/EV_Victim.png"),
        };
        int missingEv = evidence.Count(e => e == null);

        // ── the conversation box (Undertale-style, bottom of screen) ──
        var dlg = canvasGo.AddComponent<DialogueUI>();
        var box = PixelPanel(canvasGo.transform, "DialogueBox", VoidSolid, Steel);
        Anchor(box, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        box.sizeDelta = new Vector2(1100, 168);
        box.anchoredPosition = new Vector2(0, 40);
        // speaker name tab, riding the top-left edge
        var tab = PixelPanel(box, "SpeakerTab", Concrete, Steel);
        Anchor(tab, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        tab.sizeDelta = new Vector2(280, 32);
        tab.anchoredPosition = new Vector2(20, 14);
        dlg.speakerTab = Label(tab, "Name", "", 16, Anomaly, TextAnchor.MiddleLeft, new Vector2(14, 0), new Vector2(250, 30));
        dlg.body = Label(box, "Body", "", 18, Paper, TextAnchor.UpperLeft, new Vector2(28, -22), new Vector2(1040, 110));
        dlg.body.horizontalOverflow = HorizontalWrapMode.Wrap;
        // [F] next chip, bottom-right
        var advChip = new GameObject("AdvanceChip", typeof(RectTransform));
        var advRt = (RectTransform)advChip.transform;
        advRt.SetParent(box, false);
        advRt.anchorMin = advRt.anchorMax = new Vector2(1f, 0f);
        advRt.pivot = new Vector2(1f, 0f);
        advRt.anchoredPosition = new Vector2(-16, 12);
        advRt.sizeDelta = new Vector2(140, 26);
        var advKey = KeySprite(advRt, "K_F", "key_F", 24);
        if (advKey != null) advKey.anchoredPosition = new Vector2(12, 13);
        Label(advRt, "next", "next", 16, Fog, TextAnchor.MiddleLeft, new Vector2(28, 13), new Vector2(100, 24));
        dlg.advanceChip = advChip;
        // the two choice buttons — shown only at a branch
        var choices = new GameObject("Choices", typeof(RectTransform));
        var choRt = (RectTransform)choices.transform;
        choRt.SetParent(box, false);
        choRt.anchorMin = choRt.anchorMax = new Vector2(0.5f, 0f);
        choRt.pivot = new Vector2(0.5f, 0f);
        choRt.anchoredPosition = new Vector2(0, 12);
        choRt.sizeDelta = new Vector2(1040, 44);
        Button ChoiceBtn(string name, float x, out Text lbl)
        {
            var b = PixelPanel(choRt, name, Concrete, Steel);
            Anchor(b, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            b.sizeDelta = new Vector2(500, 40);
            b.anchoredPosition = new Vector2(x, 0);
            var btn = b.gameObject.AddComponent<Button>();
            btn.targetGraphic = b.GetComponent<Image>();
            lbl = Label(b, "L", "", 16, Paper, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(480, 36));
            return btn;
        }
        dlg.choiceA = ChoiceBtn("ChoiceA", -258, out var clA);
        dlg.choiceB = ChoiceBtn("ChoiceB", 258, out var clB);
        dlg.choiceALabel = clA; dlg.choiceBLabel = clB;
        UnityEventTools.AddPersistentListener(dlg.choiceA.onClick, dlg.OnChooseA);
        UnityEventTools.AddPersistentListener(dlg.choiceB.onClick, dlg.OnChooseB);
        dlg.choiceRoot = choices;
        dlg.panelRoot = box.gameObject;
        dlg.caseController = ctrl;
        box.gameObject.SetActive(false);
        toastRoot.SetAsLastSibling();   // toast still wins the sibling war

        // ── wiring ──
        var player = GameObject.Find("Player");
        PlayerInteractor inter = null;
        if (player != null)
        {
            inter = player.GetComponent<PlayerInteractor>();
            if (inter == null) inter = player.AddComponent<PlayerInteractor>();
            inter.caseController = ctrl;
            inter.dialogue = dlg;
            inter.story = Object.FindFirstObjectByType<StoryUI>();
            var pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.story = inter.story;
        }
        ctrl.ui = ui;
        ctrl.reading = reading;
        ctrl.evidenceImages = evidence;
        ui.controller = ctrl;
        ui.sightState = ctrl.sightState;
        ui.interactor = inter;
        foreach (var n in madeNodes)
            n.GetComponentInChildren<ClueMarker>(true).controller = ctrl;
        panel.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"case layer built: 7 nodes + markers, reading panel, evidence wired ({6 - missingEv}/6 images)"
             + (player == null ? " WARNING: no player" : "");
    }

    // helpers
    static Material MarkerMat(string path, Color color)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(m);
        return m;
    }

    static RectTransform Panel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return rt;
    }

    static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
    {
        rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot;
    }

    /// Solid pixel chip: border-color frame, opaque fill inset 3 px.
    /// Children added after this sit above the fill.
    static RectTransform PixelPanel(Transform parent, string name, Color fill, Color border)
    {
        var frame = Panel(parent, name, border);
        var inner = Panel(frame, "Fill", fill);
        Anchor(inner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        inner.offsetMin = new Vector2(3, 3);
        inner.offsetMax = new Vector2(-3, -3);
        return frame;
    }

    /// Dossier icon (ReffPixels/Raven pixel art) at an integer scale.
    /// Returns null if the icon isn't imported — layout must survive that.
    internal static RectTransform IconSprite(Transform parent, string name, string slot, int px)
    {
        var path = $"Assets/Art/UI/Icons/icon_{slot}.png";
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        if (ti == null) return null;
        if (ti.textureType != TextureImporterType.Sprite || ti.filterMode != FilterMode.Point)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        Anchor(rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f));
        rt.sizeDelta = new Vector2(px, px);
        var img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        img.preserveAspect = true;
        img.color = Fog;   // icons read as chrome, not content
        return rt;
    }

    /// Key-prompt sprite (Vryell pack, 16 px art) at an integer scale.
    internal static RectTransform KeySprite(Transform parent, string name, string key, int px)
    {
        var path = $"Assets/Art/UI/Keys/{key}.png";
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        if (ti != null && (ti.textureType != TextureImporterType.Sprite || ti.filterMode != FilterMode.Point))
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 16;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        Anchor(rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f));
        rt.sizeDelta = new Vector2(px, px);
        var img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        img.preserveAspect = true;
        return rt;
    }

    static void Outline(RectTransform rt)
    {
        var o = rt.gameObject.AddComponent<Outline>();
        o.effectColor = Steel;
        o.effectDistance = new Vector2(2, -2);
    }

    static void Strip(RectTransform parent, string name, float h, float y, Color c)
    {
        var s = Panel(parent, name, c);
        Anchor(s, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f));
        s.sizeDelta = new Vector2(0, h);
        s.anchoredPosition = new Vector2(0, y);
    }

    static Text Label(Transform parent, string name, string text, int size, Color color,
        TextAnchor align, Vector2 pos, Vector2 sizeDelta,
        bool pivotRight = false, bool anchorBottomCenter = false, bool anchorBottomLeft = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        if (anchorBottomCenter) { rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f); }
        else if (anchorBottomLeft) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero; rt.pivot = Vector2.zero; }
        else if (pivotRight) { rt.anchorMin = new Vector2(1f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f); rt.pivot = new Vector2(1f, 0.5f); }
        else if (align == TextAnchor.UpperLeft || align == TextAnchor.MiddleLeft)
        { rt.anchorMin = new Vector2(0f, align == TextAnchor.UpperLeft ? 1f : 0.5f); rt.anchorMax = rt.anchorMin; rt.pivot = new Vector2(0f, align == TextAnchor.UpperLeft ? 1f : 0.5f); }
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var t = go.GetComponent<Text>();
        t.font = UiFont;
        t.text = text;
        t.fontSize = size <= 23 ? 16 : (size <= 40 ? 32 : 48);  // pixel grid
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        return t;
    }
}
