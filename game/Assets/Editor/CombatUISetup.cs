// UN-HUMANITY — builds THE QUEUE's combat UI canvas in the dossier
// identity (void/concrete/steel panels, fog/paper text, anomaly reserved
// for the violations). Screen-space OVERLAY + pixelPerfect (pixel law). Re-runnable.
//   unity command eval "return CombatUISetup.Build();"

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CombatUISetup
{
    const string kScene = "Assets/Scenes/SC_02_StreetBlock.unity";

    static readonly Color Void_ = new Color32(0x0B, 0x0C, 0x0F, 0xF2);
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
        foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "UI_Combat"))
            Object.DestroyImmediate(g);

        var canvasGo = new GameObject("UI_Combat");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;  // native res - text stays crisp
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        // pixel font law: never rescale the UI non-integer — constant pixel
        // size + pixelPerfect keeps BoldPixels on the grid at any window size
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        canvasGo.AddComponent<GraphicRaycaster>();
        var ui = canvasGo.AddComponent<QueueCombatUI>();

        var root = Panel(canvasGo.transform, "Panels", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0));
        ui.panelsRoot = root.gameObject;

        // ── classbar ──
        var bar = Panel(root, "Classbar", new Vector2(0f, 1f), new Vector2(1f, 1f), Void_);
        bar.sizeDelta = new Vector2(0, 40); bar.anchoredPosition = new Vector2(0, -20);
        Strip(bar, "AccentTop", 2, 20, Anomaly);
        Label(bar, "L", "■ CLASSIFIED // THE QUEUE — FILE UH-001", 18, Fog, TextAnchor.MiddleLeft, new Vector2(16, 0), new Vector2(900, 40));
        // Fog, not rose — pure chrome must not spend the screen's one accent
        Label(bar, "R", "LEVEL-4 CLEARANCE", 18, Fog, TextAnchor.MiddleRight, new Vector2(-16, 0), new Vector2(400, 40), pivotRight: true);

        // ── round counter ──
        var round = Panel(root, "RoundBox", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Concrete);
        round.sizeDelta = new Vector2(180, 96); round.anchoredPosition = new Vector2(0, -114);
        Border(round);
        Label(round, "Cap", "ROUND", 13, Fog, TextAnchor.UpperCenter, new Vector2(0, -7), new Vector2(160, 18));
        ui.roundValue = Label(round, "Val", "01", 40, Paper, TextAnchor.LowerCenter, new Vector2(0, 8), new Vector2(160, 48));

        // queue order line under the round box (clear of the box's -162 edge)
        ui.queueOrder = Label(root, "QueueOrder", "QUEUE", 18, Fog, TextAnchor.MiddleCenter, new Vector2(0, -180), new Vector2(900, 28), anchorTop: true);
        ui.queueOrder.supportRichText = true;

        // knowledge line — what the file lets this fight BE
        ui.knowledgeLine = Label(root, "Knowledge", "", 15, Fog, TextAnchor.MiddleCenter, new Vector2(0, -210), new Vector2(1100, 24), anchorTop: true);
        ui.knowledgeLine.supportRichText = true;

        // ── the Waiter panel + its EMPTY cost frame ──
        // low enough that the weakpoint knowledge line clears it even on
        // 720-high windows
        var wp = Panel(root, "WaiterPanel", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Void_);
        wp.sizeDelta = new Vector2(360, 170); wp.anchoredPosition = new Vector2(200, 40);
        Border(wp);
        Label(wp, "Name", "THE WAITER", 24, Anomaly, TextAnchor.UpperLeft, new Vector2(14, -10), new Vector2(330, 30));
        ui.waiterStatus = Label(wp, "Status", "it waits.", 16, Fog, TextAnchor.UpperLeft, new Vector2(14, -44), new Vector2(330, 44));
        Label(wp, "CostCap", "COST", 13, Fog, TextAnchor.UpperLeft, new Vector2(14, -94), new Vector2(100, 18));
        var costFrame = Panel(wp, "CostFrame", new Vector2(0f, 0f), new Vector2(0f, 0f), new Color32(0x0B, 0x0C, 0x0F, 0xFF));
        costFrame.sizeDelta = new Vector2(330, 40); costFrame.anchoredPosition = new Vector2(179, 32);
        Border(costFrame);   // deliberately EMPTY of a price — the design is the statement
        // a steel redaction dash so the emptiness reads authored, not broken
        Label(costFrame, "Dash", "———", 16, Steel, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(120, 24));

        // ── victim clock ──
        var vc = Panel(root, "VictimClock", new Vector2(1f, 1f), new Vector2(1f, 1f), Void_);
        vc.sizeDelta = new Vector2(330, 76); vc.anchoredPosition = new Vector2(-181, -120);
        Border(vc);
        Label(vc, "Cap", "THE COMMUTER — TIME SHE HAS LEFT", 13, Fog, TextAnchor.UpperLeft, new Vector2(12, -8), new Vector2(310, 18));
        var cells = new List<Image>();
        for (int i = 0; i < 10; i++)
        {
            var c = Panel(vc, $"Cell{i}", new Vector2(0f, 0f), new Vector2(0f, 0f), Fog);
            c.sizeDelta = new Vector2(24, 12);
            c.anchoredPosition = new Vector2(24 + i * 30, 22);
            cells.Add(c.GetComponent<Image>());
        }
        ui.victimCells = cells.ToArray();

        // ── log ──
        var log = Panel(root, "Log", new Vector2(0f, 0f), new Vector2(0f, 0f), Void_);
        log.sizeDelta = new Vector2(760, 150); log.anchoredPosition = new Vector2(396, 235);
        Border(log);
        var lines = new List<Text>();
        for (int i = 0; i < 5; i++)
        {
            var t = Label(log, $"Line{i}", "", 15, Fog, TextAnchor.UpperLeft, new Vector2(12, -8 - i * 27), new Vector2(736, 26));
            t.supportRichText = true;
            lines.Add(t);
        }
        ui.logLines = lines.ToArray();

        // kit strip — the occupation-power voice, just above the action bar
        ui.kitStrip = Label(root, "KitStrip", "", 15, Fog, TextAnchor.MiddleCenter, new Vector2(0, 122), new Vector2(1200, 24));
        var ksRt = (RectTransform)ui.kitStrip.transform;
        ksRt.anchorMin = ksRt.anchorMax = new Vector2(0.5f, 0f);
        ksRt.pivot = new Vector2(0.5f, 0f);
        ksRt.anchoredPosition = new Vector2(0, 122);

        // ── action bar (6: the kit + withdraw) ──
        string[] names = { "FLARE", "RADIO CHECK-IN", "PHOTOGRAPH", "ESCORT", "HOLD", "WITHDRAW" };
        string[] iconSlots = { "action_flare", "action_radio", "action_photograph", "action_escort", "action_hold", "action_withdraw" };
        var btns = new List<Button>(); var bNames = new List<Text>(); var bCosts = new List<Text>();
        for (int i = 0; i < names.Length; i++)
        {
            // 230 px at 240 pitch = 1430 total — fits any window >= 1440 wide
            // (the old 1550 clipped even the ultrawide dev window)
            var b = Panel(root, $"Action_{names[i]}", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Concrete);
            b.sizeDelta = new Vector2(230, 88);
            b.anchoredPosition = new Vector2(-600 + i * 240, 66);
            Border(b);
            var btn = b.gameObject.AddComponent<Button>();
            btn.targetGraphic = b.GetComponent<Image>();
            int idx = i;
            UnityEventTools.AddIntPersistentListener(btn.onClick, ui.OnAction, idx);
            var icon = CaseSetup.IconSprite(b, "Ic", iconSlots[i], 24);
            if (icon != null) icon.anchoredPosition = new Vector2(20, 0);
            bNames.Add(Label(b, "N", names[i], 20, Paper, TextAnchor.UpperCenter, new Vector2(0, -12), new Vector2(220, 26)));
            bCosts.Add(Label(b, "C", "", 14, Fog, TextAnchor.LowerCenter, new Vector2(0, 10), new Vector2(220, 22)));
            btns.Add(btn);
        }
        ui.actionButtons = btns.ToArray();
        ui.actionNames = bNames.ToArray();
        ui.actionCosts = bCosts.ToArray();

        // occupation-power voice: every action names its REAL cost —
        // paying costs is what makes us not-them (GDD §6, v0.4)
        ui.actionFlavor = new[]
        {
            "Burn one flare. It ends on its own clock — paid time is a weapon.",
            "One call, three rounds of silence after. A schedule cuts both ways.",
            "One frame of film, spent. The flash proves a moment happened.",
            "Your whole turn, spent walking her out at human speed.",
            "Costs your time — the one thing it farms. Spend it knowingly.",
            "Costs the case, not you. Stopping your own wait is always legal.",
        };

        // ── outcome banner ──
        var banner = Panel(root, "Outcome", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Void_);
        banner.sizeDelta = new Vector2(1300, 130); banner.anchoredPosition = new Vector2(0, 60);
        Strip(banner, "T", 2, 65, Anomaly);
        Strip(banner, "B", 2, -65, Anomaly);
        ui.outcomeText = Label(banner, "Txt", "", 34, Paper, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1260, 60));
        ui.outcomeBanner = banner.gameObject;

        // hint chip — outside panelsRoot, but QueueCombatUI hides it while a
        // fight runs so it never overlaps the action bar
        ui.engageHint = Label(canvasGo.transform, "Hint", "T — engage / disengage THE QUEUE (gray-box)", 14, Fog,
              TextAnchor.LowerRight, new Vector2(-14, 10), new Vector2(600, 20), pivotRight: true, anchorBottomRight: true).gameObject;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "combat UI built (dossier identity, screen-space camera)";
    }

    // ── tiny builder helpers ──
    static RectTransform Panel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        if (aMin == Vector2.zero && aMax == Vector2.one) { rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        go.GetComponent<Image>().color = color;
        return rt;
    }

    static void Border(RectTransform rt)
    {
        var edge = new GameObject("Border", typeof(RectTransform), typeof(Outline));
        // Outline on the Image itself is cheaper:
        Object.DestroyImmediate(edge);
        var o = rt.gameObject.AddComponent<Outline>();
        o.effectColor = Steel;
        o.effectDistance = new Vector2(2, -2);
    }

    static void Strip(RectTransform parent, string name, float h, float y, Color c)
    {
        var s = Panel(parent, name, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), c);
        s.sizeDelta = new Vector2(0, h);
        s.anchoredPosition = new Vector2(0, y);
    }

    static Text Label(Transform parent, string name, string text, int size, Color color,
        TextAnchor align, Vector2 pos, Vector2 sizeDelta,
        bool pivotRight = false, bool anchorTop = false, bool anchorBottomRight = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        if (anchorTop) { rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f); }
        else if (anchorBottomRight) { rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = new Vector2(1f, 0f); rt.pivot = new Vector2(1f, 0f); }
        else if (pivotRight) { rt.anchorMin = new Vector2(1f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f); rt.pivot = new Vector2(1f, 0.5f); }
        else if (align == TextAnchor.UpperCenter)
        { rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f); }
        else if (align == TextAnchor.LowerCenter)
        { rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f); }
        else if (align == TextAnchor.MiddleLeft || align == TextAnchor.UpperLeft || align == TextAnchor.LowerLeft)
        { rt.anchorMin = new Vector2(0f, align == TextAnchor.MiddleLeft ? 0.5f : 1f); rt.anchorMax = rt.anchorMin; rt.pivot = new Vector2(0f, align == TextAnchor.UpperLeft ? 1f : 0.5f); }
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
