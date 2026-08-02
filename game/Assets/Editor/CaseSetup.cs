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
        void Node(ClueId id, string label, Vector3 pos, float radius = 1.8f)
        {
            var go = new GameObject($"Clue_{label}");
            go.transform.SetParent(nodesRoot, false);
            go.transform.position = pos;
            var n = go.AddComponent<ClueNode>();
            n.clueId = id;
            n.radius = radius;

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
        Node(ClueId.Witnesses, "06_WitnessA", new Vector3(-6.4f, 0.5f, 14f), 2.4f);
        Node(ClueId.Witnesses, "09_WitnessB", new Vector3(6.3f, 0.5f, 24f), 2.4f);
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

        // toast (evidence logged, sight warnings) — top-center, clear of the prompt
        var toastRoot = PixelPanel(canvasGo.transform, "ToastChip", VoidSolid, Steel);
        Anchor(toastRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        toastRoot.sizeDelta = new Vector2(640, 48);
        toastRoot.anchoredPosition = new Vector2(0, -16);
        ui.toastRoot = toastRoot.gameObject;
        ui.toastText = Label(toastRoot, "Txt", "", 16, Fog, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(620, 44));
        toastRoot.gameObject.SetActive(false);

        // hint line (bottom-left)
        Label(canvasGo.transform, "Hint", "TAB CASE FILE   E HOLD SIGHT   F EXAMINE", 16, Fog, TextAnchor.LowerLeft, new Vector2(16, 10), new Vector2(700, 20), anchorBottomLeft: true);

        // ── the dossier panel ──
        var panel = Panel(canvasGo.transform, "CasePanel", Void_);
        Anchor(panel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
        panel.sizeDelta = new Vector2(540, 760);
        panel.anchoredPosition = new Vector2(-30, 0);
        Outline(panel);
        Strip(panel, "Accent", 2, 378, Anomaly);
        Label(panel, "Head", "CASE FILE UH-001", 26, Paper, TextAnchor.UpperLeft, new Vector2(24, -22), new Vector2(480, 32));
        Label(panel, "Sub", "ROUTE 9 NORTHBOUND · THE BUS STOP WAITER", 13, Fog, TextAnchor.UpperLeft, new Vector2(24, -58), new Vector2(480, 18));
        ui.counter = Label(panel, "Count", "EVIDENCE 0/6", 16, Anomaly, TextAnchor.UpperLeft, new Vector2(24, -86), new Vector2(480, 22));

        var titles = new List<Text>();
        var tags = new List<Text>();
        for (int i = 0; i < 6; i++)
        {
            float y = -130 - i * 64;
            var row = Panel(panel, $"Row{i}", Concrete);
            Anchor(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            row.sizeDelta = new Vector2(492, 54);
            row.anchoredPosition = new Vector2(0, y);
            Outline(row);
            titles.Add(Label(row, "T", "· · · · · ·", 18, Steel, TextAnchor.MiddleLeft, new Vector2(14, 0), new Vector2(360, 50)));
            tags.Add(Label(row, "Tag", "", 12, Fog, TextAnchor.MiddleRight, new Vector2(-12, 0), new Vector2(90, 50), pivotRight: true));
        }
        ui.rowTitles = titles.ToArray();
        ui.rowTags = tags.ToArray();

        // classification
        var cls = Panel(panel, "Classify", new Color(0, 0, 0, 0));
        Anchor(cls, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        cls.sizeDelta = new Vector2(492, 190);
        cls.anchoredPosition = new Vector2(0, 108);
        Label(cls, "Cap", "CLASSIFICATION", 14, Fog, TextAnchor.UpperLeft, new Vector2(0, -2), new Vector2(300, 18));
        ui.classifyNote = Label(cls, "Note", "", 14, Fog, TextAnchor.UpperLeft, new Vector2(0, -26), new Vector2(480, 40));
        var btnRow = new GameObject("Buttons", typeof(RectTransform)).GetComponent<RectTransform>();
        btnRow.SetParent(cls, false);
        btnRow.anchoredPosition = new Vector2(0, -40);
        void ClsButton(string txt, float x, bool unhumanity)
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
            Label(b, "T", txt, 17, unhumanity ? Anomaly : Paper, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(225, 58));
        }
        ClsButton("UN-HUMANITY", -125, true);
        ClsButton("REMNANT", 125, false);
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
        reading.title = Label(rp, "Title", "", 24, Paper, TextAnchor.UpperLeft, new Vector2(32, -366), new Vector2(500, 30));
        reading.stateTag = Label(rp, "Tag", "", 13, Fog, TextAnchor.UpperLeft, new Vector2(32, -398), new Vector2(400, 18));
        reading.body = Label(rp, "Body", "", 18, Fog, TextAnchor.UpperLeft, new Vector2(32, -426), new Vector2(656, 150));
        reading.body.horizontalOverflow = HorizontalWrapMode.Wrap;
        reading.body.color = Paper;
        Label(rp, "CloseHint", "F — CLOSE RECORD", 13, Fog, TextAnchor.LowerRight, new Vector2(-24, 14), new Vector2(300, 18), pivotRight: true);
        reading.panelRoot = rp.gameObject;
        rp.gameObject.SetActive(false);

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

        // ── wiring ──
        var player = GameObject.Find("Player");
        PlayerInteractor inter = null;
        if (player != null)
        {
            inter = player.GetComponent<PlayerInteractor>();
            if (inter == null) inter = player.AddComponent<PlayerInteractor>();
            inter.caseController = ctrl;
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

    /// Key-prompt sprite (Vryell pack, 16 px art) at an integer scale.
    static RectTransform KeySprite(Transform parent, string name, string key, int px)
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
