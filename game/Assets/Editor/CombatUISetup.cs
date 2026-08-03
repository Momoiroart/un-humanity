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

        // kit strip — two lines: description/flavor over the live kit readout
        ui.kitStrip = Label(root, "KitStrip", "", 15, Fog, TextAnchor.MiddleCenter, new Vector2(0, 118), new Vector2(1200, 42));
        ui.kitStrip.verticalOverflow = VerticalWrapMode.Overflow;
        var ksRt = (RectTransform)ui.kitStrip.transform;
        ksRt.anchorMin = ksRt.anchorMax = new Vector2(0.5f, 0f);
        ksRt.pivot = new Vector2(0.5f, 0f);
        ksRt.anchoredPosition = new Vector2(0, 118);

        // ── command grammar: a familiar shell (Undertale/E33) ──
        // Tier-1 is four verbs that never move: CHECK / ACT / KIT / WITHDRAW.
        // Submenus swap IN PLACE of the row; the six real actions keep
        // their engine indices and the three-tier color law.
        RectTransform MenuRow(string rname)
        {
            var go = new GameObject(rname, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(root, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }
        var rowTop = MenuRow("Row_Top");
        var rowAct = MenuRow("Row_Act");
        var rowKit = MenuRow("Row_Kit");
        var rowItem = MenuRow("Row_Item");
        var rowWd = MenuRow("Row_Withdraw");

        (RectTransform panel, Button btn) CmdButton(RectTransform rparent, string bname, string icon,
            float x, float w, string title, string subtitle, out Text nameT, out Text costT)
        {
            var b = Panel(rparent, bname, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Concrete);
            b.sizeDelta = new Vector2(w, 88);
            b.anchoredPosition = new Vector2(x, 66);
            Border(b);
            var btn = b.gameObject.AddComponent<Button>();
            btn.targetGraphic = b.GetComponent<Image>();
            var ic = CaseSetup.IconSprite(b, "Ic", icon, 24);
            if (ic != null) ic.anchoredPosition = new Vector2(20, 0);
            nameT = Label(b, "N", title, 20, Paper, TextAnchor.UpperCenter, new Vector2(0, -12), new Vector2(w - 10, 26));
            costT = Label(b, "C", subtitle, 14, Fog, TextAnchor.LowerCenter, new Vector2(0, 10), new Vector2(w - 10, 22));
            return (b, btn);
        }
        var backBtns = new List<Button>();
        void BackButton(RectTransform rparent, float x)
        {
            var (_, btn) = CmdButton(rparent, "Back", "action_withdraw", x, 120, "BACK", "", out _, out _);
            UnityEventTools.AddPersistentListener(btn.onClick, ui.OnBack);
            backBtns.Add(btn);
        }

        // nine engine actions: 0-5 as ever, 6 ATTACK, 7-8 the ACT talk options
        string[] names = { "FLARE", "RADIO CHECK-IN", "FLASH", "ESCORT", "HOLD", "WITHDRAW", "ATTACK", "SPEAK THE DATE", "SHOW HER THE PHOTO" };
        string[] iconSlots = { "action_flare", "action_radio", "action_photograph", "action_escort", "action_hold", "action_withdraw", "action_photograph", "action_radio", "clue_victim" };
        string[] descs =
        {
            "Blocks its next cheat for 1 round. Costs 1 flare — a lit flare has a schedule.",
            "Restores the round counter and blocks 1 cheat. 3-round cooldown. Dispatch expects you.",
            "Blocks cheats 2 rounds, files the photo as evidence. 1 film. Works through [WAITING].",
            "Walks her one step out. Only works on a clean round — order must hold to move her.",
            "",
            "Confirm: end the encounter. You lose nothing; she stays in the queue until you return.",
            "Free jab. File complete: cancels its next cheat (1 round). File open: it doesn't notice.",
            "Breaks [WAITING], blocks cheats 2 rounds. Once per fight — it can't out-wait a date.",
            "Restores 2 of her clock. Once per fight — she remembers she was leaving. Needs a normal turn.",
        };
        var btns = new Button[9]; var bNames = new Text[9]; var bCosts = new Text[9];
        void Describe(RectTransform b, string d)
        {
            if (string.IsNullOrEmpty(d)) return;
            var h = b.gameObject.AddComponent<HoverDescriber>();
            h.description = d;
            h.ui = ui;
        }
        void RealAction(RectTransform rparent, int idx, float x, float w = 230)
        {
            var (bp, btn) = CmdButton(rparent, $"Action_{names[idx]}", iconSlots[idx], x, w,
                names[idx], "", out var nT, out var cT);
            UnityEventTools.AddIntPersistentListener(btn.onClick, ui.OnAction, idx);
            Describe(bp, descs[idx]);
            btns[idx] = btn; bNames[idx] = nT; bCosts[idx] = cT;
        }

        // tier-1: five verbs, always in the same place
        RealAction(rowTop, 6, -480);   // ATTACK — snapshot jab / Exposure
        string[] topTitles = { "SKILL", "ITEM", "ACT", "RUN" };
        string[] topSubs = { "your craft", "carried gear", "use the case", "leave the fight" };
        string[] topDescs =
        {
            "Your occupation's moves. Time is your craft — and time is what it cheats.",
            "Carried gear, spent for real when used. What runs out stays run out.",
            "Use the case. Evidence you collected becomes moves — some can end this without a blow.",
            "Leave the fight. No penalty, no chase — but the case stays open and so does the wait.",
        };
        string[] topIcons = { "action_photograph", "action_flare", "action_radio", "action_withdraw" };
        float[] topX = { -240, 0, 240, 480 };
        int[] topHooks = { 2, 4, 1, 3 };   // SKILL->rowKit, ITEM->rowItem, ACT->rowAct, RUN->rowWd
        var topBtns = new Button[4]; var topNames = new Text[4];
        for (int i = 0; i < 4; i++)
        {
            var (bp, btn) = CmdButton(rowTop, $"Top_{topTitles[i]}", topIcons[i], topX[i], 230,
                topTitles[i], topSubs[i], out var nT, out _);
            UnityEventTools.AddIntPersistentListener(btn.onClick, ui.OnTop, topHooks[i]);
            Describe(bp, topDescs[i]);
            topBtns[i] = btn; topNames[i] = nT;
        }
        ui.topButtons = topBtns;
        ui.topNames = topNames;

        // SKILL — the Photographer's craft (single item, centered: intentional)
        RealAction(rowKit, 2, -120);   // FLASH (engine: Photograph)
        BackButton(rowKit, 120);
        // ITEM — carried gear
        RealAction(rowItem, 0, -120);  // FLARE
        BackButton(rowItem, 120);
        // ACT — what the case taught you (HOLD retired from the menu:
        // a blind ATTACK already passes the round; the engine keeps it)
        RealAction(rowAct, 1, -345, 220);   // RADIO CHECK-IN
        RealAction(rowAct, 3, -115, 220);   // ESCORT
        RealAction(rowAct, 7, 115, 220);    // SPEAK THE DATE
        RealAction(rowAct, 8, 345, 220);    // SHOW HER THE PHOTO
        BackButton(rowAct, 525);
        // RUN — the confirm IS the submenu
        RealAction(rowWd, 5, -120, 260);
        BackButton(rowWd, 100);

        ui.actionButtons = btns;
        ui.actionNames = bNames;
        ui.actionCosts = bCosts;
        ui.backButtons = backBtns.ToArray();
        ui.rowTop = rowTop.gameObject;
        ui.rowAct = rowAct.gameObject;
        ui.rowKit = rowKit.gameObject;
        ui.rowItem = rowItem.gameObject;
        ui.rowWithdraw = rowWd.gameObject;
        rowAct.gameObject.SetActive(false);
        rowKit.gameObject.SetActive(false);
        rowItem.gameObject.SetActive(false);
        rowWd.gameObject.SetActive(false);

        // ── the two bars: pure views of engine state ──
        // ITS HOLD — under the queue strip, wine segments that drain as
        // order is sustained (and never move while unclassified)
        var holdCap = Label(root, "HoldCap", "ITS HOLD · ORDER DRAINS", 13, Fog,
            TextAnchor.MiddleCenter, new Vector2(0, -236), new Vector2(400, 18), anchorTop: true);
        var holdSegs = new List<Image>();
        for (int i = 0; i < 3; i++)
        {
            var seg = Panel(root, $"Hold{i}", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Color32(0x5E, 0x20, 0x36, 0xFF));
            seg.sizeDelta = new Vector2(64, 12);
            seg.anchoredPosition = new Vector2(-72 + i * 72, -256);
            Border(seg);
            holdSegs.Add(seg.GetComponent<Image>());
        }
        ui.holdSegments = holdSegs.ToArray();

        // COMPOSURE — bottom-right above the bar; 10 fog cells
        var compCap = Label(root, "CompCap", "COMPOSURE · 0 = EJECTED", 13, Fog,
            TextAnchor.MiddleRight, new Vector2(-16, 178), new Vector2(300, 16), pivotRight: true);
        var ccRt = compCap.rectTransform;
        ccRt.anchorMin = ccRt.anchorMax = new Vector2(1f, 0f);
        ccRt.pivot = new Vector2(1f, 0f);
        ccRt.anchoredPosition = new Vector2(-16, 176);
        var compCells = new List<Image>();
        for (int i = 0; i < 10; i++)
        {
            var c = Panel(root, $"Comp{i}", new Vector2(1f, 0f), new Vector2(1f, 0f), Fog);
            c.sizeDelta = new Vector2(18, 10);
            c.anchoredPosition = new Vector2(-226 + i * 22, 160);
            compCells.Add(c.GetComponent<Image>());
        }
        ui.composureCells = compCells.ToArray();

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
            "A jab through the frame. Modest, human — with a classified file, it EXPOSES the cheat.",
            "A date is a schedule. It cannot out-wait a schedule.",
            "A photo proves a moment happened. She remembers hers.",
        };
        ui.menuHints = new[]
        {
            "Attack, skill, item, act — or run. The five moves a human has. It has one.",
            "ACT — use what the case taught you. Evidence unlocks these; some can end a fight outright.",
            "SKILL — your craft. FLASH blocks its cheats for 2 rounds and files the photo as evidence.",
            "ITEM — carried and spent for real. A FLARE buys 1 round where nothing bends.",
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
