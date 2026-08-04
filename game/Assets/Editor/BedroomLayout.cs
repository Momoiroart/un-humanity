// UN-HUMANITY — SC_P0_Bedroom, rebuilt to the SC_P0_BEDROOM master blueprint
// (/Lattest). A warm, lived-in HD-2D dollhouse diorama at ~06:48: low sun rakes
// through a curtained window in a visible shaft across a cluttered desk, an
// unmade plaid bed and a tall wardrobe. Every surface is occupied — the one
// warm scene of the prologue, built to be missed once it is taken away.
//
// Kit = SC_P0_BEDROOM (38 floor-pivot OBJ meshes). Cast is a PLACEHOLDER quad
// on the persistent rig (SPUM off, owner decision 2026-08-05), so no character
// is placed here. Re-runnable.
//   unity command eval "return BedroomLayout.Build();"

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class BedroomLayout
{
    const string kScene = "Assets/Scenes/SC_P0_Bedroom.unity";
    const string kKit = "Assets/Art/Prologue/SC_P0_BEDROOM";
    const string kMatDir = "Assets/Art/Prologue/Materials";

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "BEDROOM" || g.name == "MOOD"))
            Object.DestroyImmediate(g);
        var room = new GameObject("BEDROOM").transform;

        // ── SHELL: room 4.20(X) × 3.60(Z) × 2.60(Y). Dollhouse cutaway — front
        // wall omitted, ceiling instanced ShadowsOnly for contained top occlusion
        // so the interior isn't lit like an open stage. Floor at Y=-0.05 (0.05
        // slab → top at Y=0; every other mesh is floor-pivot and sits at Y=0). ──
        P(room, "SM_P0_Floor_A", new Vector3(0, -0.05f, 0), 0);
        P(room, "SM_P0_Wall_A", new Vector3(0, 0, 1.80f), 0);       // back / window wall
        P(room, "SM_P0_Wall_B", new Vector3(-2.10f, 0, 0), 90);     // left / door wall
        P(room, "SM_P0_Wall_B", new Vector3(2.10f, 0, 0), 90);      // right / wardrobe wall
        PShadow(room, "SM_P0_Ceiling_A", new Vector3(0, 2.55f, 0), 0);   // invisible, casts shadow only

        // baseboards tiled along the base of the three rendered walls (skip door/window gaps)
        foreach (float x in new[] { -1.6f, -0.6f, 0.4f, 1.4f })      // back wall (4)
            P(room, "SM_P0_Baseboard_1m", new Vector3(x, 0, 1.74f), 0);
        foreach (float z in new[] { -1.3f, -0.3f, 1.0f })           // left wall (3, skip door)
            P(room, "SM_P0_Baseboard_1m", new Vector3(-2.04f, 0, z), 90);
        foreach (float z in new[] { -1.3f, -0.3f, 0.7f, 1.4f })     // right wall (4)
            P(room, "SM_P0_Baseboard_1m", new Vector3(2.04f, 0, z), 90);
        foreach (float x in new[] { -1.9f, 1.9f })                  // short front returns
            P(room, "SM_P0_Baseboard_1m", new Vector3(x, 0, -1.72f), 0);
        P(room, "SM_P0_Door_A", new Vector3(-2.03f, 0, -0.85f), 90);

        // ── THE LIGHT SOURCE (the whole mood hangs off it): window at back-wall
        // left-of-centre, sheer + dark curtains, a bright sky-glow card behind
        // the glass, the procedural sun shaft, dust motes, and a low radiator. ──
        P(room, "SM_P0_Window_Frame_A", new Vector3(-0.75f, 0.85f, 1.74f), 0);
        SkyCard(room, new Vector3(-0.75f, 1.20f, 2.02f));                 // blazing morning behind the glass
        P(room, "SM_P0_Window_Glass_A", new Vector3(-0.75f, 0.85f, 1.73f), 0);
        P(room, "SM_P0_Curtain_Sheer_A", new Vector3(-1.15f, 0, 1.70f), 0);
        P(room, "SM_P0_Curtain_Sheer_A", new Vector3(-0.35f, 0, 1.70f), 0);
        P(room, "SM_P0_Curtain_Dark_A", new Vector3(-1.55f, 0, 1.68f), 0);
        P(room, "SM_P0_Curtain_Dark_A", new Vector3(0.05f, 0, 1.68f), 0);
        P(room, "SM_P0_Radiator_A", new Vector3(0.40f, 0.28f, 1.71f), 0);
        LightShaft(room);
        DustMotes(room);

        // ── STUDY CORNER (back-left): desk under the window, chair, bookcase in
        // the corner, two wide + two narrow wall shelves climbing the back wall.
        // Every surface packed. ──
        P(room, "SM_P0_Desk_A", new Vector3(-1.00f, 0, 1.45f), 0, true);
        P(room, "SM_P0_Chair_A", new Vector3(-1.00f, 0, 0.85f), 180, true);
        P(room, "SM_P0_Bookcase_A", new Vector3(-1.80f, 0, 1.05f), 90, true);
        P(room, "SM_P0_Wall_Shelf_A", new Vector3(-1.25f, 1.78f, 1.73f), 0);
        P(room, "SM_P0_Wall_Shelf_A", new Vector3(0.55f, 1.95f, 1.73f), 0);
        P(room, "SM_P0_Wall_Shelf_B", new Vector3(-1.60f, 2.12f, 1.73f), 0);
        P(room, "SM_P0_Wall_Shelf_B", new Vector3(0.90f, 2.15f, 1.73f), 0);

        // desk-top clutter (top plane ~0.74)
        P(room, "PRP_P0_Desk_Lamp_A", new Vector3(-1.50f, 0.74f, 1.60f), 25);
        P(room, "PRP_P0_Phone_Smart_A", new Vector3(-0.80f, 0.752f, 1.32f), 12);   // the summons
        P(room, "PRP_P0_Notebook_A", new Vector3(-1.00f, 0.74f, 1.35f), 8);
        P(room, "PRP_P0_Notebook_A", new Vector3(-0.72f, 0.74f, 1.50f), -12);
        P(room, "PRP_P0_Pen_A", new Vector3(-0.90f, 0.75f, 1.30f), 0);
        P(room, "PRP_P0_Pen_A", new Vector3(-0.84f, 0.75f, 1.34f), 20);
        P(room, "PRP_P0_Pen_A", new Vector3(-1.18f, 0.75f, 1.28f), -35);
        P(room, "PRP_P0_Mug_A", new Vector3(-0.62f, 0.74f, 1.46f), 0);
        P(room, "PRP_P0_Tissues_A", new Vector3(-0.55f, 0.74f, 1.55f), 0);
        P(room, "PRP_P0_Books_Stack_A", new Vector3(-1.45f, 0.74f, 1.30f), 0);
        P(room, "PRP_P0_Plant_Small_A", new Vector3(-1.56f, 0.74f, 1.68f), 0);
        P(room, "PRP_P0_Picture_Frame_A", new Vector3(-0.58f, 0.74f, 1.68f), -8);
        P(room, "PRP_P0_Power_Strip_A", new Vector3(-1.42f, 0.02f, 1.62f), 0);
        P(room, "PRP_P0_Trash_Bin_A", new Vector3(-1.75f, 0, 1.05f), 0);

        // bookcase + shelf clutter
        P(room, "PRP_P0_Books_Stack_A", new Vector3(-1.78f, 0.35f, 1.05f), 90);
        P(room, "PRP_P0_Books_Stack_A", new Vector3(-1.78f, 0.72f, 1.05f), 90);
        P(room, "PRP_P0_Books_Stack_A", new Vector3(-1.40f, 1.82f, 1.72f), 0);
        P(room, "PRP_P0_Books_Stack_A", new Vector3(0.75f, 2.19f, 1.72f), 14);
        P(room, "PRP_P0_Mug_A", new Vector3(-1.10f, 1.82f, 1.72f), 0);
        P(room, "PRP_P0_Picture_Frame_A", new Vector3(-1.25f, 1.95f, 1.72f), 0);
        P(room, "PRP_P0_Plant_Small_A", new Vector3(0.90f, 2.19f, 1.73f), 0);

        // wall-hung on the back wall
        P(room, "PRP_P0_Clock_Analog_A", new Vector3(1.00f, 1.75f, 1.76f), 0);
        P(room, "PRP_P0_Picture_Frame_A", new Vector3(-1.70f, 1.55f, 1.76f), 0);
        P(room, "PRP_P0_Picture_Frame_A", new Vector3(1.35f, 1.55f, 1.76f), 0);

        // ── BED (centre-right): long axis along Z, head to the window wall. ──
        P(room, "SM_P0_Bed_Frame_A", new Vector3(0.75f, 0, 0.15f), 0, true);
        P(room, "PRP_P0_Bedding_Set_A", new Vector3(0.75f, 0.36f, 0.05f), 0);
        P(room, "PRP_P0_Pillow_A", new Vector3(0.55f, 0.50f, 0.95f), 4);
        P(room, "PRP_P0_Pillow_A", new Vector3(0.95f, 0.50f, 0.95f), -6);

        // ── WARDROBE on the RIGHT wall (concept + top-down place it right), with
        // a hanger + the school uniform jacket on its front corner. ──
        P(room, "SM_P0_Wardrobe_A", new Vector3(1.72f, 0, 0.75f), -90, true);
        P(room, "PRP_P0_Clothes_Hanger_A", new Vector3(1.46f, 1.55f, 0.35f), -90);
        P(room, "PRP_P0_Uniform_Jacket_A", new Vector3(1.47f, 1.15f, 0.35f), -90);
        P(room, "PRP_P0_Clothes_Hanger_A", new Vector3(-2.02f, 1.55f, -0.30f), 90);
        P(room, "PRP_P0_Clothes_Hanger_A", new Vector3(-2.02f, 1.55f, -0.55f), 90);

        // ── RUG + school bag (foreground anchors where the shaft lands) + lamp ──
        P(room, "SM_P0_Rug_Rect_A", new Vector3(0.10f, 0.005f, -0.25f), 0);
        P(room, "PRP_P0_School_Bag_A", new Vector3(0.25f, 0, -0.55f), 25);
        P(room, "SM_P0_Ceiling_Lamp_A", new Vector3(0, 2.45f, 0.20f), 0);

        // ── COLLISION: ground + boundary (front wall is a visual cutaway but
        // still needs an invisible wall so the player can't run out the open side). ──
        var ground = new GameObject("Ground"); ground.transform.SetParent(room, false);
        var gc = ground.AddComponent<BoxCollider>(); gc.center = new Vector3(0, -0.5f, 0); gc.size = new Vector3(4.4f, 1f, 3.8f);
        Bound(room, "Bound_Back", new Vector3(0, 1.3f, 1.85f), new Vector3(4.4f, 2.6f, 0.2f));
        Bound(room, "Bound_Front", new Vector3(0, 1.3f, -1.85f), new Vector3(4.4f, 2.6f, 0.2f));
        Bound(room, "Bound_Left", new Vector3(-2.05f, 1.3f, 0), new Vector3(0.2f, 2.6f, 3.8f));
        Bound(room, "Bound_Right", new Vector3(2.05f, 1.3f, 0), new Vector3(0.2f, 2.6f, 3.8f));

        var spawn = new GameObject("Spawn"); spawn.transform.SetParent(room, false);
        spawn.transform.localPosition = new Vector3(-0.20f, 0.1f, -0.30f);   // open floor, on the rug
        var exit = new GameObject("DoorExit"); exit.transform.SetParent(room, false);
        exit.transform.localPosition = new Vector3(-1.75f, 1.0f, -0.85f);
        var ec = exit.AddComponent<BoxCollider>(); ec.isTrigger = true; ec.size = new Vector3(0.9f, 2.0f, 0.9f);

        // ── LIGHTING RIG: warm-morning 4-light (replaces the single flat point). ──
        // key: directional sun raking in through the window, soft shadows = HD-2D form
        var keyGo = new GameObject("KeySun"); keyGo.transform.SetParent(room, false);
        keyGo.transform.rotation = Quaternion.Euler(32f, 205f, 0f);
        var key = keyGo.AddComponent<Light>(); key.type = LightType.Directional;
        key.color = Hx("#FFF1DE"); key.intensity = 1.35f; key.shadows = LightShadows.Soft; key.shadowStrength = 0.55f;
        // fill: cool sky bounce at the window plane (point stands in for a rect-area)
        var fillGo = new GameObject("WindowFill"); fillGo.transform.SetParent(room, false);
        fillGo.transform.localPosition = new Vector3(-0.75f, 1.30f, 1.55f);
        var fill = fillGo.AddComponent<Light>(); fill.type = LightType.Point;
        fill.color = Hx("#EDF2FF"); fill.intensity = 0.9f; fill.range = 4.5f;
        // rim: warm back separation
        var rimGo = new GameObject("RimWarm"); rimGo.transform.SetParent(room, false);
        rimGo.transform.localPosition = new Vector3(0.60f, 2.30f, 1.85f);
        var rim = rimGo.AddComponent<Light>(); rim.type = LightType.Point;
        rim.color = Hx("#FFE3BE"); rim.intensity = 0.5f; rim.range = 5f;

        // ── authored camera (C03 overview matching the concept) ──
        var camGo = new GameObject("SceneCam"); camGo.transform.SetParent(room, false);
        camGo.transform.position = new Vector3(-0.35f, 2.90f, -5.20f);
        camGo.transform.rotation = Quaternion.LookRotation(new Vector3(0.05f, 1.10f, 0.40f) - camGo.transform.position, Vector3.up);
        camGo.AddComponent<SceneCam>().fov = 45f;

        // ── mood (warm — the only warm scene) + beats ──
        var mood = new GameObject("MOOD");
        var rm = mood.AddComponent<RoomMood>();
        rm.ambient = Hx("#574636");                 // warm color ambient (NOT default grey)
        rm.fog = new Color(0.30f, 0.26f, 0.24f); rm.fogDensity = 0.004f;
        rm.cameraBackground = new Color(0.12f, 0.10f, 0.11f);
        var beats = mood.AddComponent<PrologueBeats>();
        beats.sceneId = "bedroom"; beats.nextScene = PrologueScenePaths.Walk;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        int n = room.GetComponentsInChildren<MeshRenderer>().Length;
        return $"SC_P0_Bedroom rebuilt to blueprint: {n} meshes + shaft/motes/sky-card + 4-light warm rig + C03 cam";
    }

    // ── procedural FX ──

    // bright warm plane just outside the window so the aperture reads as blazing
    // morning (not a black hole) and the shaft appears to emanate from it
    static void SkyCard(Transform parent, Vector3 pos)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "FX_SkyGlow";
        Object.DestroyImmediate(q.GetComponent<MeshCollider>());
        q.transform.SetParent(parent, false); q.transform.localPosition = pos;
        q.transform.localRotation = Quaternion.Euler(0, 180f, 0);   // face -Z into the room
        q.transform.localScale = new Vector3(2.6f, 2.0f, 1f);
        var m = GetMat("SkyGlow", Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", Hx("#3A2E22")); m.SetFloat("_Smoothness", 0f);
        m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Hx("#FFF3E2") * 2.6f);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; EditorUtility.SetDirty(m);
        q.GetComponent<MeshRenderer>().sharedMaterial = m;
    }

    // the warm volumetric sun shaft: a tapering box from the window down-inward
    // onto the rug, additive-unlit so it reads as light in the air
    static void LightShaft(Transform parent)
    {
        Vector3 nc = new Vector3(-0.75f, 1.50f, 1.55f);                 // near cap at the window
        Vector3 dir = (new Vector3(-0.15f, 0f, -0.40f) - nc).normalized; // aim at the open rug, left of the bed
        Vector3 fc = nc + dir * 2.6f;                                  // far cap lands on the floor
        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
        Vector3 up = Vector3.Cross(dir, right).normalized;
        float nhw = 0.52f, nhh = 0.42f, fhw = 0.78f, fhh = 0.68f;       // narrow beam, not a room-wide sheet
        var v = new Vector3[8];
        v[0] = nc + right * nhw + up * nhh; v[1] = nc - right * nhw + up * nhh; v[2] = nc - right * nhw - up * nhh; v[3] = nc + right * nhw - up * nhh;
        v[4] = fc + right * fhw + up * fhh; v[5] = fc - right * fhw + up * fhh; v[6] = fc - right * fhw - up * fhh; v[7] = fc + right * fhw - up * fhh;
        int[] tri = { 0, 4, 5, 0, 5, 1, 1, 5, 6, 1, 6, 2, 2, 6, 7, 2, 7, 3, 3, 7, 4, 3, 4, 0 };  // 4 sides, no caps
        var mesh = new Mesh { name = "LightShaftMesh", vertices = v, triangles = tri };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var go = new GameObject("FX_LightShaft"); go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
        mr.sharedMaterial = ShaftMat();
    }

    static Material ShaftMat()
    {
        var m = GetMat("Shaft", Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", new Color(1.0f, 0.91f, 0.77f, 0.045f));
        m.SetFloat("_Surface", 1f);                              // transparent
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.One);           // additive
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3100;
        EditorUtility.SetDirty(m); return m;
    }

    // slow dust suspended in the shaft (play-mode only; harmless in edit)
    static void DustMotes(Transform parent)
    {
        var go = new GameObject("FX_DustMotes"); go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(-0.40f, 1.20f, 0.90f);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main; main.startSize = 0.014f; main.startLifetime = 11f; main.startSpeed = 0.01f;
        main.startColor = new Color(1f, 0.955f, 0.886f, 0.5f); main.maxParticles = 70;
        main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = -0.01f;
        var emission = ps.emission; emission.rateOverTime = 7f;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(2.4f, 1.8f, 2.2f);
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.05f; noise.frequency = 0.2f;
        var psr = go.GetComponent<ParticleSystemRenderer>();
        var m = GetMat("Dust", Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", new Color(1f, 0.957f, 0.886f, 0.35f));
        m.SetFloat("_Surface", 1f); m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.One);
        m.SetFloat("_ZWrite", 0f); m.SetOverrideTag("RenderType", "Transparent"); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3100;
        EditorUtility.SetDirty(m); psr.sharedMaterial = m;
    }

    // ── helpers ──
    static Color Hx(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

    static void Bound(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.AddComponent<BoxCollider>().size = size;
    }

    static void PShadow(Transform parent, string mesh, Vector3 pos, float yaw)
    {
        var go = Place(parent, mesh, pos, yaw, false);
        if (go != null) foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
    }

    static void P(Transform parent, string mesh, Vector3 pos, float yaw, bool solid = false)
        => Place(parent, mesh, pos, yaw, solid);

    static GameObject Place(Transform parent, string mesh, Vector3 pos, float yaw, bool solid)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{kKit}/{mesh}.obj");
        if (src == null) { Debug.LogWarning($"[Bedroom] missing {mesh}"); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
        go.name = mesh; go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        var mat = MatFor(mesh);
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
        return go;
    }

    static Material GetMat(string key, Shader shader)
    {
        var path = $"{kMatDir}/M_P0_{key}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { System.IO.Directory.CreateDirectory(kMatDir); m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        if (m.shader != shader) m.shader = shader;
        return m;
    }

    // flat base-colour materials keyed by mesh name (blueprint palette ramps;
    // shading comes from the real light rig, HD-2D style)
    static Material MatFor(string mesh)
    {
        string key; string hex; bool emiss = false; string ehex = null; float k = 0; float trans = 0;
        if (mesh.Contains("Floor")) { key = "WoodFloor"; hex = "#9A6A44"; }
        else if (mesh.Contains("Wall") || mesh.Contains("Ceiling") || mesh.Contains("Baseboard")) { key = "Wall"; hex = "#D8C9BC"; }
        else if (mesh.Contains("Rug")) { key = "Rug"; hex = "#86525A"; }
        else if (mesh.Contains("Curtain_Sheer")) { key = "Sheer"; hex = "#E8DECF"; }
        else if (mesh.Contains("Curtain")) { key = "CurtainD"; hex = "#6E5A50"; }
        else if (mesh.Contains("Bedding") || mesh.Contains("Pillow")) { key = "Bedding"; hex = "#7E8A9A"; }
        else if (mesh.Contains("Glass")) { key = "Glass"; hex = "#DDEBF2"; trans = 0.22f; emiss = true; ehex = "#FFF3E2"; k = 0.6f; }
        else if (mesh.Contains("Phone")) { key = "PhoneScr"; hex = "#2A2530"; emiss = true; ehex = "#C8D8FF"; k = 1.4f; }
        else if (mesh.Contains("Desk_Lamp")) { key = "LampBulb"; hex = "#4A4450"; emiss = true; ehex = "#FFE3B0"; k = 1.8f; }
        else if (mesh.Contains("Plant")) { key = "Foliage"; hex = "#4A6A3C"; }
        else if (mesh.Contains("Mug")) { key = "Ceramic"; hex = "#C7BEB2"; }
        else if (mesh.Contains("Metal") || mesh.Contains("Radiator") || mesh.Contains("Hanger") || mesh.Contains("Power") || mesh.Contains("Clock")) { key = "Metal"; hex = "#6E7A85"; }
        else if (mesh.Contains("Notebook") || mesh.Contains("Tissues") || mesh.Contains("Books")) { key = "Paper"; hex = "#E8E0D2"; }
        else if (mesh.Contains("Picture")) { key = "Frame"; hex = "#7A4E30"; }
        else if (mesh.Contains("Trash") || mesh.Contains("Pen")) { key = "Plastic"; hex = "#2A2530"; }
        else if (mesh.Contains("Jacket") || mesh.Contains("Bag")) { key = "Fabric"; hex = "#4A3F52"; }
        else { key = "WoodFurn"; hex = "#7A4E30"; }   // desk/chair/bed/wardrobe/bookcase/shelves/door/frame

        var m = GetMat(key, Shader.Find("Universal Render Pipeline/Lit"));
        var col = Hx(hex); if (trans > 0) col.a = trans;
        m.SetColor("_BaseColor", col); m.SetFloat("_Smoothness", 0.05f); m.SetFloat("_Metallic", 0f);
        if (trans > 0)
        {
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f); m.SetOverrideTag("RenderType", "Transparent"); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000;
        }
        if (emiss) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Hx(ehex) * k); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
        EditorUtility.SetDirty(m);
        return m;
    }
}
