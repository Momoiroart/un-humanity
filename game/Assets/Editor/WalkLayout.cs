// UN-HUMANITY — SC_P1_Walk, built from the REAL street kit (Prologue Asset
// Kit, 2026-08-04): a narrow residential street the player walks down toward
// the school gate — road, facades both sides, overhead wires, trees, props,
// crosswalk, gate + distant backdrop. Grey-morning mood. Keeps the friend
// billboard + Spawn/GateReach markers the flow needs, with full boundary
// collision (you walk the 4 m road; buildings are beyond the invisible walls).
//   unity command eval "return WalkLayout.Build();"

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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

        // ── road down the middle + sidewalk filling the gap to the buildings ──
        // road slab top → Y=0 (was floating with top at +0.05); a flush
        // sidewalk each side spans the void between the road edge (X±2) and the
        // building fronts (X±3.5) so nothing floats and the street reads as ONE
        // connected surface — the kit's sidewalk/curb pieces, previously unused.
        for (int i = 0; i < 4; i++)
            P(root, "SM_P1_Road_Asphalt_4m", new Vector3(0, -0.05f, 3f + i * 6f), 0);
        for (int i = 0; i < 12; i++)
        {
            float z = 1f + i * 2f;                                   // Z 0..24, no gaps
            P(root, "SM_P1_Sidewalk_Tile_2m", new Vector3(-2.9f, -0.16f, z), 90);   // left  X[2.0,3.8]
            P(root, "SM_P1_Sidewalk_Tile_2m", new Vector3(2.9f, -0.16f, z), 90);    // right
            P(root, "SM_P1_Curb_Straight_2m", new Vector3(-2.02f, -0.05f, z), 90);  // road-edge trim
            P(root, "SM_P1_Curb_Straight_2m", new Vector3(2.02f, -0.05f, z), 90);
        }
        P(root, "SM_P1_Crosswalk_Deck_A", new Vector3(0, 0.02f, 11f), 0);
        P(root, "PRP_P1_Manhole_A", new Vector3(0.7f, 0.03f, 7f), 0);
        P(root, "PRP_P1_Puddle_Decal_A", new Vector3(-0.8f, 0.02f, 16f), 0);
        P(root, "PRP_P1_Puddle_Decal_A", new Vector3(1.3f, 0.02f, 5.2f), 40);   // off the walked path (was mid-road, read as a floating white patch)

        // ── facades down both sides (rotated so width runs along Z) ──
        float[] zRow = { 2.2f, 6.6f, 11f, 15.4f, 19.8f };
        for (int i = 0; i < zRow.Length; i++)
        {
            float z = zRow[i];
            // LEFT side (facing +X toward the street)
            P(root, "SM_P1_House_Wall_A", new Vector3(-3.5f, 0, z), 90, false, i % 3);
            P(root, "SM_P1_House_Roof_Tiled_A", new Vector3(-3.9f, 3.0f, z), 90);
            P(root, "SM_P1_House_Window_Grid_A", new Vector3(-3.28f, 1.5f, z - 0.9f), 90);
            P(root, "SM_P1_House_Window_Grid_A", new Vector3(-3.28f, 1.5f, z + 0.9f), 90);
            // RIGHT side (facing -X)
            if (i == 2)   // a storefront breaks up the row (the corner shop)
            {
                P(root, "SM_P1_Storefront_Hardware_A", new Vector3(3.7f, 0, z), -90);
                P(root, "SM_P1_Storefront_Awning_A", new Vector3(3.05f, 2.4f, z), -90);
                P(root, "PRP_P1_Signboard_Vert_A", new Vector3(2.0f, 1.1f, z - 1.6f), 0);
            }
            else
            {
                P(root, "SM_P1_House_Wall_A", new Vector3(3.5f, 0, z), -90, false, (i + 1) % 3);
                P(root, "SM_P1_House_Roof_Tiled_A", new Vector3(3.9f, 3.0f, z), -90);
                P(root, "SM_P1_House_Window_Grid_A", new Vector3(3.28f, 1.5f, z - 0.9f), -90);
                P(root, "SM_P1_House_Window_Grid_A", new Vector3(3.28f, 1.5f, z + 0.9f), -90);
            }
        }
        // low fence + concrete wall fragments filling the gaps
        P(root, "SM_P1_Concrete_Wall_2m", new Vector3(-2.7f, 0, 4.4f), 90);
        P(root, "SM_P1_Fence_Panel_2m", new Vector3(2.7f, 0, 4.4f), 90);
        P(root, "SM_P1_Fence_Panel_2m", new Vector3(2.7f, 0, 17.6f), 90);

        // ── overhead wires + poles down the right ──
        for (int i = 0; i < 3; i++)
        {
            float z = 4f + i * 8f;
            P(root, "SM_P1_Fence_Post_A", new Vector3(2.55f, 0, z), 0);        // pole trunk
            P(root, "SM_P1_Fence_Post_A", new Vector3(2.55f, 1.33f, z), 0);    // stacked taller
            P(root, "SM_P1_Fence_Post_A", new Vector3(2.55f, 2.66f, z), 0);
            P(root, "SM_P1_Pole_Crossarm_A", new Vector3(2.55f, 3.9f, z), 90);
        }
        P(root, "SM_P1_Wire_Span_8m", new Vector3(2.55f, 3.85f, 8f), 0);
        P(root, "SM_P1_Wire_Span_8m", new Vector3(2.55f, 3.85f, 16f), 0);

        // ── trees + greenery ──
        P(root, "SM_P1_Tree_Medium_A", new Vector3(-2.6f, 0, 9f), 0);
        P(root, "SM_P1_Tree_Small_A", new Vector3(2.5f, 0, 14f), 20);
        P(root, "SM_P1_Bush_Planter_A", new Vector3(-2.4f, 0, 13f), 0);
        P(root, "SM_P1_Bush_Planter_A", new Vector3(-2.4f, 0, 20f), 0);

        // ── street props (kept just off the 4 m path) ──
        P(root, "PRP_P1_Bicycle_A", new Vector3(-1.8f, 0, 6f), 15);
        P(root, "PRP_P1_Mailbox_A", new Vector3(1.85f, 0, 3.5f), -90);
        P(root, "PRP_P1_AC_Unit_A", new Vector3(-1.9f, 1.4f, 15f), 90);
        P(root, "PRP_P1_Trash_Bag_A", new Vector3(1.8f, 0, 8.5f), 0);
        P(root, "PRP_P1_Crate_Stack_A", new Vector3(1.8f, 0, 9.3f), 25);
        P(root, "PRP_P1_Signboard_Horz_A", new Vector3(-1.85f, 1.2f, 18f), 90);
        P(root, "PRP_P1_Planter_Pot_A", new Vector3(1.8f, 0, 18.5f), 0);
        P(root, "PRP_P1_Traffic_Mirror_A", new Vector3(-1.9f, 0, 10.5f), 30);
        P(root, "PRP_P1_Road_Sign_School_A", new Vector3(1.9f, 0, 20f), -20);
        P(root, "PRP_P1_Bollard_Kit_A", new Vector3(-1.4f, 0, 10.6f), 0);
        P(root, "PRP_P1_Bollard_Kit_A", new Vector3(1.4f, 0, 10.6f), 0);

        // ── the school gate at the end + distant backdrop ──
        P(root, "SM_P1_School_Gate_A", new Vector3(0, 0, 23f), 0);
        P(root, "SM_P1_Distant_Building_Card", new Vector3(0, 0, 27.5f), 0);
        P(root, "SM_P1_Distant_Building_Card", new Vector3(-14f, 0, 26f), 30);
        P(root, "SM_P1_Distant_Building_Card", new Vector3(14f, 0, 26f), -30);

        // ── the friend, a couple of metres ahead on the path ──
        Billboard(root, "SPR_Friend", new Vector3(-0.6f, 0f, 5f), "NPC_Friend");

        Marker(root, "Spawn", new Vector3(0f, 0.1f, 1.5f));
        Marker(root, "GateReach", new Vector3(0f, 0.1f, 21f));

        // ── collision: ground + keep the player on the 4 m road ──
        Bound(root, "Ground", new Vector3(0, -0.5f, 11.5f), new Vector3(5f, 1f, 27f));
        Bound(root, "Wall_L", new Vector3(-2.1f, 1.5f, 11.5f), new Vector3(0.3f, 3f, 27f));
        Bound(root, "Wall_R", new Vector3(2.1f, 1.5f, 11.5f), new Vector3(0.3f, 3f, 27f));
        Bound(root, "Wall_Start", new Vector3(0, 1.5f, 0.3f), new Vector3(4.6f, 3f, 0.4f));
        Bound(root, "Wall_End", new Vector3(0, 1.5f, 22f), new Vector3(4.6f, 3f, 0.4f));

        // ── mood (thin grey morning) + beats ──
        var mood = new GameObject("MOOD");
        var rm = mood.AddComponent<RoomMood>();
        rm.ambient = new Color(0.55f, 0.55f, 0.58f);
        rm.fog = new Color(0.62f, 0.63f, 0.66f); rm.fogDensity = 0.010f;
        rm.cameraBackground = new Color(0.68f, 0.69f, 0.72f);
        var beats = mood.AddComponent<PrologueBeats>();
        beats.sceneId = "walk"; beats.nextScene = PrologueScenePaths.School;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        int n = root.GetComponentsInChildren<MeshRenderer>().Length;
        return $"SC_P1_Walk street built: {n} meshes + friend + gate + full boundary collision";
    }

    static void P(Transform parent, string mesh, Vector3 pos, float yaw, bool solid = false, int variant = -1)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{kKit}/{mesh}.obj");
        if (src == null) { Debug.LogWarning($"[Walk] missing {mesh}"); return; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
        go.name = mesh; go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        var mat = MatFor(mesh, variant);
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
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; var c = go.AddComponent<BoxCollider>(); c.size = size;
    }

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
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = parent.position + pos;
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.name = "Sprite";
        Object.DestroyImmediate(quad.GetComponent<MeshCollider>());
        quad.transform.SetParent(go.transform, false);
        quad.transform.localScale = new Vector3(1f, 1.6875f, 1f);
        quad.transform.localPosition = new Vector3(0, 1.6875f * 0.5f, 0);
        quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
        quad.AddComponent<BillboardY>();
    }

    static Material MatFor(string mesh, int variant)
    {
        string key; Color c; bool emiss = false; float k = 0;
        if (mesh.Contains("Road") || mesh.Contains("Manhole")) { key = "Road"; c = new Color(0.17f, 0.17f, 0.19f); }
        else if (mesh.Contains("Crosswalk")) { key = "Crosswalk"; c = new Color(0.80f, 0.80f, 0.82f); }
        else if (mesh.Contains("Sidewalk") || mesh.Contains("Curb")) { key = "Walk"; c = new Color(0.44f, 0.44f, 0.46f); }
        else if (mesh.Contains("Puddle")) { key = "Puddle"; c = new Color(0.22f, 0.25f, 0.30f); emiss = true; k = 0.10f; }   // dark wet sheen, not a glowing slab
        else if (mesh.Contains("Roof")) { key = "Roof"; c = new Color(0.26f, 0.25f, 0.30f); }
        else if (mesh.Contains("House_Wall"))
        {
            var cols = new[] { new Color(0.52f, 0.38f, 0.33f), new Color(0.56f, 0.55f, 0.52f), new Color(0.60f, 0.56f, 0.49f) };
            key = "House" + Mathf.Max(0, variant); c = cols[Mathf.Clamp(variant, 0, 2)];
        }
        else if (mesh.Contains("Storefront_Awning")) { key = "Awning"; c = new Color(0.42f, 0.26f, 0.30f); }
        else if (mesh.Contains("Storefront")) { key = "Store"; c = new Color(0.44f, 0.34f, 0.28f); }
        else if (mesh.Contains("Window")) { key = "Window"; c = new Color(0.34f, 0.40f, 0.47f); emiss = true; k = 0.12f; }
        else if (mesh.Contains("Gate")) { key = "Gate"; c = new Color(0.34f, 0.35f, 0.39f); }
        else if (mesh.Contains("Distant")) { key = "Distant"; c = new Color(0.60f, 0.62f, 0.67f); }
        else if (mesh.Contains("Tree") || mesh.Contains("Bush")) { key = "Foliage"; c = new Color(0.29f, 0.39f, 0.27f); }
        else if (mesh.Contains("Fence") || mesh.Contains("Concrete") || mesh.Contains("Pole") || mesh.Contains("Wire")) { key = "Grey"; c = new Color(0.44f, 0.44f, 0.46f); }
        else if (mesh.Contains("Signboard") || mesh.Contains("Sign")) { key = "Sign"; c = new Color(0.52f, 0.36f, 0.34f); }
        else if (mesh.Contains("Mirror") || mesh.Contains("AC_") || mesh.Contains("Bollard") || mesh.Contains("Mailbox")) { key = "Metal"; c = new Color(0.45f, 0.46f, 0.49f); }
        else if (mesh.Contains("Bicycle") || mesh.Contains("Crate") || mesh.Contains("Planter") || mesh.Contains("Trash")) { key = "Prop"; c = new Color(0.40f, 0.38f, 0.36f); }
        else { key = "Grey2"; c = new Color(0.48f, 0.48f, 0.50f); }

        var path = $"{kMatDir}/M_P1_{key}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { System.IO.Directory.CreateDirectory(kMatDir); m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", mesh.Contains("Road") ? 0.35f : 0.05f);
        if (emiss) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * k); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
        EditorUtility.SetDirty(m);
        return m;
    }
}
