// UN-HUMANITY — builds SC_Prologue_Core: the persistent rig that carries
// Slice 0 across its location scenes. Player (prologue mover, the real
// sprite) + follow camera + soft key light + the prologue UI + PrologueFlow,
// all under one RIG root that PrologueFlow marks DontDestroyOnLoad.
//
// The UI is CINEMATIC: two letterbox bars and a bottom subtitle band, so the
// 3D world stays visible while the characters perform. No full-screen dossier
// cards. Re-runnable.
//   unity command eval "return PrologueCoreSetup.Build();"

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PrologueCoreSetup
{
    const string kScene = "Assets/Scenes/SC_Prologue_Core.unity";

    static readonly Color Bar = new Color(0f, 0f, 0f, 0.90f);
    static readonly Color Fog = new Color32(0x86, 0x8C, 0x96, 0xFF);
    static readonly Color Paper = new Color32(0xEC, 0xEE, 0xF1, 0xFF);
    static Font UiFont => AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/UI/Fonts/BoldPixels.otf")
                       ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "RIG"))
            Object.DestroyImmediate(g);

        var rig = new GameObject("RIG").transform;
        var flow = rig.gameObject.AddComponent<PrologueFlow>();

        // ── player: the real prologue mover ──
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Sprites/Player.prefab");
        GameObject player;
        if (prefab != null) player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        else { player = new GameObject("Player"); player.AddComponent<CharacterController>(); }
        player.transform.SetParent(rig, false);
        player.transform.position = new Vector3(0f, 0.1f, 0f);
        foreach (var pc in player.GetComponents<PlayerController>()) Object.DestroyImmediate(pc);
        foreach (var pi in player.GetComponents<PlayerInteractor>()) Object.DestroyImmediate(pi);
        var mover = player.GetComponent<PrologueMover>() ?? player.AddComponent<PrologueMover>();
        var sprite = player.transform.Find("Sprite");
        if (sprite != null) mover.spriteQuad = sprite;
        flow.player = player.transform;

        // ── camera ──
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(rig, false);
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.16f, 0.13f, 0.14f);
        cam.fieldOfView = 40f;
        cam.nearClipPlane = 0.2f; cam.farClipPlane = 120f;
        var uac = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam);
        uac.renderPostProcessing = true;
        var rigCam = camGo.AddComponent<CameraRigFollow>();
        rigCam.target = player.transform;
        rigCam.sightState = null;
        rigCam.pitch = 20f; rigCam.distance = 8f; rigCam.lookHeight = 1.2f; rigCam.lateralFollow = 1f;
        flow.cam = cam;

        // ── soft key light so the sprite reads everywhere ──
        var lightGo = new GameObject("RigKey");
        lightGo.transform.SetParent(rig, false);
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var lt = lightGo.AddComponent<Light>();
        lt.type = LightType.Directional;
        lt.color = new Color(1f, 0.95f, 0.9f);
        lt.intensity = 0.9f;

        // ── the cinematic UI ──
        var canvasGo = new GameObject("PrologueUI");
        canvasGo.transform.SetParent(rig, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        // letterbox bars (top thin, bottom taller — it holds the subtitle)
        flow.letterTop = BarEl(canvasGo.transform, "LetterTop", true, 58);
        flow.letterBot = BarEl(canvasGo.transform, "LetterBot", false, 172);

        // time stamp, top-left over the top bar
        flow.timeChip = Text(flow.letterTop.transform, "TimeChip", 16, Fog, TextAnchor.MiddleLeft);
        Place(flow.timeChip, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
              new Vector2(34, 0), new Vector2(700, 34));

        // subtitle band, in the bottom bar
        flow.capSpeaker = Text(flow.letterBot.transform, "Speaker", 16, kFriendHint, TextAnchor.UpperLeft);
        Place(flow.capSpeaker, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
              new Vector2(46, -20), new Vector2(900, 26));
        flow.capBody = Text(flow.letterBot.transform, "Body", 16, Paper, TextAnchor.UpperLeft);
        flow.capBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        var bodyRt = (RectTransform)flow.capBody.transform;
        bodyRt.anchorMin = new Vector2(0f, 0f); bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.offsetMin = new Vector2(46, 30); bodyRt.offsetMax = new Vector2(-46, -52);
        flow.capPrompt = Text(flow.letterBot.transform, "Prompt", 15, Fog, TextAnchor.LowerRight);
        Place(flow.capPrompt, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
              new Vector2(-34, 16), new Vector2(360, 24));

        // fade overlay — on top, starts BLACK (play opens on black, fades in)
        var fadeGo = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        var fadeRt = (RectTransform)fadeGo.transform;
        fadeRt.SetParent(canvasGo.transform, false);
        fadeRt.anchorMin = Vector2.zero; fadeRt.anchorMax = Vector2.one;
        fadeRt.offsetMin = Vector2.zero; fadeRt.offsetMax = Vector2.zero;
        var fadeImg = fadeGo.GetComponent<Image>();
        fadeImg.color = new Color(0, 0, 0, 1f);
        fadeImg.raycastTarget = false;
        flow.fade = fadeImg;

        // layer order INSIDE the overlay canvas: the fade covers the 3D world
        // but sits BELOW the letterbox + subtitle, so a full-black "title card"
        // (the ONE WEEK LATER beat) still shows its text. Otherwise the fade
        // blacks out the caption and you get a dead black screen.
        fadeGo.transform.SetSiblingIndex(0);
        flow.letterTop.transform.SetAsLastSibling();
        flow.letterBot.transform.SetAsLastSibling();

        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.transform.SetParent(rig, false);
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"prologue core rig built: player({(prefab != null ? "prefab" : "stub")}) + camera + letterbox UI + flow";
    }

    static readonly Color kFriendHint = new Color32(0x9A, 0xCF, 0x93, 0xFF);

    // a full-width letterbox bar pinned to the top or bottom edge
    static Image BarEl(Transform parent, string name, bool top, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rt.sizeDelta = new Vector2(0, height);
        rt.anchoredPosition = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = Bar; img.raycastTarget = false;
        return img;
    }

    static Text Text(Transform parent, string name, int size, Color color, TextAnchor align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = UiFont; t.fontSize = size; t.color = color; t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void Place(Text t, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = (RectTransform)t.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }
}
