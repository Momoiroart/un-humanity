// UN-HUMANITY — SC_P2_School REMADE from the outdoor gate into the blueprint's
// interior 2F-East hallway (Prologue Asset Kit, 2026-08-04). A 2.4 m-wide
// corridor the player faces down; the CRACK bursts from the left wall mid-hall
// (near the locked actors, so it's visible, the frozen figure reads, and the
// friend can walk up to paint it — per the design workflow's adversarial pass).
// Preserves the flow contracts verbatim: Spawn (active), NPC_Friend billboard,
// SchoolAnomaly (built INACTIVE, found via FindInScene), MOOD with RoomMood +
// PrologueBeats{sceneId="school", nextScene=Street}, authored SceneCam.
//   unity command eval "return SchoolLayout.Build();"

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SchoolLayout
{
    const string kScene = "Assets/Scenes/SC_P2_School.unity";
    const string kKit = "Assets/Art/Prologue/SC_P2_SCHOOL";
    const string kMatDir = "Assets/Art/Prologue/Materials";
    const string kSprDir = "Assets/Art/Sprites";

    static readonly float[] Cells = { 1f, 3f, 5f, 7f };   // corridor Z 0..8, crack at the terminus
    const float WallX = 1.285f, InnerX = 1.15f, CrackZ = 8f;

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "SCHOOL" || g.name == "MOOD"))
            Object.DestroyImmediate(g);
        var root = new GameObject("SCHOOL").transform;

        // ── shell: floor / ceiling / walls, tiled 2 m along Z (rotY 90) ──
        foreach (var z in Cells)
        {
            P(root, "SM_P2_Hallway_Floor_2m", new Vector3(0, -0.11f, z), 90);   // slab top → Y=0
            P(root, "SM_P2_Hallway_Ceiling_2m", new Vector3(0, 3.10f, z), 90);
            P(root, "SM_P2_Hallway_Wall_2m", new Vector3(-WallX, 0, z), 90);
            P(root, "SM_P2_Hallway_Wall_2m", new Vector3(WallX, 0, z), 90);
            P(root, "SM_P2_Baseboard_2m", new Vector3(-InnerX, 0, z), 90);
            P(root, "SM_P2_Baseboard_2m", new Vector3(InnerX, 0, z), 90);
        }
        // the stairwell terminus (the corridor OPENS into it during the crack;
        // in normal state it reads as a dim recessed end past a doorway)
        P(root, "SM_P2_Stairwell_Flight_A", new Vector3(0, 0, 9.3f), 0);
        P(root, "SM_P2_Stair_Rail_A", new Vector3(1.15f, 0, 9.3f), 0);

        // ── LEFT wall (-X): lockers, doors, entrance fire kit ──
        P(root, "SM_P2_Shoe_Cubby_A", new Vector3(-1.015f, 0, 1f), 90, true);
        P(root, "SM_P2_Locker_Bank_A", new Vector3(-0.955f, 0, 3f), 90, true);
        P(root, "SM_P2_Door_Classroom_A", new Vector3(-1.155f, 0, 5f), 90);
        P(root, "SM_P2_Classroom_Sign_A", new Vector3(-1.15f, 2.25f, 5f), 90);
        P(root, "SM_P2_Door_Clinic_A", new Vector3(-1.14f, 0, 6.6f), 90, true);
        P(root, "SM_P2_Notice_Board_A", new Vector3(-1.175f, 1.0f, 6.5f), 90);
        // fire kit near the ENTRANCE (kept away from the crack so safety-red
        // doesn't pre-train the eye where the crimson will bloom)
        P(root, "PRP_P2_Fire_Cabinet_A", new Vector3(-1.075f, 0, 2f), 90);
        P(root, "PRP_P2_Fire_Extinguisher_A", new Vector3(-1.05f, 0, 2.4f), 0);

        // ── RIGHT wall (+X): windows + clock ──
        foreach (var z in new[] { 3f, 5f })
            P(root, "SM_P2_Window_Bay_A", new Vector3(1.08f, 0.90f, z), 90);
        P(root, "PRP_P2_Clock_Wall_A", new Vector3(1.16f, 2.40f, 4f), 90);

        // ── ceiling / floor dressing ──
        foreach (var z in new[] { 2f, 5f, 7f })
            P(root, "SM_P2_Service_Light_A", new Vector3(0, 2.95f, z), 0);
        // real lights so the corridor has form/shading (it was flat ambient
        // only — the single biggest anti-flat fix). CrackLerp drains these too.
        foreach (var z in new[] { 2f, 5f, 7f })
        {
            var lg = new GameObject($"HallLight_{z}"); lg.transform.SetParent(root, false);
            lg.transform.localPosition = new Vector3(0, 2.82f, z);
            var pl = lg.AddComponent<Light>(); pl.type = LightType.Point;
            pl.color = new Color(0.98f, 0.95f, 0.88f); pl.intensity = 1.4f; pl.range = 5.5f;
        }
        var keyGo = new GameObject("HallKey"); keyGo.transform.SetParent(root, false);
        keyGo.transform.rotation = Quaternion.Euler(52f, -18f, 0f);
        var kl = keyGo.AddComponent<Light>(); kl.type = LightType.Directional;
        kl.color = new Color(0.85f, 0.87f, 0.95f); kl.intensity = 0.5f; kl.shadows = LightShadows.Soft;
        P(root, "SM_P2_Ceiling_Vent_A", new Vector3(0.4f, 3.06f, 4f), 0);
        P(root, "SM_P2_Pipe_Utility_A", new Vector3(-1.10f, 2.70f, 6f), 0);
        P(root, "PRP_P2_Vending_Hall_A", new Vector3(1.02f, 0, 7f), 90, true);   // far-right, clear of the crack sightline
        P(root, "PRP_P2_Bench_Hall_A", new Vector3(0.85f, 0, 6f), 90, true);
        P(root, "PRP_P2_Trash_Bin_A", new Vector3(0.90f, 0, 3.6f), 0);
        P(root, "PRP_P2_Bucket_Mop_A", new Vector3(-0.90f, 0, 7f), 0);
        P(root, "PRP_P2_Umbrella_Stand_A", new Vector3(0.90f, 0, 7f), 0);
        P(root, "PRP_P2_Desk_Stack_A", new Vector3(-0.80f, 0, 4f), 90);
        P(root, "PRP_P2_Floor_Decal_Scuff_A", new Vector3(0, 0.01f, 6f), 0);

        // ── the friend + spawn ── placed in the open aisle (not jammed against
        // the lockers): protagonist right-of-centre so it reads clear of the
        // left wall; friend a step deeper in the lane, visible, not occluded
        Billboard(root, "SPR_Friend", new Vector3(-0.4f, 0f, 3.4f), "NPC_Friend");
        Marker(root, "Spawn", new Vector3(0.25f, 0.1f, 2.2f));

        // ── THE ANOMALY (built INACTIVE) — bursts from the left wall at CrackZ ──
        BuildAnomaly(root);

        // ── authored camera: offset to the right so the actors sit LEFT and
        // don't occlude the crack blooming at the corridor terminus ──
        var camGo = new GameObject("SceneCam"); camGo.transform.SetParent(root, false);
        camGo.transform.position = new Vector3(0.72f, 1.85f, -2.3f);
        camGo.transform.rotation = Quaternion.LookRotation(new Vector3(-0.15f, 1.35f, 7.2f) - camGo.transform.position, Vector3.up);
        camGo.AddComponent<SceneCam>().fov = 55f;

        // ── collision: seal the corridor box (Z 0..8) ──
        Bound(root, "Bound_Ground", new Vector3(0, -0.5f, 4f), new Vector3(2.4f, 1f, 9.6f));
        Bound(root, "Bound_Wall_L", new Vector3(-1.35f, 1.7f, 4f), new Vector3(0.3f, 3.4f, 9.6f));
        Bound(root, "Bound_Wall_R", new Vector3(1.35f, 1.7f, 4f), new Vector3(0.3f, 3.4f, 9.6f));
        Bound(root, "Bound_Near", new Vector3(0, 1.7f, 0.2f), new Vector3(2.6f, 3.4f, 0.3f));
        Bound(root, "Bound_Far", new Vector3(0, 1.7f, 8.3f), new Vector3(2.6f, 3.4f, 0.3f));

        // ── mood (normal indoor — ambient MUST match CrackLerp's restore) + beats ──
        var mood = new GameObject("MOOD");
        var rm = mood.AddComponent<RoomMood>();
        rm.ambient = new Color(0.52f, 0.50f, 0.52f);
        rm.fog = new Color(0.60f, 0.60f, 0.62f); rm.fogDensity = 0.012f;
        rm.cameraBackground = new Color(0.30f, 0.28f, 0.30f);
        var beats = mood.AddComponent<PrologueBeats>();
        beats.sceneId = "school"; beats.nextScene = PrologueScenePaths.Handoff;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        int n = root.GetComponentsInChildren<MeshRenderer>(true).Length;
        return $"SC_P2_School hallway remade: {n} meshes + crack (inactive) + friend + SceneCam + collision";
    }

    // the crack: a single INACTIVE root filling the corridor TERMINUS (Z=8), so
    // it blooms at the vanishing point and the stairwell + frozen figure
    // silhouette against a wine-void backdrop — visible past the off-centre cam.
    static void BuildAnomaly(Transform root)
    {
        var a = new GameObject("SchoolAnomaly").transform;
        a.SetParent(root, false);
        a.localPosition = new Vector3(0, 0, CrackZ);

        // the wine-black void the stair climbs into — a large emissive backdrop
        // plane facing the camera, dim enough to read as depth, bright enough
        // to silhouette the stairwell and the figure against it
        var glow = GameObject.CreatePrimitive(PrimitiveType.Quad); glow.name = "Void_Glow";
        Object.DestroyImmediate(glow.GetComponent<MeshCollider>());
        glow.transform.SetParent(a, false);
        glow.transform.localPosition = new Vector3(0, 1.7f, 1.0f);
        glow.transform.localScale = new Vector3(2.8f, 3.4f, 1f);
        glow.GetComponent<MeshRenderer>().sharedMaterial = Wine(0.20f, 0.03f, 0.06f, 0.7f);   // dim deep-wine void, not a bright slab

        PA(a, "ANM_P2_Stairwell_Hidden_A", new Vector3(0, 0, 0.7f), 0, Ink(0.09f, 0.03f, 0.05f, 0.5f));
        PA(a, "ANM_P2_Crack_Core_A", new Vector3(0, 0, 0.45f), 0, Wine(0.46f, 0.05f, 0.11f, 2.4f));
        PA(a, "ANM_P2_Crimson_Light_Rig", new Vector3(0, 0.3f, 0.55f), 0, Ink(0.08f, 0.02f, 0.04f, 0.2f));
        PA(a, "ANM_P2_Rift_Shard_A", new Vector3(-0.8f, 0, 0.2f), 15, Wine(0.5f, 0.06f, 0.14f, 1.8f));
        PA(a, "ANM_P2_Rift_Shard_A", new Vector3(0.85f, 0.4f, 0.3f), -20, Wine(0.5f, 0.06f, 0.14f, 1.8f));
        PA(a, "ANM_P2_Distortion_Card_A", new Vector3(-0.6f, 1.6f, -0.2f), 12, Wine(0.5f, 0.08f, 0.16f, 1.4f));
        PA(a, "ANM_P2_Distortion_Card_A", new Vector3(0.6f, 2.1f, -0.1f), -18, Wine(0.5f, 0.08f, 0.16f, 1.4f));
        PA(a, "ANM_P2_Residue_Decal_A", new Vector3(0.0f, 0.01f, -0.7f), 0, Ink(0.18f, 0.07f, 0.10f, 0f));
        PA(a, "ANM_P2_Billboard_Graffiti_A", new Vector3(-1.1f, 1.3f, -0.3f), 90, Ink(0.14f, 0.05f, 0.09f, 0.4f));

        // the frozen first-year — on the near step, closer to camera, silhouetted
        // against the void glow (a dark billboard with a thin wine rim)
        var fig = new GameObject("SchoolAnomaly_Figure");
        fig.transform.SetParent(a, false); fig.transform.localPosition = new Vector3(0.35f, 0.25f, -0.6f);
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Sprite";
        Object.DestroyImmediate(q.GetComponent<MeshCollider>());
        q.transform.SetParent(fig.transform, false);
        q.transform.localScale = new Vector3(0.9f, 1.7f, 1f);
        q.transform.localPosition = new Vector3(0, 0.85f, 0);
        q.GetComponent<MeshRenderer>().sharedMaterial = FigureMat();
        q.AddComponent<BillboardY>();

        // the crimson bleed-light (wine, not hot pink) — rides the toggle
        var lightGo = new GameObject("Crack_Light"); lightGo.transform.SetParent(a, false);
        lightGo.transform.localPosition = new Vector3(0, 1.5f, 0.3f);
        var lt = lightGo.AddComponent<Light>();
        lt.type = LightType.Point; lt.color = new Color(0.62f, 0.08f, 0.09f);   // crimson bleed (blue killed)
        lt.intensity = 2.4f; lt.range = 8f;

        a.gameObject.SetActive(false);   // hidden until CrackLerp reveals it
    }

    // ── helpers ──
    static void P(Transform parent, string mesh, Vector3 pos, float yaw, bool solid = false)
        => Place(parent, mesh, pos, yaw, MatFor(mesh), solid);

    static void PA(Transform parent, string mesh, Vector3 pos, float yaw, Material mat)
        => Place(parent, mesh, pos, yaw, mat, false);

    static void Place(Transform parent, string mesh, Vector3 pos, float yaw, Material mat, bool solid)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{kKit}/{mesh}.obj");
        if (src == null) { Debug.LogWarning($"[School] missing {mesh}"); return; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
        go.name = mesh; go.transform.SetParent(parent, false);
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
    { var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos; var c = go.AddComponent<BoxCollider>(); c.size = size; }

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

    // ── materials ──
    static Material Get(string key)
    {
        var path = $"{kMatDir}/M_P2_{key}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { System.IO.Directory.CreateDirectory(kMatDir); m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        return m;
    }
    static Material Flat(string key, Color c, bool emiss = false, float k = 0)
    {
        var m = Get(key); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0.05f);
        if (emiss) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * k); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
        EditorUtility.SetDirty(m); return m;
    }
    // wine-crimson anomaly emissive (base dark wine, emission toward accent #D14673)
    static Material Wine(float r, float g, float b, float k)
    {
        var m = Get($"Anom_{(int)(r * 100)}_{(int)(k * 10)}"); m.SetColor("_BaseColor", new Color(r, g, b)); m.SetFloat("_Smoothness", 0.1f);
        m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(0.62f, 0.06f, 0.08f) * k);   // crimson (blue killed) — reads red, not pink
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; EditorUtility.SetDirty(m); return m;
    }
    static Material Ink(float r, float g, float b, float k)
    {
        var m = Get($"Ink_{(int)(r * 100)}_{(int)(k * 10)}"); m.SetColor("_BaseColor", new Color(r, g, b)); m.SetFloat("_Smoothness", 0.05f);
        if (k > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(0.52f, 0.05f, 0.06f) * k); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
        EditorUtility.SetDirty(m); return m;
    }
    static Material FigureMat()
    {
        var m = Get("Figure"); m.SetColor("_BaseColor", new Color(0.06f, 0.03f, 0.05f)); m.SetFloat("_Smoothness", 0f);
        m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.4f);
        m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(0.55f, 0.14f, 0.24f) * 0.7f);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; EditorUtility.SetDirty(m); return m;
    }

    static Material MatFor(string mesh)
    {
        if (mesh.Contains("Floor") && !mesh.Contains("Decal")) return Flat("Floor", new Color(0.43f, 0.48f, 0.45f));
        if (mesh.Contains("Wall") || mesh.Contains("Ceiling") || mesh.Contains("Sealed")) return Flat("Wall", new Color(0.77f, 0.76f, 0.74f));
        if (mesh.Contains("Locker") || mesh.Contains("Cubby") || mesh.Contains("Bench")) return Flat("Locker", new Color(0.31f, 0.42f, 0.39f));
        if (mesh.Contains("Window")) return Flat("Window", new Color(0.62f, 0.70f, 0.74f), true, 0.45f);
        if (mesh.Contains("Door") || mesh.Contains("Notice") || mesh.Contains("Desk")) return Flat("Wood", new Color(0.33f, 0.26f, 0.24f));
        if (mesh.Contains("Service_Light")) return Flat("Fluoro", new Color(0.98f, 0.94f, 0.85f), true, 1.4f);
        if (mesh.Contains("Fire")) return Flat("SafetyRed", new Color(0.62f, 0.20f, 0.16f));    // mundane safety red, NOT absence
        if (mesh.Contains("Sign") || mesh.Contains("Flyer") || mesh.Contains("Clock")) return Flat("Paper", new Color(0.82f, 0.80f, 0.74f));
        if (mesh.Contains("Vending")) return Flat("Vending", new Color(0.40f, 0.46f, 0.50f), true, 0.3f);
        if (mesh.Contains("Pipe") || mesh.Contains("Vent") || mesh.Contains("Baseboard") || mesh.Contains("Umbrella")) return Flat("Metal", new Color(0.52f, 0.54f, 0.55f));
        if (mesh.Contains("Bucket") || mesh.Contains("Trash")) return Flat("Prop", new Color(0.40f, 0.42f, 0.42f));
        return Flat("Grey", new Color(0.55f, 0.55f, 0.56f));
    }
}
