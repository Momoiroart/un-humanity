using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

// Builds real 3D residential houses for SC_P1_Walk from 4 archetypes and places
// them down both sides of the street with colour/type variation, replacing the
// old flat facade panels. Re-runnable: WalkHouses.Build().
//
// Local build frame for every archetype: FRONT face at local x = 0, the house
// extends toward -x (depth), width runs along z (centred), height is +y from 0.
// The placement loop rotates each house so its front faces the road.
public static class WalkHouses
{
    static Shader Lit => Shader.Find("Universal Render Pipeline/Lit");
    static Shader Unlit => Shader.Find("Universal Render Pipeline/Unlit");
    static readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();

    static Material M(string hex, float smooth = 0.05f, float metal = 0f, bool emis = false)
    {
        string key = hex + "|" + smooth + "|" + metal + "|" + emis;
        if (_mats.TryGetValue(key, out var cached)) return cached;
        var m = new Material(Lit);
        ColorUtility.TryParseHtmlString(hex, out var c);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        if (emis) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 1.4f); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
        _mats[key] = m;
        return m;
    }

    static GameObject Box(Transform p, string n, Vector3 pos, Vector3 size, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = n; Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(p, false);
        g.transform.localPosition = pos; g.transform.localScale = size;
        g.GetComponent<MeshRenderer>().sharedMaterial = m;
        return g;
    }

    static GameObject Mesh(Transform p, string mesh, Vector3 pos, float yaw, Vector3 scale, Material over = null)
    {
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prologue/SC_P1_WALK/" + mesh + ".obj");
        if (pf == null) return null;
        var g = (GameObject)PrefabUtility.InstantiatePrefab(pf);
        g.transform.SetParent(p, false);
        g.transform.localPosition = pos; g.transform.localEulerAngles = new Vector3(0, yaw, 0); g.transform.localScale = scale;
        if (over != null) foreach (var mr in g.GetComponentsInChildren<MeshRenderer>()) { var arr = mr.sharedMaterials; for (int i = 0; i < arr.Length; i++) arr[i] = over; mr.sharedMaterials = arr; }
        return g;
    }

    // window unit: recessed frame + dark glass, faces +x (local). w,h = size.
    static void Win(Transform p, Vector3 c, float w, float h, Material glass, Material frame)
    {
        Box(p, "winframe", c, new Vector3(0.10f, h + 0.14f, w + 0.14f), frame);
        Box(p, "winglass", c + new Vector3(0.03f, 0, 0), new Vector3(0.06f, h, w), glass);
        Box(p, "winsill", c + new Vector3(0.02f, -h / 2 - 0.05f, 0), new Vector3(0.22f, 0.08f, w + 0.2f), frame);
    }

    // ---- gable roof from two tilted planes (ridge along x/depth) ----
    static void Gable(Transform p, float cx, float depth, float w, float baseY, float peak, Material m, float over = 0.45f)
    {
        float len = depth + over * 2f;
        float half = w / 2f + over;
        float slope = Mathf.Sqrt(half * half + peak * peak);
        float ang = Mathf.Atan2(peak, half) * Mathf.Rad2Deg;
        var l = Box(p, "roofL", new Vector3(cx, baseY + peak / 2f, -half / 2f), new Vector3(len, 0.14f, slope), m);
        l.transform.localEulerAngles = new Vector3(ang, 0, 0);
        var r = Box(p, "roofR", new Vector3(cx, baseY + peak / 2f, half / 2f), new Vector3(len, 0.14f, slope), m);
        r.transform.localEulerAngles = new Vector3(-ang, 0, 0);
        // gable end fills (front + back triangles approximated by thin tall boxes)
        Box(p, "gableF", new Vector3(cx + depth / 2f + over - 0.05f, baseY + peak * 0.5f, 0), new Vector3(0.1f, peak, 0.3f), m);
    }

    static void Parapet(Transform p, float cx, float depth, float w, float topY, Material m)
    {
        Box(p, "roofslab", new Vector3(cx, topY + 0.08f, 0), new Vector3(depth + 0.2f, 0.16f, w + 0.2f), m);
        Box(p, "parF", new Vector3(cx + depth / 2f + 0.05f, topY + 0.28f, 0), new Vector3(0.12f, 0.3f, w + 0.3f), m);
        Box(p, "parL", new Vector3(cx, topY + 0.28f, w / 2f + 0.05f), new Vector3(depth + 0.3f, 0.3f, 0.12f), m);
        Box(p, "parR", new Vector3(cx, topY + 0.28f, -w / 2f - 0.05f), new Vector3(depth + 0.3f, 0.3f, 0.12f), m);
    }

    // ============ ARCHETYPES (return root, front at x=0) ============

    public static GameObject Machiya(Transform parent, string wallHex, string roofHex)
    {
        var root = new GameObject("House_Machiya"); root.transform.SetParent(parent, false);
        var t = root.transform;
        var wood = M("#5A4433"); var wall = M(wallHex); var trim = M("#3E2E22"); var glass = M("#2C3A44", 0.8f); var roof = M(roofHex, 0.1f);
        float w = 4.0f, d = 5.0f;
        Box(t, "ground", new Vector3(-d / 2f, 1.15f, 0), new Vector3(d, 2.3f, w), wood);          // wood ground floor
        Box(t, "upper", new Vector3(-d / 2f + 0.1f, 3.35f, 0), new Vector3(d - 0.2f, 2.0f, w - 0.2f), wall); // plaster upper
        Box(t, "beltcourse", new Vector3(-0.05f, 2.35f, 0), new Vector3(0.3f, 0.18f, w + 0.1f), trim);
        Gable(t, -d / 2f, d, w, 4.35f, 1.35f, roof);
        // entrance recess + sliding door + noren
        Box(t, "genkan", new Vector3(-0.35f, 1.05f, -0.9f), new Vector3(0.7f, 2.1f, 1.5f), M("#241A14"));
        Mesh(t, "SM_P1_House_Door_A", new Vector3(0.02f, 0, -0.9f), 90f, Vector3.one, wood);
        Box(t, "noren", new Vector3(0.06f, 1.95f, -0.9f), new Vector3(0.04f, 0.5f, 1.4f), M("#3A5570"));
        Box(t, "lantern", new Vector3(0.08f, 2.1f, -1.75f), new Vector3(0.18f, 0.4f, 0.18f), M("#E8C070", 0.1f, 0f, true));
        // lattice windows: ground (right of door), upper (two)
        Win(t, new Vector3(0.02f, 1.3f, 1.0f), 1.7f, 1.3f, glass, trim);
        Win(t, new Vector3(0.03f, 3.4f, -0.9f), 1.3f, 1.2f, glass, trim);
        Win(t, new Vector3(0.03f, 3.4f, 1.0f), 1.3f, 1.2f, glass, trim);
        Box(t, "downpipe", new Vector3(0.03f, 2.2f, w / 2f - 0.15f), new Vector3(0.12f, 4.4f, 0.12f), trim);
        Mesh(t, "PRP_P1_Planter_Pot_A", new Vector3(-0.1f, 0, -1.9f), 0, Vector3.one);
        return root;
    }

    public static GameObject Modern(Transform parent, string wallHex, string accentHex)
    {
        var root = new GameObject("House_Modern"); root.transform.SetParent(parent, false);
        var t = root.transform;
        var wall = M(wallHex); var accent = M(accentHex); var trim = M("#4A4A50"); var glass = M("#33506A", 0.85f); var rail = M("#6A6E74", 0.4f, 0.6f);
        float w = 4.4f, d = 5.0f;
        Box(t, "ground", new Vector3(-d / 2f, 1.3f, 0), new Vector3(d, 2.6f, w), wall);
        Box(t, "upper", new Vector3(-d / 2f, 3.9f, 0), new Vector3(d, 2.6f, w), wall);
        Box(t, "accentband", new Vector3(-0.06f, 2.62f, 0), new Vector3(0.28f, 0.12f, w + 0.1f), accent);
        Parapet(t, -d / 2f, d, w, 5.2f, wall);
        // upper balcony (protrudes past front) + railing
        Box(t, "balconyslab", new Vector3(0.35f, 2.75f, 0.6f), new Vector3(0.9f, 0.14f, 3.0f), trim);
        for (int i = -3; i <= 3; i++) Box(t, "railpost", new Vector3(0.78f, 3.1f, 0.6f + i * 0.5f), new Vector3(0.06f, 0.7f, 0.06f), rail);
        Box(t, "railtop", new Vector3(0.78f, 3.45f, 0.6f), new Vector3(0.08f, 0.08f, 3.1f), rail);
        // big ground window + porch canopy over door
        Win(t, new Vector3(0.02f, 1.4f, 0.7f), 2.2f, 1.7f, glass, trim);
        Box(t, "porch", new Vector3(0.5f, 2.35f, -1.2f), new Vector3(1.2f, 0.12f, 1.8f), accent);
        Mesh(t, "SM_P1_House_Door_A", new Vector3(0.02f, 0, -1.2f), 90f, Vector3.one, accent);
        Win(t, new Vector3(0.02f, 3.9f, -0.9f), 1.5f, 1.4f, glass, trim);
        Win(t, new Vector3(0.02f, 3.9f, 1.1f), 1.5f, 1.4f, glass, trim);
        Mesh(t, "PRP_P1_AC_Unit_A", new Vector3(0.05f, 3.0f, w / 2f - 0.4f), 90f, Vector3.one);
        // carport slab + posts on the far side
        Box(t, "carroof", new Vector3(-1.2f, 2.5f, -w / 2f - 1.0f), new Vector3(3.4f, 0.12f, 2.0f), trim);
        Box(t, "carpost1", new Vector3(0.3f, 1.25f, -w / 2f - 1.7f), new Vector3(0.14f, 2.5f, 0.14f), rail);
        Box(t, "carpost2", new Vector3(-2.6f, 1.25f, -w / 2f - 1.7f), new Vector3(0.14f, 2.5f, 0.14f), rail);
        return root;
    }

    static GameObject Shophouse(Transform parent, string wallHex, string awnHex)
    {
        var root = new GameObject("House_Shop"); root.transform.SetParent(parent, false);
        var t = root.transform;
        var wall = M(wallHex); var shop = M("#6E5240"); var awn = M(awnHex); var glass = M("#3A4A55", 0.85f, 0f, false); var sign = M("#E8C878", 0.1f, 0f, true); var trim = M("#3E3228");
        float w = 4.6f, d = 4.5f;
        Box(t, "ground", new Vector3(-d / 2f, 1.25f, 0), new Vector3(d, 2.5f, w), shop);
        Box(t, "upper", new Vector3(-d / 2f + 0.1f, 3.75f, 0), new Vector3(d - 0.2f, 2.4f, w - 0.1f), wall);
        Parapet(t, -d / 2f, d, w, 4.95f, wall);
        // storefront glass front (big) + bulkhead
        Box(t, "shopglass", new Vector3(0.04f, 1.25f, 0.2f), new Vector3(0.06f, 2.0f, 3.4f), glass);
        Box(t, "bulkhead", new Vector3(0.06f, 0.3f, 0.2f), new Vector3(0.14f, 0.6f, 3.6f), trim);
        // awning + horizontal sign band
        Box(t, "awning", new Vector3(0.7f, 2.55f, 0.2f), new Vector3(1.4f, 0.14f, 4.0f), awn);
        Box(t, "signband", new Vector3(0.05f, 2.9f, 0.2f), new Vector3(0.12f, 0.55f, 4.2f), sign);
        // vertical kanban sign on the pier
        Box(t, "kanban", new Vector3(0.2f, 2.4f, -w / 2f - 0.1f), new Vector3(0.3f, 1.9f, 0.3f), sign);
        // upper residence windows + small balcony w/ laundry pole
        Win(t, new Vector3(0.02f, 3.8f, -1.0f), 1.4f, 1.3f, M("#2C3A44", 0.8f), trim);
        Win(t, new Vector3(0.02f, 3.8f, 1.1f), 1.4f, 1.3f, M("#2C3A44", 0.8f), trim);
        Box(t, "balcony", new Vector3(0.3f, 2.75f, 0.0f), new Vector3(0.7f, 0.12f, 3.2f), trim);
        Box(t, "laundrypole", new Vector3(0.55f, 3.2f, 0.0f), new Vector3(0.05f, 0.05f, 3.0f), M("#8A8E94", 0.4f, 0.6f));
        return root;
    }

    public static GameObject Danchi(Transform parent, string wallHex)
    {
        var root = new GameObject("House_Danchi"); root.transform.SetParent(parent, false);
        var t = root.transform;
        var wall = M(wallHex); var conc = M("#8A857C"); var glass = M("#33506A", 0.85f); var rail = M("#7C8088", 0.4f, 0.6f); var trim = M("#5A5650");
        float w = 5.0f, d = 4.2f;
        for (int f = 0; f < 3; f++)
        {
            float y = 1.3f + f * 2.5f;
            Box(t, "floor" + f, new Vector3(-d / 2f, y, 0), new Vector3(d, 2.5f, w), (f % 2 == 0) ? wall : conc);
            // repeated windows per floor
            for (int wI = -1; wI <= 1; wI++) Win(t, new Vector3(0.02f, y + 0.1f, wI * 1.5f), 1.1f, 1.3f, glass, trim);
        }
        Parapet(t, -d / 2f, d, w, 8.05f, conc);
        // rooftop water tank
        Box(t, "tanklegs", new Vector3(-d / 2f, 8.4f, 1.4f), new Vector3(0.1f, 0.6f, 0.1f), rail);
        Box(t, "watertank", new Vector3(-d / 2f, 8.9f, 1.4f), new Vector3(1.4f, 1.0f, 1.4f), M("#9AA0A6", 0.3f));
        // external walkway + railing on the front, all floors
        for (int f = 0; f < 3; f++)
        {
            float y = 1.3f + f * 2.5f;
            Box(t, "walk" + f, new Vector3(0.35f, y - 1.25f, 0), new Vector3(0.7f, 0.12f, w), trim);
            Box(t, "wrail" + f, new Vector3(0.7f, y - 0.9f, 0), new Vector3(0.06f, 0.7f, w), rail);
        }
        // external staircase (diagonal box) at one end
        var stair = Box(t, "stair", new Vector3(0.4f, 3.0f, -w / 2f - 0.4f), new Vector3(0.8f, 0.2f, 5.5f), trim);
        stair.transform.localEulerAngles = new Vector3(48f, 0, 0);
        // mailbox bank at ground
        Mesh(t, "PRP_P1_Mailbox_A", new Vector3(-0.1f, 0, -w / 2f + 0.3f), 90f, new Vector3(1.4f, 1f, 1f));
        return root;
    }

    // proper vending machine: bright body, lit display, product window, tray
    static GameObject Vending(Transform parent, string bodyHex)
    {
        var root = new GameObject("Vending"); root.transform.SetParent(parent, false);
        var t = root.transform;
        var body = M(bodyHex, 0.35f); var disp = M("#EAF2FF", 0.1f, 0f, true); var prod = M("#12161C", 0.6f);
        Box(t, "body", new Vector3(0, 0.95f, 0), new Vector3(0.75f, 1.9f, 1.05f), body);
        Box(t, "display", new Vector3(0.4f, 1.35f, 0), new Vector3(0.06f, 0.95f, 0.9f), disp);              // lit upper display
        Box(t, "prodwin", new Vector3(0.4f, 1.35f, 0), new Vector3(0.03f, 0.85f, 0.8f), prod);
        // colourful product cans in the window (rows)
        string[] cans = { "#D8342E", "#2E7BD8", "#E8B020", "#2EA84E", "#D84EA0" };
        for (int r = 0; r < 3; r++) for (int cI = 0; cI < 4; cI++)
                Box(t, "can", new Vector3(0.42f, 1.05f + r * 0.28f, -0.32f + cI * 0.21f), new Vector3(0.02f, 0.18f, 0.12f), M(cans[(r + cI) % cans.Length], 0.2f));
        Box(t, "tray", new Vector3(0.4f, 0.35f, 0), new Vector3(0.08f, 0.14f, 0.55f), prod);                // dispense tray
        Box(t, "buttons", new Vector3(0.42f, 0.75f, 0.36f), new Vector3(0.04f, 0.4f, 0.12f), M("#B03028", 0.3f));
        return root;
    }

    // =================== PLACE ===================
    [MenuItem("UNHUMANITY/Build Walk Houses")]
    public static string Build()
    {
        _mats.Clear();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SC_P1_Walk.unity", OpenSceneMode.Single);
        Transform root = null;
        foreach (var r in scene.GetRootGameObjects()) if (r.name == "WALK") root = r.transform;
        if (root == null) return "no WALK root";

        // remove any previous generated houses
        var old = new List<GameObject>();
        foreach (var tr in root.GetComponentsInChildren<Transform>(true))
            if (tr != root && (tr.name == "HouseKit")) old.Add(tr.gameObject);
        foreach (var g in old) Object.DestroyImmediate(g);

        // deactivate the old flat facade elements
        string[] hide = { "SM_P1_House_Wall_A", "SM_P1_House_Roof_Tiled_A", "SM_P1_House_Window_Grid_A", "SM_P1_House_Door_A", "SM_P1_Concrete_Wall_2m", "SM_P1_Storefront_Hardware_A", "SM_P1_Storefront_Awning_A" };
        var hideSet = new HashSet<string>(hide);
        int hidden = 0;
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mf = mr.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
            var fn = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(mf.sharedMesh));
            if (hideSet.Contains(fn)) { var pr = mr.transform; while (pr.parent != null && pr.parent != root) pr = pr.parent; if (pr.gameObject.activeSelf) { pr.gameObject.SetActive(false); hidden++; } }
        }

        var kit = new GameObject("HouseKit"); kit.transform.SetParent(root, false);
        // colour palettes (walls incl. two purples; roofs; awnings)
        string[] walls = { "#D8C9A8", "#C6A87A", "#A0AC88", "#C48460", "#A6B2B0", "#BCAE94", "#B49AA6", "#A69EB2", "#CBB68F" };
        string[] roofs = { "#3C4048", "#5A4436", "#46505A" };
        string[] awns = { "#3D5A38", "#6E4A50", "#3A5570", "#8A5A34" };
        int wi = 0;
        // per side: front x, whether facing +x (yaw 0) or -x (yaw 180)
        float[] frontX = { -3.95f, 3.95f };
        float[] yaws = { 0f, 180f };
        for (int side = 0; side < 2; side++)
        {
            float z = 4f; int idx = 0;
            while (z < 78f)
            {
                int type = (idx + side) % 4;
                string wall = walls[wi % walls.Length]; string roof = roofs[wi % roofs.Length]; string awn = awns[wi % awns.Length]; wi++;
                GameObject h = null;
                switch (type)
                {
                    case 0: h = Machiya(kit.transform, wall, roof); break;
                    case 1: h = Modern(kit.transform, wall, awns[(wi + 1) % awns.Length]); break;
                    case 2: h = Shophouse(kit.transform, wall, awn); break;
                    default: h = Danchi(kit.transform, wall); break;
                }
                h.transform.position = new Vector3(frontX[side], 0, z);
                h.transform.eulerAngles = new Vector3(0, yaws[side], 0);
                // vending machine + garden at some frontages
                if (type == 2) { var v = Vending(kit.transform, (idx % 2 == 0) ? "#C0392B" : "#2C6FBF"); v.transform.position = new Vector3(frontX[side] + (side == 0 ? 0.9f : -0.9f), 0, z - 2.2f); v.transform.eulerAngles = new Vector3(0, yaws[side], 0); }
                if (type == 1 || type == 0) { Mesh(kit.transform, "SM_P1_Fence_Panel_2m", new Vector3(frontX[side] + (side == 0 ? 0.6f : -0.6f), 0, z + 3.2f), yaws[side] + 90f, new Vector3(1.4f, 1f, 1f)); Mesh(kit.transform, "SM_P1_Bush_Planter_A", new Vector3(frontX[side] + (side == 0 ? 0.7f : -0.7f), 0, z + 3.0f), 0, Vector3.one); }
                float depth = (type == 2) ? 5.2f : (type == 3) ? 5.6f : 6.6f;
                z += depth; idx++;
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "houses placed, oldFacadesHidden=" + hidden;
    }
}
