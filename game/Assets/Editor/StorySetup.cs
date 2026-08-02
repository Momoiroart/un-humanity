// UN-HUMANITY — builds the story-beat card UI (dispatch / resolution /
// endings / bookend) in the dossier identity. Re-runnable.
//   unity command eval "return StorySetup.Build();"

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class StorySetup
{
    const string kScene = "Assets/Scenes/SC_02_StreetBlock.unity";

    static readonly Color Void_ = new Color32(0x0B, 0x0C, 0x0F, 0xFA);
    static readonly Color Dim = new Color(0.02f, 0.02f, 0.03f, 0.72f);
    static readonly Color Concrete = new Color32(0x1C, 0x1F, 0x25, 0xFF);
    static readonly Color Steel = new Color32(0x3A, 0x3F, 0x48, 0xFF);
    static readonly Color Fog = new Color32(0x97, 0x9D, 0xA8, 0xFF);
    static readonly Color Paper = new Color32(0xE7, 0xE9, 0xEC, 0xFF);
    static readonly Color Anomaly = new Color32(0xE4, 0x56, 0x8A, 0xFF);
    static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "UI_Story"))
            Object.DestroyImmediate(g);

        var canvasGo = new GameObject("UI_Story");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 1.2f;    // front of everything
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();
        var story = canvasGo.AddComponent<StoryUI>();

        // full-screen dim + centered card
        var dim = Panel(canvasGo.transform, "Dim", Dim);
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one;
        dim.offsetMin = Vector2.zero; dim.offsetMax = Vector2.zero;

        var card = Panel(dim, "Card", Void_);
        card.anchorMin = new Vector2(0.5f, 0.5f); card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(960, 600);
        var o = card.gameObject.AddComponent<Outline>();
        o.effectColor = Steel; o.effectDistance = new Vector2(2, -2);
        Strip(card, "AccentT", 2, 298, Anomaly);
        Strip(card, "AccentB", 2, -298, Anomaly);

        story.title = Label(card, "Title", "", 30, Paper, TextAnchor.UpperCenter, new Vector2(0, -40), new Vector2(880, 40));
        story.body = Label(card, "Body", "", 20, Fog, TextAnchor.UpperCenter, new Vector2(0, -110), new Vector2(820, 320));
        story.body.horizontalOverflow = HorizontalWrapMode.Wrap;
        story.body.color = Paper;
        story.prompt = Label(card, "Prompt", "", 15, Fog, TextAnchor.LowerCenter, new Vector2(0, 26), new Vector2(600, 22));

        // resolution choices
        var choices = new GameObject("Choices", typeof(RectTransform)).GetComponent<RectTransform>();
        choices.SetParent(card, false);
        choices.anchoredPosition = new Vector2(0, -170);
        var btns = new List<Button>(); var labels = new List<Text>();
        for (int i = 0; i < 3; i++)
        {
            var b = Panel(choices, $"Choice{i}", Concrete);
            b.anchorMin = new Vector2(0.5f, 0.5f); b.anchorMax = new Vector2(0.5f, 0.5f);
            b.pivot = new Vector2(0.5f, 0.5f);
            b.sizeDelta = new Vector2(272, 92);
            b.anchoredPosition = new Vector2(-288 + i * 288, 0);
            var bo = b.gameObject.AddComponent<Outline>();
            bo.effectColor = Steel; bo.effectDistance = new Vector2(2, -2);
            var btn = b.gameObject.AddComponent<Button>();
            btn.targetGraphic = b.GetComponent<Image>();
            UnityEventTools.AddIntPersistentListener(btn.onClick, story.OnChoice, i);
            var t = Label(b, "L", "", 16, Paper, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(262, 86));
            t.supportRichText = true;
            btns.Add(btn); labels.Add(t);
        }
        story.choiceButtons = btns.ToArray();
        story.choiceLabels = labels.ToArray();
        story.choicesRoot = choices.gameObject;

        story.panelRoot = dim.gameObject;
        story.combatUI = Object.FindFirstObjectByType<QueueCombatUI>();
        story.caseController = Object.FindFirstObjectByType<CaseController>();
        dim.gameObject.SetActive(false);
        choices.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        bool ok = story.combatUI != null && story.caseController != null;
        return "story beats built (dispatch / resolution / endings / bookend)" + (ok ? "" : " WARNING: refs missing");
    }

    static RectTransform Panel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return rt;
    }

    static void Strip(RectTransform parent, string name, float h, float y, Color c)
    {
        var s = Panel(parent, name, c);
        s.anchorMin = new Vector2(0f, 0.5f); s.anchorMax = new Vector2(1f, 0.5f);
        s.pivot = new Vector2(0.5f, 0.5f);
        s.sizeDelta = new Vector2(0, h);
        s.anchoredPosition = new Vector2(0, y);
    }

    static Text Label(Transform parent, string name, string text, int size, Color color,
        TextAnchor align, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        if (align == TextAnchor.UpperCenter) { rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f); }
        else if (align == TextAnchor.LowerCenter) { rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f); }
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var t = go.GetComponent<Text>();
        t.font = UiFont;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        return t;
    }
}
