// UN-HUMANITY — SC_P3_Handoff, rebuilt to the SC_HANDOFF master blueprint
// (/Lattest). "One week later": a low, behind-the-arrival-point HD-2D establisher
// looking straight DOWN a damp, overcast early-evening street corridor — dense
// facade blocks converging both sides toward a hazy vanishing point, a wire web
// on utility poles, warm sodium lamp pools on wet-black asphalt, the pink
// "UH-001 STREET BLOCK" neon breathing on the right, a route + stop sign in the
// foreground, and a lone placeholder figure on the near crosswalk. Drained
// crimson-mauve, quiet, anticipatory. Hands off to SC_02.
//
// Kit = SC_HANDOFF (12 meshes) + procedural ground/lamps/poles/windows/skyline.
// Cast = placeholder billboard (SPUM off). Re-runnable.
//   unity command eval "return HandoffLayout.Build();"

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class HandoffLayout
{
    const string kScene = "Assets/Scenes/SC_P3_Handoff.unity";
    const string kKit = "Assets/Art/Prologue/SC_HANDOFF";
    const string kMatDir = "Assets/Art/Prologue/Materials";
    const string kSprDir = "Assets/Art/Sprites";

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "HANDOFF" || g.name == "MOOD"))
            Object.DestroyImmediate(g);
        var root = new GameObject("HANDOFF").transform;

        // ── GROUND: wet-asphalt road corridor + raised sidewalks + curbs +
        // foreground crosswalk (primitives — the kit has no ground). ──
        Box(root, "Road", new Vector3(0, -0.1f, 20f), new Vector3(7f, 0.2f, 54f), "AsphaltWet");
        foreach (int s in new[] { -1, 1 })
        {
            Box(root, "Sidewalk", new Vector3(s * 4.75f, 0.075f, 20f), new Vector3(2.5f, 0.15f, 54f), "Concrete");
            Box(root, "Curb", new Vector3(s * 3.5f, 0.2f, 20f), new Vector3(0.3f, 0.4f, 54f), "Concrete");
        }
        for (int i = 0; i < 7; i++)   // foreground crosswalk stripes at the handoff zone
            Box(root, "Crosswalk", new Vector3(-1.8f + i * 0.6f, 0.012f, 1.5f), new Vector3(0.42f, 0.02f, 3.2f), "Crosswalk");

        // ── CONVERGING FACADES: the corner block instanced 5× per side, staggered
        // + Y-jittered so the two rows converge and never mirror. Window grids on
        // each road-facing face. ──
        for (int i = 0; i < 5; i++)
        {
            float z = 4f + i * 8.28f;
            Facade(root, -8f, z + 0.6f, 90, i);     // left, road-facing +X
            Facade(root, 8f, z, -90, i);            // right, road-facing -X
        }

        // ── VERTICALS + CANOPY: procedural utility poles + sodium street lamps,
        // and the overhead wire web strung between the pole tops. ──
        var tops = new List<Vector3>();
        float[] pz = { -1f, 8f, 18f, 28f, 40f };
        for (int i = 0; i < pz.Length; i++) tops.Add(UtilityPole(root, (i % 2 == 0) ? -5.3f : 5.3f, pz[i]));
        foreach (var l in new[] { (4.2f, 2f), (-4.2f, 9f), (4.2f, 16f), (-4.2f, 23f), (4.2f, 30f), (-4.2f, 37f) })
            StreetLamp(root, l.Item1, l.Item2);
        var wireMat = Mat("WireSteel", "#3A3E48", false, null, 0);
        for (int i = 0; i < tops.Count - 1; i++) { Wire(root, tops[i], tops[i + 1], wireMat); Wire(root, tops[i] + Vector3.down * 0.5f, tops[i + 1] + Vector3.down * 0.7f, wireMat); }
        for (int i = 0; i < tops.Count; i++) Wire(root, tops[i], new Vector3(-tops[i].x, tops[i].y - 0.4f, tops[i].z + 3f), wireMat);   // lateral cross-street
        foreach (var c in new[] { (5.6f, 8f), (-5.6f, 15f), (5.6f, 22f), (-5.6f, 30f), (5.6f, 37f), (-5.6f, 42f) })
            P(root, "PRP_H_Cable_Drape_A", new Vector3(c.Item1, 3.6f, c.Item2), 0, "WireSteel");

        // ── IDENTITY SIGNAGE: the UH-001 hero neon (right, high), signboards
        // stepping down both facades, route + stop signs in the left foreground. ──
        P(root, "SM_H_Neon_Sign_UH001_A", new Vector3(4.9f, 3.4f, 8f), -110, "NeonUH");
        foreach (var sg in new[] { (4.7f, 6f, -95f), (4.7f, 20f, -95f), (4.7f, 34f, -95f), (-4.7f, 11f, 95f), (-4.7f, 25f, 95f), (-4.7f, 38f, 95f) })
            P(root, "SM_H_Signboard_Neon_A", new Vector3(sg.Item1, 2.5f, sg.Item2), sg.Item3, "NeonRose");
        P(root, "SM_H_Route_Sign_A", new Vector3(-3.0f, 0.15f, 3.5f), 25, "SteelSign");
        P(root, "SM_H_Stop_Sign_A", new Vector3(-3.4f, 0.15f, 1.0f), 12, "StopRed");

        // ── WEATHER / DRESSING PASS: puddles (foreground + under lamps), grime,
        // curb bollards, trash piles. ──
        foreach (var p in new[] { (-0.8f, 2f, 0f), (0.9f, 4f, 40f), (-1.2f, 9f, 15f), (1.4f, 16f, 0f), (-0.6f, 23f, 30f), (1.0f, 30f, 20f), (-1.5f, 6f, 0f), (0.5f, 12f, 60f), (-0.4f, 37f, 10f), (1.3f, 44f, 0f), (-2.2f, 3f, 0f), (2.4f, 8f, 30f) })
            P(root, "PRP_H_Puddle_Decal_A", new Vector3(p.Item1, 0.02f, p.Item2), p.Item3, "Puddle");
        for (int i = 0; i < 16; i++) P(root, "PRP_H_Grime_Decal_A", new Vector3(Random01(i, -3f, 3f), 0.01f, Random01(i + 5, 0f, 44f)), (i * 61f) % 360f, "Grime");
        foreach (var b in new[] { (3.4f, 1f), (-3.4f, 5f), (3.4f, 9f), (-3.4f, 13f), (3.4f, 17f), (-3.4f, 21f), (3.4f, 25f), (-3.4f, 29f) })
            P(root, "PRP_H_Bollard_Wet_A", new Vector3(b.Item1, 0.15f, b.Item2), 0, "Concrete");
        foreach (var t in new[] { (-5.0f, 7f), (5.0f, 14f), (-4.9f, 24f), (5.1f, 33f) })
            P(root, "PRP_H_Trash_Pile_A", new Vector3(t.Item1, 0.15f, t.Item2), (t.Item2 * 37f) % 360f, "Grime");

        // ── FAR VISTA: distant skyline dissolving into fog ──
        foreach (var d in new[] { (0f, 52f, 5f, 5.5f), (-8f, 55f, 4f, 4.5f), (7f, 56f, 6f, 6.5f), (2f, 58f, 3f, 4f) })
            SkylineCard(root, new Vector3(d.Item1, d.Item3, d.Item2), d.Item4);

        // ── HANDOFF: invisible trigger proxy + the lone placeholder arrival ──
        P(root, "SM_H_Handoff_Volume_Proxy", new Vector3(0, 1.5f, 2f), 0, "Invisible", true);
        Billboard(root, "SPR_Friend", new Vector3(0.2f, 0f, 3.0f), "ArrivalFigure");

        var spawn = new GameObject("Spawn"); spawn.transform.SetParent(root, false); spawn.transform.localPosition = new Vector3(0, 0.1f, 1f);

        // ── COLLISION ──
        Bound(root, "Ground", new Vector3(0, -0.5f, 20f), new Vector3(12f, 1f, 56f));
        Bound(root, "Wall_L", new Vector3(-3.6f, 1.5f, 20f), new Vector3(0.3f, 3f, 56f));
        Bound(root, "Wall_R", new Vector3(3.6f, 1.5f, 20f), new Vector3(0.3f, 3f, 56f));
        Bound(root, "Wall_Near", new Vector3(0, 1.5f, -0.6f), new Vector3(8f, 3f, 0.4f));

        // ── authored camera: low, behind the arrival point, looking DOWN the
        // corridor with telephoto compression ──
        var camGo = new GameObject("SceneCam"); camGo.transform.SetParent(root, false);
        camGo.transform.position = new Vector3(0, 2.3f, -7.5f);
        camGo.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        camGo.AddComponent<SceneCam>().fov = 30f;

        // ── LIGHTING: overcast damp dusk — dim cool key + sodium lamp pools +
        // one breathing rose neon, wine ambient, thick haze. ──
        var keyGo = new GameObject("OvercastKey"); keyGo.transform.SetParent(root, false);
        keyGo.transform.rotation = Quaternion.Euler(42f, -18f, 0f);
        var key = keyGo.AddComponent<Light>(); key.type = LightType.Directional; key.color = Hx("#9FA6C0"); key.intensity = 0.55f; key.shadows = LightShadows.Soft; key.shadowStrength = 0.5f;
        var neonGo = new GameObject("NeonAccent"); neonGo.transform.SetParent(root, false); neonGo.transform.localPosition = new Vector3(4.2f, 3.2f, 8f);
        var neon = neonGo.AddComponent<Light>(); neon.type = LightType.Point; neon.color = Hx("#FF3B6B"); neon.intensity = 1.6f; neon.range = 5f;

        var mood = new GameObject("MOOD");
        var rm = mood.AddComponent<RoomMood>();
        rm.ambient = Hx("#2E2333");
        rm.fog = Hx("#33262F"); rm.fogDensity = 0.020f;
        rm.cameraBackground = Hx("#2E2333");
        var beats = mood.AddComponent<PrologueBeats>();
        beats.sceneId = "handoff"; beats.nextScene = PrologueScenePaths.Street;

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        int n = root.GetComponentsInChildren<MeshRenderer>(true).Length;
        return $"SC_P3_Handoff rebuilt to blueprint: {n} meshes — {5}×2 facade corridor + poles/wires + sodium lamps + UH-001 neon + crosswalk + skyline/fog";
    }

    // a facade block + a lit/dark window grid on its road-facing face
    static void Facade(Transform parent, float x, float z, float yaw, int i)
    {
        var go = P(parent, "SM_H_Street_Corner_Block_A", new Vector3(x, 0, z), yaw, "FacadeEvening", true);
        if (go != null) { var s = go.transform.localScale; s.y = 0.85f + (i % 3) * 0.22f; go.transform.localScale = s; }
        float faceX = x - Mathf.Sign(x) * 3.34f;                  // road-facing face (block half-depth ≈3.24)
        float sign = Mathf.Sign(x);
        for (int gy = 0; gy < 3; gy++)
            for (int gz = 0; gz < 4; gz++)
            {
                bool lit = ((i + gy * 4 + gz) * 7) % 10 < 6;      // ~60% lit
                LitWindow(parent, new Vector3(faceX, 1.4f + gy * 1.5f, z - 3f + gz * 2f), sign < 0 ? -90 : 90, lit);
            }
    }

    static void LitWindow(Transform parent, Vector3 pos, float yaw, bool lit)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = lit ? "Window_Lit" : "Window_Dark";
        Object.DestroyImmediate(q.GetComponent<MeshCollider>());
        q.transform.SetParent(parent, false); q.transform.localPosition = pos; q.transform.localRotation = Quaternion.Euler(0, yaw, 0); q.transform.localScale = new Vector3(0.85f, 1.05f, 1f);
        var m = lit ? Mat("WindowLit", "#8A6A45", true, "#FFB37A", 1.1f) : Mat("WindowDark", "#141418", false, null, 0);
        m.SetFloat("_Cull", 0);
        q.GetComponent<MeshRenderer>().sharedMaterial = m;
    }

    static Vector3 UtilityPole(Transform parent, float x, float z)
    {
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cube); pole.name = "UtilityPole";
        Object.DestroyImmediate(pole.GetComponent<BoxCollider>());
        pole.transform.SetParent(parent, false); pole.transform.localPosition = new Vector3(x, 4f, z); pole.transform.localScale = new Vector3(0.28f, 8f, 0.28f);
        pole.GetComponent<MeshRenderer>().sharedMaterial = Mat("PoleTimber", "#2E2A2E", false, null, 0);
        var arm = GameObject.CreatePrimitive(PrimitiveType.Cube); arm.name = "Crossarm";
        Object.DestroyImmediate(arm.GetComponent<BoxCollider>());
        arm.transform.SetParent(parent, false); arm.transform.localPosition = new Vector3(x, 7.4f, z); arm.transform.localScale = new Vector3(1.8f, 0.12f, 0.12f);
        arm.GetComponent<MeshRenderer>().sharedMaterial = Mat("PoleTimber", "#2E2A2E", false, null, 0);
        return new Vector3(x, 7.4f, z);
    }

    static void StreetLamp(Transform parent, float x, float z)
    {
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cube); pole.name = "LampPole";
        Object.DestroyImmediate(pole.GetComponent<BoxCollider>());
        pole.transform.SetParent(parent, false); pole.transform.localPosition = new Vector3(x, 2.6f, z); pole.transform.localScale = new Vector3(0.12f, 5.2f, 0.12f);
        pole.GetComponent<MeshRenderer>().sharedMaterial = Mat("SteelSign", "#414A55", false, null, 0);
        float arm = x < 0 ? 1 : -1;
        var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere); bulb.name = "LampBulb";
        Object.DestroyImmediate(bulb.GetComponent<SphereCollider>());
        bulb.transform.SetParent(parent, false); bulb.transform.localPosition = new Vector3(x + arm * 0.55f, 5.0f, z); bulb.transform.localScale = Vector3.one * 0.28f;
        bulb.GetComponent<MeshRenderer>().sharedMaterial = Mat("LampBulb", "#FFDCA8", true, "#FFC98A", 2.6f);
        var lg = new GameObject("LampLight"); lg.transform.SetParent(parent, false); lg.transform.localPosition = new Vector3(x + arm * 0.55f, 4.9f, z);
        var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = Hx("#FFB37A"); l.intensity = 3.4f; l.range = 9f; l.shadows = LightShadows.Soft;
    }

    static void Wire(Transform parent, Vector3 a, Vector3 b, Material mat)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{kKit}/SM_H_Wire_Span_10m.obj");
        if (src == null) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src); go.name = "Wire"; go.transform.SetParent(parent, false);
        go.transform.localPosition = (a + b) * 0.5f; go.transform.localRotation = Quaternion.FromToRotation(Vector3.right, (b - a).normalized);
        go.transform.localScale = new Vector3(Vector3.Distance(a, b) / 10f, 1f, 1f);
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = mat;
        foreach (var mc in go.GetComponentsInChildren<MeshCollider>()) Object.DestroyImmediate(mc);
    }

    static void SkylineCard(Transform parent, Vector3 centre, float h)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Cube); b.name = "Skyline";
        Object.DestroyImmediate(b.GetComponent<BoxCollider>());
        b.transform.SetParent(parent, false); b.transform.localPosition = centre; b.transform.localScale = new Vector3(9f, h * 2f, 0.5f);
        b.GetComponent<MeshRenderer>().sharedMaterial = Mat("Skyline", "#33262F", false, null, 0);
    }

    // ── helpers ──
    static Color Hx(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
    static float Random01(int seed, float lo, float hi) { float f = Mathf.Abs(Mathf.Sin(seed * 12.9898f) * 43758.5453f); f -= Mathf.Floor(f); return Mathf.Lerp(lo, hi, f); }

    static void Box(Transform parent, string name, Vector3 pos, Vector3 size, string matKey)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Cube); b.name = name;
        Object.DestroyImmediate(b.GetComponent<BoxCollider>());
        b.transform.SetParent(parent, false); b.transform.localPosition = pos; b.transform.localScale = size;
        b.GetComponent<MeshRenderer>().sharedMaterial = MatKey(matKey);
    }

    static GameObject P(Transform parent, string mesh, Vector3 pos, float yaw, string matKey, bool invisibleOrSolid = false)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{kKit}/{mesh}.obj");
        if (src == null) { Debug.LogWarning($"[Handoff] missing {mesh}"); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
        go.name = mesh; go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        var mat = MatKey(matKey);
        Bounds bb = new Bounds(); bool first = true;
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
        {
            if (matKey == "Invisible") { r.enabled = false; continue; }
            var mats = new Material[r.sharedMaterials.Length];
            for (int k = 0; k < mats.Length; k++) mats[k] = mat;
            r.sharedMaterials = mats;
            var mf = r.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) { if (first) { bb = mf.sharedMesh.bounds; first = false; } else bb.Encapsulate(mf.sharedMesh.bounds); }
        }
        foreach (var mc in go.GetComponentsInChildren<MeshCollider>()) Object.DestroyImmediate(mc);
        if (invisibleOrSolid && matKey == "FacadeEvening" && !first) { var bc = go.AddComponent<BoxCollider>(); bc.center = bb.center; bc.size = bb.size; }
        return go;
    }

    static void Bound(Transform parent, string name, Vector3 pos, Vector3 size)
    { var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.AddComponent<BoxCollider>().size = size; }

    static void Billboard(Transform parent, string sprite, Vector3 pos, string name)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{kSprDir}/{sprite}.png");
        var matPath = $"{kSprDir}/M_{sprite}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
        mat.SetTexture("_BaseMap", tex); mat.SetFloat("_Smoothness", 0f); mat.SetFloat("_AlphaClip", 1f); mat.SetFloat("_Cutoff", 0.5f); mat.EnableKeyword("_ALPHATEST_ON"); EditorUtility.SetDirty(mat);
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.name = "Sprite";
        Object.DestroyImmediate(quad.GetComponent<MeshCollider>());
        quad.transform.SetParent(go.transform, false); quad.transform.localScale = new Vector3(1f, 1.6875f, 1f); quad.transform.localPosition = new Vector3(0, 1.6875f * 0.5f, 0);
        quad.GetComponent<MeshRenderer>().sharedMaterial = mat; quad.AddComponent<BillboardY>();
    }

    // ── materials ──
    static Material Get(string key, Shader shader)
    {
        var path = $"{kMatDir}/M_H_{key}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { System.IO.Directory.CreateDirectory(kMatDir); m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        if (m.shader != shader) m.shader = shader;
        return m;
    }
    static Material Mat(string key, string hex, bool emiss, string ehex, float k)
    {
        var m = Get(key, Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", Hx(hex)); m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Smoothness", key.Contains("Asphalt") ? 0.45f : key.Contains("Puddle") ? 0.5f : 0.08f);
        if (emiss) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Hx(ehex) * k); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
        EditorUtility.SetDirty(m); return m;
    }
    static Material MatKey(string key)
    {
        switch (key)
        {
            case "AsphaltWet": return Mat("AsphaltWet", "#2A2A33", false, null, 0);
            case "Concrete": return Mat("Concrete", "#6E6A72", false, null, 0);
            case "Crosswalk": return Mat("Crosswalk", "#C9C4C2", false, null, 0);
            case "FacadeEvening": return Mat("FacadeEvening", "#3E2028", false, null, 0);
            case "NeonUH": return Mat("NeonUH", "#5E2036", true, "#FF3B6B", 2.6f);
            case "NeonRose": return Mat("NeonRose", "#33202A", true, "#FF6A8B", 2.0f);
            case "SteelSign": return Mat("SteelSign", "#6E7A85", false, null, 0);
            case "WireSteel": return Mat("WireSteel", "#3A3E48", false, null, 0);
            case "StopRed": return Mat("StopRed", "#A01842", false, null, 0);
            case "Puddle": return Mat("Puddle", "#242630", false, null, 0);
            case "Grime": return Mat("Grime", "#201A1E", false, null, 0);
            case "Skyline": return Mat("Skyline", "#33262F", false, null, 0);
            case "Invisible": return Mat("Concrete", "#6E6A72", false, null, 0);
            default: return Mat("Grey", "#4A464C", false, null, 0);
        }
    }
}
