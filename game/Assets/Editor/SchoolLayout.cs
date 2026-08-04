// UN-HUMANITY — SC_P2_School, rebuilt to the SC_P2_SCHOOL master blueprint
// (/Lattest). An ordinary, slightly gloomy 2F-East corridor: a long one-point
// hall, an 8-bay window wall pouring cool daylight across satin tile, classroom
// doors + lockers opposite, dense overhead clutter, receding ~14 m to an HONEST
// sealed wall. It must read mundane so the ANOMALY — the terminus bursting into
// a crimson CRACK while colour/sound drain to ink — lands as a violation.
//
// The anomaly is one INACTIVE "SchoolAnomaly" root staged IN FRONT of the sealed
// wall; PrologueFlow.CrackLerp activates it and drains every non-anomaly light.
// Kit = SC_P2_SCHOOL (37 meshes, incl. ANM_ set). Cast = placeholder billboard.
//   unity command eval "return SchoolLayout.Build();"

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class SchoolLayout
{
    const string kScene = "Assets/Scenes/SC_P2_School.unity";
    const string kKit = "Assets/Art/Prologue/SC_P2_SCHOOL";
    const string kMatDir = "Assets/Art/Prologue/Materials";
    const string kSprDir = "Assets/Art/Sprites";

    const float WallX = 1.285f, InnerX = 1.21f, SealZ = 14f;
    static readonly float[] Cells = { 1f, 3f, 5f, 7f, 9f, 11f, 13f };   // 7 cells, Z 0→14

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "SCHOOL" || g.name == "MOOD"))
            Object.DestroyImmediate(g);
        var root = new GameObject("SCHOOL").transform;

        // ── SHELL: floor / ceiling / walls / baseboards tiled across 7 cells
        // (floor top → Y=0 at pos Y=-0.11). ──
        foreach (float z in Cells)
        {
            P(root, "SM_P2_Hallway_Floor_2m", new Vector3(0, -0.11f, z), 90);
            P(root, "SM_P2_Hallway_Ceiling_2m", new Vector3(0, 3.10f, z), 90);
            P(root, "SM_P2_Hallway_Wall_2m", new Vector3(-WallX, 0, z), 90);
            P(root, "SM_P2_Hallway_Wall_2m", new Vector3(WallX, 0, z), 90);
            P(root, "SM_P2_Baseboard_2m", new Vector3(-InnerX, 0, z), 90);
            P(root, "SM_P2_Baseboard_2m", new Vector3(InnerX, 0, z), 90);
        }
        // vestibule + entrance cap wall behind the camera
        P(root, "SM_P2_Hallway_Floor_2m", new Vector3(0, -0.11f, -1f), 90);
        P(root, "SM_P2_Hallway_Ceiling_2m", new Vector3(0, 3.10f, -1f), 90);

        // ── LEFT WALL = the daylight window wall (7 bays) + sky backdrop ──
        foreach (float z in Cells)
            P(root, "SM_P2_Window_Bay_A", new Vector3(-InnerX, 0.90f, z), 90);
        SkyBackdrop(root);
        // ── RIGHT WALL = classroom doors + signs + a clinic door ──
        P(root, "SM_P2_Door_Clinic_A", new Vector3(InnerX, 0, 1f), -90);
        foreach (float z in new[] { 3f, 5f, 7f, 9f, 11f, 13f })
        {
            P(root, "SM_P2_Door_Classroom_A", new Vector3(InnerX - 0.05f, 0, z), -90);
            P(root, "SM_P2_Classroom_Sign_A", new Vector3(InnerX - 0.02f, 2.25f, z), -90);
        }

        // ── OVERHEAD: fluorescent fixtures (+ real drainable lights), vents, pipes ──
        float[] lz = { 1f, 2.4f, 3.8f, 5.2f, 6.6f, 8f, 9.4f, 10.8f, 12.2f, 13.6f };
        foreach (float z in lz)
        {
            P(root, "SM_P2_Service_Light_A", new Vector3(0, 2.95f, z), 0);
            var lg = new GameObject($"Fluoro_{z}"); lg.transform.SetParent(root, false); lg.transform.localPosition = new Vector3(0, 2.85f, z);
            var pl = lg.AddComponent<Light>(); pl.type = LightType.Point; pl.color = Hx("#FFF0D8"); pl.intensity = 0.7f; pl.range = 4.5f;
        }
        foreach (float z in new[] { 2.5f, 5.5f, 7f, 8.5f, 11.5f, 13f })
            P(root, "SM_P2_Ceiling_Vent_A", new Vector3(0.4f, 3.06f, z), 0);
        // pipe runs hugging the wall/ceiling corners (NOT bisecting the light spine)
        foreach (float z in new[] { 3f, 7f, 11f })
        {
            P(root, "SM_P2_Pipe_Utility_A", new Vector3(-2.1f, 2.78f, z), 0);
            P(root, "SM_P2_Pipe_Utility_A", new Vector3(2.1f, 2.78f, z), 0);
        }

        // ── cool morning light shafts raking in from the window wall ──
        foreach (float z in new[] { 2.5f, 6f, 9.5f, 12.5f })
        {
            var sg = new GameObject($"WinShaft_{z}"); sg.transform.SetParent(root, false);
            sg.transform.localPosition = new Vector3(-1.15f, 2.4f, z);
            sg.transform.rotation = Quaternion.Euler(52f, 62f, 0f);
            var sl = sg.AddComponent<Light>(); sl.type = LightType.Spot; sl.color = Hx("#C7D8E4"); sl.intensity = 2.2f; sl.range = 6f; sl.spotAngle = 70f; sl.innerSpotAngle = 30f;
        }

        // ── DRESSING: lockers, cubbies, boards, benches, vending, fire, clutter ──
        foreach (var l in new[] { (InnerX, 4.4f, -90f), (InnerX, 7.8f, -90f), (InnerX, 9.6f, -90f), (InnerX, 12.8f, -90f), (-InnerX, 5.5f, 90f), (-InnerX, 9f, 90f) })
            P(root, "SM_P2_Locker_Bank_A", new Vector3(l.Item1, 0, l.Item2), l.Item3, true);
        P(root, "SM_P2_Shoe_Cubby_A", new Vector3(InnerX, 0, 0.4f), -90, true);
        P(root, "SM_P2_Shoe_Cubby_A", new Vector3(-InnerX, 0, 0.4f), 90, true);
        foreach (var b in new[] { (-InnerX, 3f, 90f), (InnerX, 8f, -90f), (-InnerX, 12f, 90f) })
            P(root, "SM_P2_Notice_Board_A", new Vector3(b.Item1, 1.1f, b.Item2), b.Item3);
        foreach (var bn in new[] { (InnerX, 4f, -90f), (InnerX, 9f, -90f), (InnerX, 12.5f, -90f) })
            P(root, "PRP_P2_Bench_Hall_A", new Vector3(bn.Item1, 0, bn.Item2), bn.Item3, true);
        Vending(root, InnerX - 0.05f, 7f);
        P(root, "PRP_P2_Fire_Cabinet_A", new Vector3(-InnerX, 0, 2f), 90);
        P(root, "PRP_P2_Fire_Cabinet_A", new Vector3(InnerX, 0, 9.5f), -90);
        foreach (float z in new[] { 2.3f, 7.3f, 12f }) P(root, "PRP_P2_Fire_Extinguisher_A", new Vector3(-InnerX + 0.15f, 0, z), 0);
        foreach (var t in new[] { (-0.9f, 2f), (0.9f, 6f), (-0.9f, 10f), (0.8f, 7.4f) }) P(root, "PRP_P2_Trash_Bin_A", new Vector3(t.Item1, 0, t.Item2), 0);
        P(root, "PRP_P2_Bucket_Mop_A", new Vector3(-0.7f, 0, 6f), 0);
        P(root, "PRP_P2_Bucket_Mop_A", new Vector3(0.7f, 0, 12f), 0);
        foreach (var d in new[] { (0.8f, 5f, 90f), (0.85f, 10f, 90f) }) P(root, "PRP_P2_Desk_Stack_A", new Vector3(d.Item1, 0, d.Item2), d.Item3);
        P(root, "PRP_P2_Clock_Wall_A", new Vector3(InnerX, 2.4f, 4f), -90);
        P(root, "PRP_P2_Clock_Wall_A", new Vector3(-InnerX, 2.4f, 11f), 90);
        P(root, "PRP_P2_Umbrella_Stand_A", new Vector3(InnerX - 0.2f, 0, 1f), 0);
        for (int i = 0; i < 14; i++) P(root, "PRP_P2_Flyer_Scrap_A", new Vector3(Random01(i, -0.9f, 0.9f), 0.006f, 2f + i * 0.8f), i * 47f % 360f);
        foreach (float z in new[] { 2f, 4f, 5f, 6f, 8f, 9f, 10f, 11f, 12f, 13f }) P(root, "PRP_P2_Floor_Decal_Scuff_A", new Vector3(0, 0.008f, z), z * 30f % 360f);
        // pre-hint warning tape at the sealed wall
        P(root, "PRP_P2_Warning_Tape_A", new Vector3(0, 1.0f, 13.3f), 0);
        P(root, "PRP_P2_Warning_Tape_A", new Vector3(0, 0.6f, 6f), 0);

        // ── THE HONEST WALL at the terminus (indistinguishable from E02) ──
        P(root, "SM_P2_Sealed_Wall_A", new Vector3(0, 0, SealZ), 0, false, "SealedWall");

        // ── THE ANOMALY — inactive, staged IN FRONT of the sealed wall (Z 11..13.9)
        // so the crack + red void bloom over the terminus when CrackLerp fires ──
        BuildAnomaly(root);

        // ── cast (friend billboard; protagonist is the rig) + spawn ──
        Billboard(root, "SPR_Friend", new Vector3(-0.4f, 0f, 11.0f), "NPC_Friend");
        Marker(root, "Spawn", new Vector3(0.25f, 0.1f, 2.2f));

        // ── collision: seal the corridor box (Z -1.5 .. 14) ──
        Bound(root, "Bound_Ground", new Vector3(0, -0.5f, 6f), new Vector3(2.4f, 1f, 17f));
        Bound(root, "Bound_Wall_L", new Vector3(-1.35f, 1.7f, 6f), new Vector3(0.3f, 3.4f, 17f));
        Bound(root, "Bound_Wall_R", new Vector3(1.35f, 1.7f, 6f), new Vector3(0.3f, 3.4f, 17f));
        Bound(root, "Bound_Near", new Vector3(0, 1.7f, -1.6f), new Vector3(2.6f, 3.4f, 0.3f));
        Bound(root, "Bound_Far", new Vector3(0, 1.7f, 14.2f), new Vector3(2.6f, 3.4f, 0.3f));

        // ── authored camera: centred one-point, crack framed dead-centre ──
        var camGo = new GameObject("SceneCam"); camGo.transform.SetParent(root, false);
        camGo.transform.position = new Vector3(0, 1.90f, -3.2f);
        camGo.transform.rotation = Quaternion.LookRotation(new Vector3(0, 1.15f, 14.0f) - camGo.transform.position, Vector3.up);
        camGo.AddComponent<SceneCam>().fov = 44f;

        // ── cool morning key + mood (ambient MUST match CrackLerp normalAmb) ──
        var keyGo = new GameObject("MorningKey"); keyGo.transform.SetParent(root, false);
        keyGo.transform.rotation = Quaternion.Euler(34f, 62f, 0f);
        var kl = keyGo.AddComponent<Light>(); kl.type = LightType.Directional; kl.color = Hx("#CBD8E8"); kl.intensity = 0.55f; kl.shadows = LightShadows.Soft; kl.shadowStrength = 0.5f;

        var mood = new GameObject("MOOD");
        var rm = mood.AddComponent<RoomMood>();
        rm.ambient = new Color(0.34f, 0.33f, 0.37f);
        rm.fog = Hx("#6E727A"); rm.fogDensity = 0.022f;
        rm.cameraBackground = new Color(0.20f, 0.19f, 0.22f);
        var beats = mood.AddComponent<PrologueBeats>();
        beats.sceneId = "school"; beats.nextScene = PrologueScenePaths.Handoff;

        AssetDatabase.SaveAssets();   // persist material edits so a fresh OpenScene renders them
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        int n = root.GetComponentsInChildren<MeshRenderer>(true).Length;
        return $"SC_P2_School rebuilt to blueprint: {n} meshes + 7-cell hall + window wall + sealed wall + inactive crack + one-point cam";
    }

    // the daylight beyond the window wall (a cool sky card + a dark silhouette
    // so the 8 bays read as a lit world, not black holes)
    static void SkyBackdrop(Transform parent)
    {
        var sky = GameObject.CreatePrimitive(PrimitiveType.Quad); sky.name = "WindowSky";
        Object.DestroyImmediate(sky.GetComponent<MeshCollider>());
        sky.transform.SetParent(parent, false); sky.transform.localPosition = new Vector3(-2.1f, 2.4f, 7f);
        sky.transform.localRotation = Quaternion.Euler(0, 90f, 0); sky.transform.localScale = new Vector3(18f, 5.5f, 1f);
        sky.GetComponent<MeshRenderer>().sharedMaterial = Mat("WinSky", "#AEC0D0", true, "#AEC0D0", 0.12f);
        var sil = GameObject.CreatePrimitive(PrimitiveType.Quad); sil.name = "WindowSilhouette";
        Object.DestroyImmediate(sil.GetComponent<MeshCollider>());
        sil.transform.SetParent(parent, false); sil.transform.localPosition = new Vector3(-2.6f, 1.6f, 7f);
        sil.transform.localRotation = Quaternion.Euler(0, 90f, 0); sil.transform.localScale = new Vector3(16f, 3.5f, 1f);
        sil.GetComponent<MeshRenderer>().sharedMaterial = Mat("WinSil", "#2A2E36", false, null, 0);
    }

    static void Vending(Transform parent, float x, float z)
    {
        P(parent, "PRP_P2_Vending_Hall_A", new Vector3(x, 0, z), -90, true);
        var face = GameObject.CreatePrimitive(PrimitiveType.Quad); face.name = "VendGlow";
        Object.DestroyImmediate(face.GetComponent<MeshCollider>());
        face.transform.SetParent(parent, false); face.transform.localPosition = new Vector3(x - 0.45f, 1.0f, z); face.transform.localRotation = Quaternion.Euler(0, -90f, 0); face.transform.localScale = new Vector3(0.75f, 1.4f, 1f);
        face.GetComponent<MeshRenderer>().sharedMaterial = Mat("VendFace", "#1A2A30", true, "#D2ECF2", 3f);
        var lg = new GameObject("VendLight"); lg.transform.SetParent(parent, false); lg.transform.localPosition = new Vector3(x - 0.9f, 1.1f, z);
        var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = Hx("#D2ECF2"); l.intensity = 1.4f; l.range = 3f;
    }

    // the crack: an INACTIVE root at the terminus, all geometry IN FRONT of the
    // sealed wall (Z<14) so it blooms over it. Crimson light is a CHILD → not
    // drained by CrackLerp. The wine-black void backdrop is the one allowed fade.
    static void BuildAnomaly(Transform root)
    {
        var a = new GameObject("SchoolAnomaly").transform; a.SetParent(root, false); a.localPosition = new Vector3(0, 0, 13.4f);

        // wine-black void the stair climbs into (large emissive backdrop facing camera)
        var glow = GameObject.CreatePrimitive(PrimitiveType.Quad); glow.name = "Void_Glow";
        Object.DestroyImmediate(glow.GetComponent<MeshCollider>());
        glow.transform.SetParent(a, false); glow.transform.localPosition = new Vector3(0, 1.7f, 0.45f);
        glow.transform.localRotation = Quaternion.Euler(0, 180f, 0); glow.transform.localScale = new Vector3(2.4f, 3.4f, 1f);
        glow.GetComponent<MeshRenderer>().sharedMaterial = Anom("Void", "#2A0E16", "#6E1018", 0.6f);   // wine-black, blue killed, kept dark so it doesn't blow to pink

        // stair silhouette climbing into the void (kit flights, in front of the wall)
        PA(a, "ANM_P2_Stairwell_Hidden_A", new Vector3(0, 0, 0.1f), 0, Anom("Stair", "#160A10", "#5E2036", 0.5f));
        // the crack core + shards + distortion cloud
        PA(a, "ANM_P2_Crack_Core_A", new Vector3(0, 0, 0.2f), 0, Anom("Core", "#2A0810", "#C81828", 0.85f));   // just over 1.0 red only → glows crimson, not white
        for (int i = 0; i < 12; i++) PA(a, "ANM_P2_Rift_Shard_A", new Vector3(Random01(i, -1.0f, 1.0f), Random01(i + 5, 0.2f, 2.6f), Random01(i + 9, -0.4f, 0.3f)), (i * 37f) % 50f - 25f, Anom("Shard", "#280E14", "#C81828", 0.9f));
        for (int i = 0; i < 6; i++) PA(a, "ANM_P2_Distortion_Card_A", new Vector3(Random01(i + 2, -1.0f, 1.0f), Random01(i + 7, 0.6f, 2.8f), Random01(i + 3, -2.4f, -0.2f)), (i * 53f) % 60f - 30f, Additive("Distort", "#B83038", 0.3f));   // few + dim (additive stacks to a pink slab otherwise)
        for (int i = 0; i < 10; i++) PA(a, Rand2(i) ? "ANM_P2_Billboard_Graffiti_A" : "ANM_P2_Residue_Decal_A", new Vector3(Rand2(i) ? (i % 2 == 0 ? -1.19f : 1.19f) : Random01(i, -1.0f, 1.0f), Rand2(i) ? Random01(i, 0.6f, 2.2f) : 0.01f, Random01(i + 4, -3.5f, 0.3f)), Rand2(i) ? (i % 2 == 0 ? 90f : -90f) : 0f, Anom("Res", "#1A0810", "#6E1018", 0.6f));

        // the frozen first-year — silhouetted against the void
        var fig = new GameObject("SchoolAnomaly_Figure"); fig.transform.SetParent(a, false); fig.transform.localPosition = new Vector3(0.35f, 0, -0.3f);
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Sprite";
        Object.DestroyImmediate(q.GetComponent<MeshCollider>());
        q.transform.SetParent(fig.transform, false); q.transform.localScale = new Vector3(0.9f, 1.7f, 1f); q.transform.localPosition = new Vector3(0, 0.85f, 0);
        q.GetComponent<MeshRenderer>().sharedMaterial = Anom("Figure", "#0A0508", "#5E2036", 0.7f);
        q.AddComponent<BillboardY>();

        // crimson bleed-light (CHILD of the anomaly → survives the drain)
        var lightGo = new GameObject("Crack_Light"); lightGo.transform.SetParent(a, false); lightGo.transform.localPosition = new Vector3(0, 1.5f, -0.2f);
        var lt = lightGo.AddComponent<Light>(); lt.type = LightType.Point; lt.color = Hx("#C81828"); lt.intensity = 1.2f; lt.range = 5.5f;

        a.gameObject.SetActive(false);
    }

    // ── helpers ──
    static Color Hx(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
    static float Random01(int seed, float lo, float hi) { float f = Mathf.Abs(Mathf.Sin(seed * 12.9898f) * 43758.5453f); f -= Mathf.Floor(f); return Mathf.Lerp(lo, hi, f); }
    static bool Rand2(int i) => (i % 2) == 0;

    static void P(Transform parent, string mesh, Vector3 pos, float yaw, bool solid = false, string name = null)
        => Place(parent, mesh, pos, yaw, MatFor(mesh), solid, name);
    static void PA(Transform parent, string mesh, Vector3 pos, float yaw, Material mat)
        => Place(parent, mesh, pos, yaw, mat, false, null);

    static void Place(Transform parent, string mesh, Vector3 pos, float yaw, Material mat, bool solid, string name)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{kKit}/{mesh}.obj");
        if (src == null) { Debug.LogWarning($"[School] missing {mesh}"); return; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
        go.name = name ?? mesh; go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        Bounds b = new Bounds(); bool first = true;
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
        {
            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
            var mf = r.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) { if (first) { b = mf.sharedMesh.bounds; first = false; } else b.Encapsulate(mf.sharedMesh.bounds); }
        }
        foreach (var mc in go.GetComponentsInChildren<MeshCollider>()) Object.DestroyImmediate(mc);
        if (solid && !first) { var bc = go.AddComponent<BoxCollider>(); bc.center = b.center; bc.size = b.size; }
    }

    static void Bound(Transform parent, string name, Vector3 pos, Vector3 size)
    { var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.AddComponent<BoxCollider>().size = size; }
    static void Marker(Transform p, string n, Vector3 pos)
    { var go = new GameObject(n); go.transform.SetParent(p, false); go.transform.localPosition = pos; }

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
        var path = $"{kMatDir}/M_P2_{key}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { System.IO.Directory.CreateDirectory(kMatDir); m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        if (m.shader != shader) m.shader = shader;
        return m;
    }
    static Material Mat(string key, string hex, bool emiss, string ehex, float k)
    {
        var m = Get(key, Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", Hx(hex)); m.SetFloat("_Smoothness", key.Contains("Floor") ? 0.2f : 0.05f); m.SetFloat("_Metallic", 0f);
        if (emiss) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Hx(ehex) * k); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
        EditorUtility.SetDirty(m); return m;
    }
    static Material Anom(string key, string baseHex, string emHex, float k)
    {
        var m = Get("Anom_" + key, Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", Hx(baseHex)); m.SetFloat("_Smoothness", 0.1f);
        m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Hx(emHex) * k); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(m); return m;
    }
    static Material Additive(string key, string emHex, float k)
    {
        var m = Get("Anom_" + key, Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", Hx(emHex) * k);
        m.SetFloat("_Surface", 1f); m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.One);
        m.SetFloat("_ZWrite", 0f); m.SetOverrideTag("RenderType", "Transparent"); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3100;
        EditorUtility.SetDirty(m); return m;
    }

    static Material MatFor(string mesh)
    {
        if (mesh.Contains("Floor") && !mesh.Contains("Decal")) return Mat("Floor", "#6A7068", false, null, 0);
        if (mesh.Contains("Wall") || mesh.Contains("Ceiling") || mesh.Contains("Sealed")) return Mat("Wall", "#8A827E", false, null, 0);
        if (mesh.Contains("Baseboard")) return Mat("Baseboard", "#2A2622", false, null, 0);
        if (mesh.Contains("Window")) return Mat("Glass", "#8FA8B8", true, "#C7D8E4", 0.25f);   // cool daylight, not a white blowout
        if (mesh.Contains("Locker") || mesh.Contains("Cubby")) return Mat("Locker", "#6E7A85", false, null, 0);
        if (mesh.Contains("Door") || mesh.Contains("Desk")) return Mat("DoorWood", "#5A4A3A", false, null, 0);
        if (mesh.Contains("Service_Light")) return Mat("Fluoro", "#E8E2D2", true, "#FFF0D8", 0.18f);
        if (mesh.Contains("Fire")) return Mat("SafetyRed", "#9A3B32", false, null, 0);
        if (mesh.Contains("Sign") || mesh.Contains("Notice") || mesh.Contains("Flyer") || mesh.Contains("Clock")) return Mat("Paper", "#C8C2BC", false, null, 0);
        if (mesh.Contains("Pipe") || mesh.Contains("Vent") || mesh.Contains("Rail") || mesh.Contains("Stair")) return Mat("Metal", "#70726E", false, null, 0);
        if (mesh.Contains("Warning")) return Mat("WarnTape", "#C8B048", true, "#C8B048", 0.3f);
        if (mesh.Contains("Bench") || mesh.Contains("Umbrella")) return Mat("MetalRust", "#6A544A", false, null, 0);
        if (mesh.Contains("Bucket") || mesh.Contains("Trash") || mesh.Contains("Vending")) return Mat("Plastic", "#5E6A66", false, null, 0);
        return Mat("Grey", "#78747A", false, null, 0);
    }
}
