// UN-HUMANITY — SC_P0_Bedroom, built from the REAL furnished kit (Prologue
// Asset Kit, 2026-08-04) instead of gray-box cubes. Places the imported
// OBJ meshes at authored transforms per the SC_P0_BEDROOM blueprint
// (4.20 × 3.60 m room, 2.60 m ceiling, warm morning), assigns flat palette
// materials (meshes ship unpainted), and sets the spawn / door / mood /
// authored C01 camera. Re-runnable.
//   unity command eval "return BedroomLayout.Build();"

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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

        // ── architecture (room 4.20 × 3.60, ceiling 2.60) ──
        // dollhouse cutaway: the front wall + ceiling are OMITTED so the
        // authored camera (in front of the room, -Z) can see the whole space
        // — a 5 m room can't be framed from a camera sealed inside it.
        P(room, "SM_P0_Floor_A",       new Vector3(0, 0, 0),        0);
        P(room, "SM_P0_Wall_A",        new Vector3(0, 0, 1.80f),    0);      // back (window wall)
        P(room, "SM_P0_Wall_B",        new Vector3(-2.10f, 0, 0),   90);     // left (door wall)
        P(room, "SM_P0_Wall_B",        new Vector3(2.10f, 0, 0),    90);     // right
        P(room, "SM_P0_Door_A",        new Vector3(-2.02f, 0, -0.55f), 90);  // door on the left wall

        // window + curtains on the back wall (sill 0.85)
        P(room, "SM_P0_Window_Frame_A", new Vector3(-0.55f, 0.85f, 1.74f), 0);
        P(room, "SM_P0_Window_Glass_A", new Vector3(-0.55f, 0.85f, 1.73f), 0);
        P(room, "SM_P0_Curtain_Sheer_A", new Vector3(-1.00f, 0, 1.68f), 0);
        P(room, "SM_P0_Curtain_Sheer_A", new Vector3(-0.10f, 0, 1.68f), 0);
        P(room, "SM_P0_Curtain_Dark_A",  new Vector3(-1.45f, 0, 1.66f), 0);
        P(room, "SM_P0_Curtain_Dark_A",  new Vector3(0.25f, 0, 1.66f), 0);

        // desk under the window + chair + shelves  (solid = walk-blocking)
        P(room, "SM_P0_Desk_A",        new Vector3(-1.15f, 0, 1.35f), 0, true);
        P(room, "SM_P0_Chair_A",       new Vector3(-1.10f, 0, 0.70f), 180, true);
        P(room, "SM_P0_Wall_Shelf_A",  new Vector3(-1.05f, 1.85f, 1.72f), 0);
        P(room, "SM_P0_Wall_Shelf_B",  new Vector3(0.55f, 2.00f, 1.72f), 0);
        P(room, "SM_P0_Bookcase_A",    new Vector3(1.72f, 0, -1.05f), 90, true);   // right wall, front
        P(room, "SM_P0_Radiator_A",    new Vector3(0.55f, 0.30f, 1.70f), 0);

        // bed along the right wall, head to the back
        P(room, "SM_P0_Bed_Frame_A",   new Vector3(1.45f, 0, 0.30f), 0, true);
        P(room, "PRP_P0_Bedding_Set_A", new Vector3(1.45f, 0.36f, 0.28f), 0);
        P(room, "PRP_P0_Pillow_A",     new Vector3(1.45f, 0.50f, 1.05f), 0);

        // wardrobe on the left wall, front
        P(room, "SM_P0_Wardrobe_A",    new Vector3(-1.55f, 0, -1.05f), 90, true);
        P(room, "PRP_P0_Clothes_Hanger_A", new Vector3(-1.35f, 1.55f, -0.70f), 90);
        P(room, "PRP_P0_Uniform_Jacket_A", new Vector3(-1.30f, 1.10f, -0.70f), 90);

        // rug + ceiling lamp
        P(room, "SM_P0_Rug_Rect_A",    new Vector3(0.1f, 0.01f, 0.05f), 0);
        P(room, "SM_P0_Ceiling_Lamp_A", new Vector3(0, 2.45f, 0.1f), 0);

        // desk props
        P(room, "PRP_P0_Desk_Lamp_A",  new Vector3(-1.62f, 0.74f, 1.45f), 20);
        P(room, "PRP_P0_Notebook_A",   new Vector3(-1.05f, 0.74f, 1.30f), 8);
        P(room, "PRP_P0_Pen_A",        new Vector3(-0.95f, 0.75f, 1.24f), 0);
        P(room, "PRP_P0_Mug_A",        new Vector3(-0.78f, 0.74f, 1.42f), 0);
        P(room, "PRP_P0_Books_Stack_A", new Vector3(-1.55f, 0.74f, 1.28f), 0);
        P(room, "PRP_P0_Tissues_A",    new Vector3(-0.60f, 0.74f, 1.48f), 0);
        P(room, "PRP_P0_Phone_Smart_A", new Vector3(-0.72f, 0.755f, 1.30f), 12);   // the summons phone
        P(room, "PRP_P0_Power_Strip_A", new Vector3(-1.45f, 0.03f, 1.55f), 0);

        // shelf props
        P(room, "PRP_P0_Clock_Analog_A", new Vector3(0.25f, 1.86f, 1.73f), 0);
        P(room, "PRP_P0_Picture_Frame_A", new Vector3(0.85f, 1.55f, 1.75f), 0);
        P(room, "PRP_P0_Plant_Small_A", new Vector3(0.55f, 2.12f, 1.72f), 0);
        P(room, "PRP_P0_Books_Stack_A", new Vector3(-1.05f, 2.00f, 1.72f), 14);

        // floor clutter
        P(room, "PRP_P0_School_Bag_A", new Vector3(0.35f, 0, -0.35f), 25);
        P(room, "PRP_P0_Trash_Bin_A",  new Vector3(-1.92f, 0, 0.95f), 0);

        // ── ground collider + boundary walls (keep the player IN the room —
        // the front wall is a visual cutaway but still needs an invisible
        // collider or you run out the open side and fall) ──
        var ground = new GameObject("Ground"); ground.transform.SetParent(room, false);
        var gc = ground.AddComponent<BoxCollider>(); gc.center = new Vector3(0, -0.5f, 0); gc.size = new Vector3(4.4f, 1f, 3.8f);
        Bound(room, "Bound_Back",  new Vector3(0, 1.3f, 1.85f),  new Vector3(4.4f, 2.6f, 0.2f));
        Bound(room, "Bound_Front", new Vector3(0, 1.3f, -1.85f), new Vector3(4.4f, 2.6f, 0.2f));
        Bound(room, "Bound_Left",  new Vector3(-2.05f, 1.3f, 0), new Vector3(0.2f, 2.6f, 3.8f));
        Bound(room, "Bound_Right", new Vector3(2.05f, 1.3f, 0),  new Vector3(0.2f, 2.6f, 3.8f));

        var spawn = new GameObject("Spawn"); spawn.transform.SetParent(room, false);
        spawn.transform.localPosition = new Vector3(0.7f, 0.1f, 0.1f);       // beside the bed

        var exit = new GameObject("DoorExit"); exit.transform.SetParent(room, false);
        exit.transform.localPosition = new Vector3(-1.75f, 1.0f, -0.55f);    // at the left-wall door
        var ec = exit.AddComponent<BoxCollider>(); ec.isTrigger = true; ec.size = new Vector3(0.9f, 2.0f, 0.9f);

        // morning key light (warm), plus the window bleeding light
        var keyGo = new GameObject("BedroomKey"); keyGo.transform.SetParent(room, false);
        keyGo.transform.localPosition = new Vector3(-0.6f, 2.2f, 1.2f);
        var kl = keyGo.AddComponent<Light>(); kl.type = LightType.Point;
        kl.color = new Color(1f, 0.92f, 0.80f); kl.intensity = 2.4f; kl.range = 9f;

        // ── authored camera — warm 3/4 from in front of the open wall ──
        var camGo = new GameObject("SceneCam"); camGo.transform.SetParent(room, false);
        camGo.transform.position = new Vector3(0.3f, 2.75f, -4.4f);
        camGo.transform.rotation = Quaternion.LookRotation(new Vector3(-0.1f, 0.9f, 0.6f) - camGo.transform.position, Vector3.up);
        camGo.AddComponent<SceneCam>().fov = 46f;

        // ── mood (warm — the only warm scene in the prologue) + beats ──
        var mood = new GameObject("MOOD");
        var rm = mood.AddComponent<RoomMood>();
        rm.ambient = new Color(0.42f, 0.36f, 0.33f);
        rm.fog = new Color(0.30f, 0.26f, 0.24f); rm.fogDensity = 0.004f;
        rm.cameraBackground = new Color(0.14f, 0.11f, 0.12f);
        var beats = mood.AddComponent<PrologueBeats>();
        beats.sceneId = "bedroom"; beats.nextScene = PrologueScenePaths.Walk;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        int n = room.GetComponentsInChildren<MeshRenderer>().Length;
        return $"SC_P0_Bedroom furnished: {n} meshes placed + spawn/door/warm mood/C01 cam";
    }

    // an invisible boundary collider (keeps the player inside the room)
    static void Bound(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var c = go.AddComponent<BoxCollider>(); c.size = size;
    }

    // instantiate an imported kit mesh, place it, flat-material it by name.
    // solid = give it a box collider fitted to its bounds (walk-blocking).
    static void P(Transform parent, string mesh, Vector3 pos, float yaw, bool solid = false)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{kKit}/{mesh}.obj");
        if (src == null) { Debug.LogWarning($"[Bedroom] missing {mesh}"); return; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
        go.name = mesh;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
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
        if (solid && !first)
        {
            var bc = go.AddComponent<BoxCollider>();
            bc.center = b.center; bc.size = b.size;   // local-space mesh bounds
        }
    }

    // flat Momoiro-warm materials, chosen by mesh-name keyword
    static Material MatFor(string mesh)
    {
        string key; Color c; bool emiss = false; float k = 0;
        if (mesh.Contains("Floor")) { key = "Floor"; c = new Color(0.46f, 0.33f, 0.24f); }
        else if (mesh.Contains("Wall") || mesh.Contains("Ceiling") || mesh.Contains("Baseboard")) { key = "Wall"; c = new Color(0.80f, 0.75f, 0.72f); }
        else if (mesh.Contains("Rug")) { key = "Rug"; c = new Color(0.52f, 0.32f, 0.31f); }
        else if (mesh.Contains("Curtain_Sheer")) { key = "Sheer"; c = new Color(0.88f, 0.85f, 0.82f); }
        else if (mesh.Contains("Curtain")) { key = "CurtainD"; c = new Color(0.40f, 0.34f, 0.37f); }
        else if (mesh.Contains("Bedding") || mesh.Contains("Pillow")) { key = "Bedding"; c = new Color(0.50f, 0.55f, 0.63f); }
        else if (mesh.Contains("Glass")) { key = "Glass"; c = new Color(0.94f, 0.90f, 0.80f); emiss = true; k = 1.6f; }
        else if (mesh.Contains("Phone") || mesh.Contains("Lamp")) { key = "Emiss"; c = new Color(0.85f, 0.80f, 0.70f); emiss = true; k = 1.2f; }
        else if (mesh.Contains("Plant")) { key = "Foliage"; c = new Color(0.30f, 0.44f, 0.28f); }
        else if (mesh.Contains("Mug")) { key = "Ceramic"; c = new Color(0.82f, 0.80f, 0.80f); }
        else if (mesh.Contains("Metal") || mesh.Contains("Radiator") || mesh.Contains("Hanger") || mesh.Contains("Power")) { key = "Metal"; c = new Color(0.55f, 0.55f, 0.58f); }
        else if (mesh.Contains("Notebook") || mesh.Contains("Tissues") || mesh.Contains("Picture") || mesh.Contains("Books") || mesh.Contains("Clock")) { key = "Paper"; c = new Color(0.88f, 0.85f, 0.82f); }
        else if (mesh.Contains("Jacket") || mesh.Contains("Bag")) { key = "Fabric"; c = new Color(0.34f, 0.30f, 0.36f); }
        else { key = "Wood"; c = new Color(0.50f, 0.36f, 0.26f); }

        var path = $"{kMatDir}/M_P0_{key}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            System.IO.Directory.CreateDirectory(kMatDir);
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0.05f);
        if (emiss) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * k); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
        EditorUtility.SetDirty(m);
        return m;
    }
}
