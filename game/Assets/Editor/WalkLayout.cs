// UN-HUMANITY — SC_P1_Walk, rebuilt to the SC_P1_WALK master blueprint
// (/Lattest). A narrow Japanese residential side street at ~07:20 morning,
// walked ~85 m toward a distant school gate: continuous two-storey facades both
// sides (gardens left, a busy shop cluster right), a wire web on procedural
// utility poles overhead, wet reflective road, and a warm sun raking down the
// street so the far end dissolves into haze. Follow-camera scene (no SceneCam).
//
// Kit = SC_P1_WALK (35 floor-pivot OBJ meshes) + procedural poles / wires /
// vending / skyline. Cast = placeholder billboards (SPUM off). Re-runnable.
//   unity command eval "return WalkLayout.Build();"

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class WalkLayout
{
    const string kScene = "Assets/Scenes/SC_P1_Walk.unity";
    const string kKit = "Assets/Art/Prologue/SC_P1_WALK";
    const string kMatDir = "Assets/Art/Prologue/Materials";
    const string kSprDir = "Assets/Art/Sprites";

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "WALK" || g.name == "MOOD"))
            Object.DestroyImmediate(g);
        var root = new GameObject("WALK").transform;

        // ── GROUND: road down the middle (top→Y=0), raised sidewalks + curbs
        // both sides, crosswalks, manholes + a wet-puddle field. ──
        for (int i = 0; i < 15; i++) P(root, "SM_P1_Road_Asphalt_4m", new Vector3(0, -0.05f, 3f + i * 6f), 0);   // Z 0..90
        for (int j = 0; j < 49; j++)
        {
            float z = 0.9f + j * 1.8f;                                              // Z 0..88, no gaps
            P(root, "SM_P1_Sidewalk_Tile_2m", new Vector3(-3.0f, 0, z), 0);         // top → 0.16 (raised)
            P(root, "SM_P1_Sidewalk_Tile_2m", new Vector3(3.0f, 0, z), 0);
            P(root, "SM_P1_Sidewalk_Tile_2m", new Vector3(-5.0f, 0, z), 0);         // 2nd row fills out to the facades (was ~35% too narrow)
            P(root, "SM_P1_Sidewalk_Tile_2m", new Vector3(5.0f, 0, z), 0);
        }
        for (int j = 0; j < 44; j++)
        {
            float z = 1f + j * 2f;
            P(root, "SM_P1_Curb_Straight_2m", new Vector3(-2.05f, 0, z), 90);       // road-edge trim
            P(root, "SM_P1_Curb_Straight_2m", new Vector3(2.05f, 0, z), 90);
        }
        P(root, "SM_P1_Crosswalk_Deck_A", new Vector3(0, 0.02f, 40f), 0);           // WP04 crossing
        P(root, "SM_P1_Crosswalk_Deck_A", new Vector3(0, 0.02f, 83f), 0);           // gate approach
        foreach (float z in new[] { 7f, 30f, 60f }) P(root, "SM_P1_Manhole_A", new Vector3(0.7f, 0.03f, z), 0);
        foreach (var p in new[] { (-0.8f, 8f, 0f), (0.9f, 20f, 30f), (-0.6f, 38f, 15f), (1.0f, 55f, 40f), (-0.9f, 70f, 0f), (0.5f, 44f, 20f) })
            P(root, "PRP_P1_Puddle_Decal_A", new Vector3(p.Item1, 0.02f, p.Item2), p.Item3);

        // ── FACADES (continuous both sides): left = residential + gardens,
        // right = residential with a shop cluster mid-street. Facade width 4.04
        // runs along Z (yaw ±90); ground storey unbroken, second storey + roofs
        // step so the roofline isn't flat. ──
        int panels = 20;                                                            // 20 × 4.04 ≈ 80 m
        for (int i = 0; i < panels; i++)
        {
            float z = 2.0f + i * 4.04f;
            // LEFT facade pushed out to -6.0 (was -4.3, which made the walls loom)
            Facade(root, -6.0f, z, +1, i);
            // RIGHT — shop cluster at i==13,14 (Z≈54-58)
            if (i == 13 || i == 14) ShopFront(root, z, i == 13);
            else Facade(root, 6.0f, z, -1, i);
        }
        // near-left garden edge (fence + low concrete wall + hedges), Z 0..30
        for (int j = 0; j < 8; j++)
        {
            float z = 2f + j * 3.6f;
            P(root, "SM_P1_Fence_Panel_2m", new Vector3(-5.7f, 0.16f, z), 90);
            if (j % 2 == 0) P(root, "SM_P1_Concrete_Wall_2m", new Vector3(-5.9f, 0, z + 1f), 90);
        }

        // ── DEPTH: procedural utility poles alternating sides + a wire web. ──
        var poleTops = new List<Vector3>();
        float[] pz = { 6f, 18f, 30f, 42f, 54f, 66f, 78f };
        for (int i = 0; i < pz.Length; i++)
        {
            float x = (i % 2 == 0) ? -5.3f : 5.3f;
            poleTops.Add(UtilityPole(root, x, pz[i]));
        }
        var wireMat = FlatMat("Wire", "#1E1F2A", false, null, 0);
        for (int i = 0; i < poleTops.Count - 1; i++)                                // a few gentle high spans, not a net
            Wire(root, poleTops[i], poleTops[i + 1], wireMat, 0.15f);

        // street lamps (present but OFF in the morning) between the poles
        foreach (var l in new[] { (-5.0f, 12f), (5.0f, 24f), (-5.0f, 48f), (5.0f, 72f) })
            StreetLamp(root, l.Item1, l.Item2);

        // ── TREES overhanging toward the road (the concept's leafy framing) ──
        foreach (var t in new[] { (-5.2f, 10f), (-5.3f, 34f), (5.2f, 26f), (-5.2f, 60f), (5.3f, 74f) })
            P(root, "SM_P1_Tree_Medium_A", new Vector3(t.Item1, 0, t.Item2), 0);
        foreach (float z in new[] { 6f, 22f, 44f, 66f, 80f })
        {
            P(root, "SM_P1_Bush_Planter_A", new Vector3(-4.6f, 0.16f, z), 0);
            P(root, "SM_P1_Bush_Planter_A", new Vector3(4.6f, 0.16f, z + 3f), 0);
        }

        // ── STREET PROPS (~40, all off the 4 m walked path, |X|>1.9) ──
        P(root, "PRP_P1_Bicycle_A", new Vector3(-1.9f, 0.16f, 6f), 15);
        P(root, "PRP_P1_Bicycle_A", new Vector3(2.4f, 0.16f, 50f), -30);
        P(root, "PRP_P1_Mailbox_A", new Vector3(1.9f, 0.16f, 4f), -90);
        P(root, "PRP_P1_Traffic_Mirror_A", new Vector3(-2.2f, 0.16f, 40f), 30);
        P(root, "PRP_P1_Road_Sign_School_A", new Vector3(2.2f, 0.16f, 41f), -20);
        P(root, "PRP_P1_Road_Sign_School_A", new Vector3(-2.2f, 0.16f, 80f), 20);
        foreach (float z in new[] { 55f, 57f }) P(root, "PRP_P1_Trash_Bag_A", new Vector3(2.35f, 0.16f, z), 0);
        foreach (float z in new[] { 54f, 56f }) P(root, "PRP_P1_Crate_Stack_A", new Vector3(2.4f, 0.16f, z), 25);
        foreach (var s in new[] { (2.0f, 55f, 0f), (2.0f, 58f, 0f), (-2.0f, 30f, 0f) }) P(root, "PRP_P1_Signboard_Vert_A", new Vector3(s.Item1, 1.1f, s.Item2), s.Item3);
        P(root, "PRP_P1_Signboard_Horz_A", new Vector3(-2.0f, 1.3f, 18f), 90);
        foreach (var a in new[] { (5.85f, 2.4f, 56f, -90f), (-5.85f, 2.5f, 30f, 90f), (5.85f, 2.6f, 20f, -90f), (-5.85f, 2.3f, 62f, 90f) })
            P(root, "PRP_P1_AC_Unit_A", new Vector3(a.Item1, a.Item2, a.Item3), a.Item4);
        foreach (var pp in new[] { (2.3f, 12f), (-2.3f, 48f), (2.3f, 70f), (-2.3f, 16f) }) P(root, "PRP_P1_Planter_Pot_A", new Vector3(pp.Item1, 0.16f, pp.Item2), 0);
        P(root, "PRP_P1_Bollard_Kit_A", new Vector3(-1.9f, 0, 40f), 0);
        P(root, "PRP_P1_Bollard_Kit_A", new Vector3(1.9f, 0, 40f), 0);
        foreach (var po in new[] { (-5.86f, 1.6f, 26f, 90f), (5.86f, 1.7f, 62f, -90f) }) P(root, "PRP_P1_Poster_Card_A", new Vector3(po.Item1, po.Item2, po.Item3), po.Item4);

        // ── FAR END: the school gate at the destination + distant cards melting
        // into the warm haze (staggered, fog-tinted, far past the gate). ──
        P(root, "SM_P1_School_Gate_A", new Vector3(0, 0, 85f), 0);
        foreach (var d in new[] { (0f, 98f, 0f), (-14f, 108f, 12f), (13f, 106f, -12f), (-6f, 120f, 4f), (8f, 124f, -6f) })
            DistantCard(root, new Vector3(d.Item1, 0, d.Item2), d.Item3);

        // ── CAST: the friend billboard + a few BG pedestrians (protagonist is the
        // persistent rig). ──
        Billboard(root, "SPR_Friend", new Vector3(-0.5f, 0f, 5f), "NPC_Friend");
        foreach (var bg in new[] { (1.4f, 26f), (-1.5f, 45f), (1.3f, 62f), (-1.2f, 72f), (1.5f, 34f) })
            Billboard(root, "SPR_Friend", new Vector3(bg.Item1, 0f, bg.Item2), "NPC_BG");

        // markers the flow needs
        Marker(root, "Spawn", new Vector3(0f, 0.1f, 2f));
        Marker(root, "GateReach", new Vector3(0f, 0.1f, 83f));

        // ── COLLISION: ground + keep the player on the 4 m road ──
        Bound(root, "Ground", new Vector3(0, -0.5f, 44f), new Vector3(5f, 1f, 92f));
        Bound(root, "Wall_L", new Vector3(-2.15f, 1.5f, 44f), new Vector3(0.3f, 3f, 92f));
        Bound(root, "Wall_R", new Vector3(2.15f, 1.5f, 44f), new Vector3(0.3f, 3f, 92f));
        Bound(root, "Wall_Start", new Vector3(0, 1.5f, -0.4f), new Vector3(4.6f, 3f, 0.4f));
        Bound(root, "Wall_End", new Vector3(0, 1.5f, 86f), new Vector3(4.6f, 3f, 0.4f));

        // ── LIGHTING: bright warm morning. Directional sun rakes DOWN the street
        // from the far (+Z) end toward camera → subjects backlit, long soft
        // shadows toward the viewer. Local cool/warm emitters. ──
        var sunGo = new GameObject("MorningSun"); sunGo.transform.SetParent(root, false);
        sunGo.transform.rotation = Quaternion.Euler(18f, 182f, 0f);
        var sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional;
        sun.color = Hx("#FFE7C4"); sun.intensity = 1.35f; sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.85f;   // long raking shadows = the scene's main read

        // ── mood (warm morning haze) + beats ──
        var mood = new GameObject("MOOD");
        var rm = mood.AddComponent<RoomMood>();
        rm.ambient = Hx("#8A7668");                          // dimmer → restores the shade-vs-light value range (bright flat ambient was flattening it)
        rm.fog = Hx("#F1DFC0"); rm.fogDensity = 0.007f;
        rm.cameraBackground = Hx("#F1DFC0");                 // warm glow at the vanishing point (was washing to white)
        var beats = mood.AddComponent<PrologueBeats>();
        beats.sceneId = "walk"; beats.nextScene = PrologueScenePaths.School;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        int n = root.GetComponentsInChildren<MeshRenderer>().Length;
        return $"SC_P1_Walk rebuilt to blueprint: {n} meshes (~85 m street) + poles/wires + shop cluster + warm sun + haze";
    }

    // ── a two-storey residential facade panel (wall + roof + windows + door) ──
    static void Facade(Transform p, float x, float z, int face, int i)
    {
        float yaw = face > 0 ? 90 : -90;
        float inX = x - face * 0.13f;                         // road-facing inner face
        P(p, "SM_P1_House_Wall_A", new Vector3(x, 0, z), yaw);
        if (i % 3 != 1) { P(p, "SM_P1_House_Wall_A", new Vector3(x, 3.0f, z), yaw); P(p, "SM_P1_House_Roof_Tiled_A", new Vector3(x - face * 0.3f, 6.0f, z), yaw); }
        else P(p, "SM_P1_House_Roof_Tiled_A", new Vector3(x - face * 0.3f, 3.0f, z), yaw);
        P(p, "SM_P1_House_Window_Grid_A", new Vector3(inX, 1.3f, z - 0.9f), yaw);
        P(p, "SM_P1_House_Window_Grid_A", new Vector3(inX, 1.3f, z + 0.9f), yaw);
        if (i % 3 != 1) { P(p, "SM_P1_House_Window_Grid_A", new Vector3(inX, 4.3f, z - 0.9f), yaw); P(p, "SM_P1_House_Window_Grid_A", new Vector3(inX, 4.3f, z + 0.9f), yaw); }
        if (i % 3 == 0) P(p, "SM_P1_House_Door_A", new Vector3(inX, 0, z + 1.5f), yaw);
    }

    static void ShopFront(Transform p, float z, bool withVending)
    {
        P(p, "SM_P1_Storefront_Hardware_A", new Vector3(6.0f, 0, z), -90);
        P(p, "SM_P1_Storefront_Awning_A", new Vector3(5.0f, 2.5f, z), -90);
        P(p, "SM_P1_House_Wall_A", new Vector3(6.0f, 3.4f, z), -90);      // upper storey over the shop
        if (withVending) Vending(p, 5.2f, z - 1.4f);
    }

    // ── procedural builds (no kit mesh fits) ──
    static Vector3 UtilityPole(Transform parent, float x, float z)
    {
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cube); pole.name = "UtilityPole";
        Object.DestroyImmediate(pole.GetComponent<BoxCollider>());
        pole.transform.SetParent(parent, false); pole.transform.localPosition = new Vector3(x, 4.0f, z); pole.transform.localScale = new Vector3(0.26f, 8.0f, 0.26f);
        pole.GetComponent<MeshRenderer>().sharedMaterial = FlatMat("Pole", "#5A5A60", false, null, 0);
        P(parent, "SM_P1_Pole_Crossarm_A", new Vector3(x, 6.9f, z), 0);
        P(parent, "SM_P1_Pole_Crossarm_A", new Vector3(x, 6.1f, z), 0);
        return new Vector3(x, 6.9f, z);
    }

    static void Wire(Transform parent, Vector3 a, Vector3 b, Material mat, float sag)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{kKit}/SM_P1_Wire_Span_8m.obj");
        if (src == null) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src); go.name = "Wire"; go.transform.SetParent(parent, false);
        Vector3 mid = (a + b) * 0.5f + Vector3.down * sag;
        go.transform.localPosition = mid;
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.right, (b - a).normalized);
        go.transform.localScale = new Vector3(Vector3.Distance(a, b) / 8f, 1f, 1f);
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = mat;
        foreach (var mc in go.GetComponentsInChildren<MeshCollider>()) Object.DestroyImmediate(mc);
    }

    static void StreetLamp(Transform parent, float x, float z)
    {
        var col = GameObject.CreatePrimitive(PrimitiveType.Cube); col.name = "StreetLamp";
        Object.DestroyImmediate(col.GetComponent<BoxCollider>());
        col.transform.SetParent(parent, false); col.transform.localPosition = new Vector3(x, 2.5f, z); col.transform.localScale = new Vector3(0.14f, 5.0f, 0.14f);
        col.GetComponent<MeshRenderer>().sharedMaterial = FlatMat("Lamp", "#414A55", false, null, 0);
        float armDir = x < 0 ? 1 : -1;
        var head = GameObject.CreatePrimitive(PrimitiveType.Cube); head.name = "LampHead";
        Object.DestroyImmediate(head.GetComponent<BoxCollider>());
        head.transform.SetParent(parent, false); head.transform.localPosition = new Vector3(x + armDir * 0.45f, 4.9f, z); head.transform.localScale = new Vector3(0.5f, 0.22f, 0.32f);
        head.GetComponent<MeshRenderer>().sharedMaterial = FlatMat("LampHead", "#2A2A30", false, null, 0);
    }

    static void Vending(Transform parent, float x, float z)
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube); body.name = "VendingMachine";
        Object.DestroyImmediate(body.GetComponent<BoxCollider>());
        body.transform.SetParent(parent, false); body.transform.localPosition = new Vector3(x, 0.965f, z); body.transform.localScale = new Vector3(0.99f, 1.93f, 0.82f);
        body.GetComponent<MeshRenderer>().sharedMaterial = FlatMat("VendBody", "#2A2C33", false, null, 0);
        var face = GameObject.CreatePrimitive(PrimitiveType.Quad); face.name = "VendGlow";
        Object.DestroyImmediate(face.GetComponent<MeshCollider>());
        face.transform.SetParent(parent, false); face.transform.localPosition = new Vector3(x - 0.42f, 1.0f, z); face.transform.localRotation = Quaternion.Euler(0, -90f, 0); face.transform.localScale = new Vector3(0.72f, 1.5f, 1f);
        face.GetComponent<MeshRenderer>().sharedMaterial = FlatMat("VendFace", "#1A2A30", true, "#BFE3EC", 3f);
        var lg = new GameObject("VendLight"); lg.transform.SetParent(parent, false); lg.transform.localPosition = new Vector3(x - 0.9f, 1.1f, z);
        var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = Hx("#BFE3EC"); l.intensity = 2.2f; l.range = 3.2f;
    }

    static void DistantCard(Transform parent, Vector3 pos, float yaw)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{kKit}/SM_P1_Distant_Building_Card.obj");
        if (src == null) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src); go.name = "DistantCard"; go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        var m = GetMat("Distant", Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", Hx("#C7B6A2")); EditorUtility.SetDirty(m);
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = m;
        foreach (var mc in go.GetComponentsInChildren<MeshCollider>()) Object.DestroyImmediate(mc);
    }

    // ── kit-mesh placement ──
    static void P(Transform parent, string mesh, Vector3 pos, float yaw)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{kKit}/{mesh}.obj");
        if (src == null) { Debug.LogWarning($"[Walk] missing {mesh}"); return; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
        go.name = mesh; go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        var mat = MatFor(mesh);
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
        {
            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
        foreach (var mc in go.GetComponentsInChildren<MeshCollider>()) Object.DestroyImmediate(mc);
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
        mat.SetTexture("_BaseMap", tex); mat.SetFloat("_Smoothness", 0f);
        mat.SetFloat("_AlphaClip", 1f); mat.SetFloat("_Cutoff", 0.5f); mat.EnableKeyword("_ALPHATEST_ON");
        EditorUtility.SetDirty(mat);
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.name = "Sprite";
        Object.DestroyImmediate(quad.GetComponent<MeshCollider>());
        quad.transform.SetParent(go.transform, false);
        quad.transform.localScale = new Vector3(1f, 1.6875f, 1f);
        quad.transform.localPosition = new Vector3(0, 1.6875f * 0.5f, 0);
        quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
        quad.AddComponent<BillboardY>();
    }

    // ── materials (desaturated albedo; light does the warming) ──
    static Color Hx(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

    static Material GetMat(string key, Shader shader)
    {
        var path = $"{kMatDir}/M_P1_{key}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { System.IO.Directory.CreateDirectory(kMatDir); m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        if (m.shader != shader) m.shader = shader;
        return m;
    }

    static Material FlatMat(string key, string hex, bool emiss, string ehex, float k)
    {
        var m = GetMat(key, Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", Hx(hex)); m.SetFloat("_Smoothness", 0.05f); m.SetFloat("_Metallic", 0f);
        if (emiss) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Hx(ehex) * k); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
        EditorUtility.SetDirty(m); return m;
    }

    static Material MatFor(string mesh)
    {
        if (mesh.Contains("Road") || mesh.Contains("Manhole")) { var m = FlatMat("AsphaltWet", "#26232A", false, null, 0); m.SetFloat("_Smoothness", 0.35f); return m; }
        if (mesh.Contains("Crosswalk")) return FlatMat("Crosswalk", "#B7ADB0", false, null, 0);
        if (mesh.Contains("Sidewalk") || mesh.Contains("Curb")) return FlatMat("Concrete", "#9E948C", false, null, 0);   // warm grey pavement (not mauve)
        if (mesh.Contains("Puddle")) { var m = FlatMat("Puddle", "#2A3038", false, null, 0); m.SetFloat("_Smoothness", 0.5f); return m; }
        if (mesh.Contains("Roof")) return FlatMat("RoofTile", "#3E3A46", false, null, 0);
        if (mesh.Contains("House_Wall") || mesh.Contains("Concrete_Wall")) return FlatMat("Plaster", "#C9B7A2", false, null, 0);
        if (mesh.Contains("Storefront_Awning")) return FlatMat("Awning", "#6E4A50", false, null, 0);
        if (mesh.Contains("Storefront")) return FlatMat("Storefront", "#6E5A48", false, null, 0);
        if (mesh.Contains("Window")) return FlatMat("WinWarm", "#3A342E", true, "#FFDCA8", 0.8f);   // warm occupied interior
        if (mesh.Contains("Gate")) return FlatMat("Gate", "#4A4A52", false, null, 0);
        if (mesh.Contains("Tree") || mesh.Contains("Bush")) return FlatMat("Foliage", "#5A6B3E", false, null, 0);
        if (mesh.Contains("Fence") || mesh.Contains("Pole") || mesh.Contains("Crossarm") || mesh.Contains("Bollard")) return FlatMat("MetalFence", "#6E7A85", false, null, 0);
        if (mesh.Contains("Signboard") || mesh.Contains("Sign")) return FlatMat("Sign", "#6E4A50", true, "#E5AFBA", 0.9f);   // warm signage, calm morning glow
        if (mesh.Contains("AC_") || mesh.Contains("Mirror") || mesh.Contains("Mailbox")) return FlatMat("Metal", "#6E7A85", false, null, 0);
        if (mesh.Contains("Door")) return FlatMat("DoorWood", "#5A4A3A", false, null, 0);
        if (mesh.Contains("Poster")) return FlatMat("Poster", "#9A8A80", false, null, 0);
        return FlatMat("Prop", "#5A544E", false, null, 0);
    }
}
