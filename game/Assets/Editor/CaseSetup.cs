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
    static readonly Color Concrete = new Color32(0x1C, 0x1F, 0x25, 0xFF);
    static readonly Color Steel = new Color32(0x3A, 0x3F, 0x48, 0xFF);
    static readonly Color Fog = new Color32(0x97, 0x9D, 0xA8, 0xFF);
    static readonly Color Paper = new Color32(0xE7, 0xE9, 0xEC, 0xFF);
    static readonly Color Anomaly = new Color32(0xE4, 0x56, 0x8A, 0xFF);
    static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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
        // the stop cluster, spread along the sidewalk (Z 39.2 → 43.4)
        Node(ClueId.Victim, "04_Victim", new Vector3(-5.6f, 0.5f, 39.2f));
        Node(ClueId.TheStop, "01_TheStop", new Vector3(-4.6f, 0.5f, 41.2f), 1.6f);
        Node(ClueId.Bench, "08_Bench", new Vector3(-6.9f, 0.5f, 41.9f), 1.6f);
        Node(ClueId.Sediment, "15_Sediment", new Vector3(-7.9f, 0.5f, 43.4f), 1.6f);
        // the spread-out three
        Node(ClueId.Witnesses, "06_WitnessA", new Vector3(-6.4f, 0.5f, 14f), 2.4f);
        Node(ClueId.Witnesses, "09_WitnessB", new Vector3(6.3f, 0.5f, 24f), 2.4f);
        Node(ClueId.Archive, "16_Archive", new Vector3(-6.8f, 0.5f, 28f), 2.0f);

        // ── controller ──
        var ctrlGo = new GameObject("CaseController");
        var ctrl = ctrlGo.AddComponent<CaseController>();
        ctrl.sightState = Object.FindFirstObjectByType<SightState>();

        // ── UI canvas ──
        var canvasGo = new GameObject("UI_Case");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 1.4f;   // in front of the combat canvas
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();
        var ui = canvasGo.AddComponent<CaseLogUI>();

        // HUD: sight meter (top-left)
        var meter = Panel(canvasGo.transform, "SightMeter", Void_);
        Anchor(meter, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        meter.sizeDelta = new Vector2(268, 54);
        meter.anchoredPosition = new Vector2(16, -56);
        Outline(meter);
        Label(meter, "Cap", "SIGHT", 12, Fog, TextAnchor.UpperLeft, new Vector2(10, -6), new Vector2(200, 16));
        var cells = new List<Image>();
        for (int i = 0; i < 10; i++)
        {
            var c = Panel(meter, $"c{i}", Fog);
            Anchor(c, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            c.sizeDelta = new Vector2(20, 10);
            c.anchoredPosition = new Vector2(12 + i * 25, 10);
            cells.Add(c.GetComponent<Image>());
        }
        ui.sightCells = cells.ToArray();

        // HUD: interact prompt + toast (bottom center)
        ui.promptText = Label(canvasGo.transform, "Prompt", "", 20, Paper, TextAnchor.MiddleCenter, new Vector2(0, 120), new Vector2(900, 30), anchorBottomCenter: true);
        ui.toastText = Label(canvasGo.transform, "Toast", "", 17, Fog, TextAnchor.MiddleCenter, new Vector2(0, 156), new Vector2(1100, 26), anchorBottomCenter: true);
        Label(canvasGo.transform, "Hint", "TAB — case file · E hold — Sight · F — examine", 13, Fog, TextAnchor.LowerLeft, new Vector2(16, 10), new Vector2(600, 18), anchorBottomLeft: true);

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
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        return t;
    }
}
