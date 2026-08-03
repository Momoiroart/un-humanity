// UN-HUMANITY — builds SC_P0_Bedroom in its OWN scene: a gray-box room at
// origin with a warm local mood, the wake spawn, the door-exit trigger,
// and this scene's PrologueBeats. No player/camera/UI — the persistent rig
// (SC_Prologue_Core) carries those in.
//   unity command eval "return BedroomLayout.Build();"

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BedroomLayout
{
    const string kScene = "Assets/Scenes/SC_P0_Bedroom.unity";
    const string kMatDir = "Assets/Art/StreetBlock/Materials";

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "BEDROOM" || g.name == "MOOD"))
            Object.DestroyImmediate(g);

        var room = new GameObject("BEDROOM").transform;

        var wall = Mat("M_Room_Wall", new Color(0.34f, 0.30f, 0.33f));
        var floor = Mat("M_Room_Floor", new Color(0.28f, 0.24f, 0.22f));
        var sheet = Mat("M_Room_Bed", new Color(0.42f, 0.30f, 0.34f));
        var wood = Mat("M_Room_Wood", new Color(0.32f, 0.26f, 0.20f));
        var glass = MatEmissive("M_Room_Window", new Color(0.92f, 0.86f, 0.74f), 2.2f);

        Box(room, "Floor", new Vector3(0, -0.05f, 0), new Vector3(5f, 0.1f, 5f), floor);
        Box(room, "Wall_Back", new Vector3(0, 1.5f, 2.5f), new Vector3(5f, 3f, 0.1f), wall);
        Box(room, "Wall_Left", new Vector3(-2.5f, 1.5f, 0), new Vector3(0.1f, 3f, 5f), wall);
        Box(room, "Wall_Right_a", new Vector3(2.5f, 1.5f, 1.1f), new Vector3(0.1f, 3f, 2.8f), wall);  // door gap between
        Box(room, "Wall_Right_b", new Vector3(2.5f, 1.5f, -2.05f), new Vector3(0.1f, 3f, 0.9f), wall);
        Box(room, "Wall_Front", new Vector3(0, 1.5f, -2.5f), new Vector3(5f, 3f, 0.1f), wall);
        Box(room, "Ceiling", new Vector3(0, 3.0f, 0), new Vector3(5f, 0.1f, 5f), wall);
        // bed against the left wall — spawn is here
        Box(room, "Bed", new Vector3(-1.4f, 0.35f, -0.6f), new Vector3(1.6f, 0.5f, 2.6f), sheet);
        Box(room, "Pillow", new Vector3(-1.4f, 0.62f, -1.6f), new Vector3(1.4f, 0.25f, 0.7f), wall);
        // desk on the back wall
        Box(room, "Desk", new Vector3(1.3f, 0.75f, 2.1f), new Vector3(1.6f, 0.1f, 0.7f), wood);
        Box(room, "Desk_Leg_a", new Vector3(0.6f, 0.375f, 2.1f), new Vector3(0.1f, 0.75f, 0.1f), wood);
        Box(room, "Desk_Leg_b", new Vector3(2.0f, 0.375f, 2.1f), new Vector3(0.1f, 0.75f, 0.1f), wood);
        // nightstand + phone (kills the alarm; later carries the summons)
        Box(room, "Nightstand", new Vector3(-2.0f, 0.4f, 1.0f), new Vector3(0.7f, 0.8f, 0.7f), wood);
        var phone = MatEmissive("M_Room_Phone", new Color(0.6f, 0.75f, 0.85f), 1.4f);
        Box(room, "Phone", new Vector3(-2.0f, 0.82f, 1.0f), new Vector3(0.3f, 0.04f, 0.5f), phone);
        // window (morning bleeding in) on the back wall
        Box(room, "Window", new Vector3(-0.8f, 1.7f, 2.44f), new Vector3(1.5f, 1.3f, 0.06f), glass);

        // warm interior key light
        var lightGo = new GameObject("BedroomLight");
        lightGo.transform.SetParent(room, false);
        lightGo.transform.localPosition = new Vector3(-0.5f, 2.7f, 1.2f);
        var lt = lightGo.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.color = new Color(1f, 0.9f, 0.78f);
        lt.intensity = 3.0f;
        lt.range = 14f;

        // spawn marker (by the bed) + door-exit trigger
        var spawn = new GameObject("Spawn");
        spawn.transform.SetParent(room, false);
        spawn.transform.localPosition = new Vector3(-0.6f, 0.05f, -0.6f);

        var exit = new GameObject("DoorExit");
        exit.transform.SetParent(room, false);
        exit.transform.localPosition = new Vector3(2.3f, 1.0f, -0.6f);   // the door gap
        var col = exit.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(0.8f, 2.2f, 1.6f);

        // this scene's mood + its beats
        var mood = new GameObject("MOOD");
        var rm = mood.AddComponent<RoomMood>();
        rm.ambient = new Color(0.46f, 0.39f, 0.35f);       // warm morning interior
        rm.fog = new Color(0.30f, 0.26f, 0.26f);
        rm.fogDensity = 0.004f;
        rm.cameraBackground = new Color(0.16f, 0.13f, 0.14f);

        var beats = mood.AddComponent<PrologueBeats>();
        beats.sceneId = "bedroom";
        beats.nextScene = PrologueScenes.Walk;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        int renderers = room.GetComponentsInChildren<MeshRenderer>().Length;
        return $"SC_P0_Bedroom built: {renderers} boxes, spawn + door trigger + warm mood + beats";
    }

    static void Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static Material Mat(string name, Color c)
    {
        var path = $"{kMatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material MatEmissive(string name, Color c, float k)
    {
        var m = Mat(name, c);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * k);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(m);
        return m;
    }
}
