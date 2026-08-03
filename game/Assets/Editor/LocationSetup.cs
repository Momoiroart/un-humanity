// UN-HUMANITY — gray-box narrative LOCATIONS beyond the street. Slice 0
// needs places to tell a story in (a bedroom to wake in, a school to walk
// to). Built procedurally like the street kit; placed far from the street
// block (X += 200) so one scene can hold several stages without collision.
// The prologue teleports the player + camera between them.
//   unity command eval "return LocationSetup.Build();"

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LocationSetup
{
    const string kScene = "Assets/Scenes/SC_02_StreetBlock.unity";
    const string kMatDir = "Assets/Art/StreetBlock/Materials";

    // far-flung anchors — each location is its own island in world space
    public static readonly Vector3 Bedroom = new Vector3(200f, 0f, 0f);
    public static readonly Vector3 School = new Vector3(240f, 0f, 0f);

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "LOCATIONS"))
            Object.DestroyImmediate(g);
        var root = new GameObject("LOCATIONS").transform;

        BuildBedroom(root);
        BuildSchoolGate(root);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "locations built: gray-box bedroom + school gate (offset X 200/240)";
    }

    // ── a small bedroom: floor, three walls, bed, desk, window, door ──
    static void BuildBedroom(Transform parent)
    {
        var room = new GameObject("BEDROOM").transform;
        room.SetParent(parent, false);
        room.position = Bedroom;

        var wall = Mat("M_Room_Wall", new Color(0.34f, 0.30f, 0.33f));   // muted mauve-grey
        var floor = Mat("M_Room_Floor", new Color(0.28f, 0.24f, 0.22f)); // warm boards
        var sheet = Mat("M_Room_Bed", new Color(0.42f, 0.30f, 0.34f));   // bedding
        var wood = Mat("M_Room_Wood", new Color(0.32f, 0.26f, 0.20f));
        var glass = MatEmissive("M_Room_Window", new Color(0.88f, 0.82f, 0.72f), 1.6f); // morning outside

        Box(room, "Floor", new Vector3(0, -0.05f, 0), new Vector3(5f, 0.1f, 5f), floor);
        Box(room, "Wall_Back", new Vector3(0, 1.5f, 2.5f), new Vector3(5f, 3f, 0.1f), wall);
        Box(room, "Wall_Left", new Vector3(-2.5f, 1.5f, 0), new Vector3(0.1f, 3f, 5f), wall);
        Box(room, "Wall_Right", new Vector3(2.5f, 1.5f, 0), new Vector3(0.1f, 3f, 5f), wall);
        // bed against the left wall
        Box(room, "Bed", new Vector3(-1.4f, 0.35f, -0.6f), new Vector3(1.6f, 0.5f, 2.6f), sheet);
        Box(room, "Pillow", new Vector3(-1.4f, 0.62f, -1.6f), new Vector3(1.4f, 0.25f, 0.7f), wall);
        // desk against the back wall
        Box(room, "Desk", new Vector3(1.3f, 0.75f, 2.1f), new Vector3(1.6f, 0.1f, 0.7f), wood);
        Box(room, "Desk_Leg_a", new Vector3(0.6f, 0.375f, 2.1f), new Vector3(0.1f, 0.75f, 0.1f), wood);
        Box(room, "Desk_Leg_b", new Vector3(2.0f, 0.375f, 2.1f), new Vector3(0.1f, 0.75f, 0.1f), wood);
        // window on the back wall (morning bleeding in)
        Box(room, "Window", new Vector3(-0.8f, 1.7f, 2.44f), new Vector3(1.4f, 1.2f, 0.06f), glass);
        // door on the right wall
        Box(room, "Door", new Vector3(2.44f, 1.1f, -1.6f), new Vector3(0.08f, 2.2f, 1.1f), wood);

        // a warm interior key light, local to the room
        var lightGo = new GameObject("BedroomLight");
        lightGo.transform.SetParent(room, false);
        lightGo.transform.localPosition = new Vector3(-0.8f, 2.6f, 1.5f);
        var lt = lightGo.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.color = new Color(1f, 0.9f, 0.78f);
        lt.intensity = 2.4f;
        lt.range = 12f;

        // a spawn marker where the player wakes (by the bed, facing the door)
        var spawn = new GameObject("Spawn_Bedroom");
        spawn.transform.SetParent(room, false);
        spawn.transform.localPosition = new Vector3(-0.6f, 0.05f, -0.6f);
    }

    // ── a school gate: a facade wall + a gap + railings (walk-to target) ──
    static void BuildSchoolGate(Transform parent)
    {
        var loc = new GameObject("SCHOOL_GATE").transform;
        loc.SetParent(parent, false);
        loc.position = School;

        var brick = Mat("M_School_Brick", new Color(0.40f, 0.32f, 0.30f));
        var rail = Mat("M_School_Rail", new Color(0.30f, 0.32f, 0.36f));
        var ground = Mat("M_School_Ground", new Color(0.30f, 0.29f, 0.30f));

        Box(loc, "Ground", new Vector3(0, -0.05f, 0), new Vector3(16f, 0.1f, 10f), ground);
        Box(loc, "Facade_L", new Vector3(-5f, 2.5f, 4f), new Vector3(6f, 5f, 0.6f), brick);
        Box(loc, "Facade_R", new Vector3(5f, 2.5f, 4f), new Vector3(6f, 5f, 0.6f), brick);
        // gate posts + railings across the gap
        Box(loc, "Post_L", new Vector3(-1.6f, 1.6f, 4f), new Vector3(0.4f, 3.2f, 0.4f), rail);
        Box(loc, "Post_R", new Vector3(1.6f, 1.6f, 4f), new Vector3(0.4f, 3.2f, 0.4f), rail);
        for (int i = 0; i < 6; i++)
            Box(loc, $"Rail_{i}", new Vector3(-1.4f + i * 0.55f, 1.3f, 4f), new Vector3(0.08f, 2.6f, 0.08f), rail);

        var spawn = new GameObject("Spawn_School");
        spawn.transform.SetParent(loc, false);
        spawn.transform.localPosition = new Vector3(0f, 0.05f, -3f);
    }

    // ── helpers ──
    static void Box(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static Material Mat(string name, Color c)
    {
        var path = $"{kMatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material MatEmissive(string name, Color c, float intensity)
    {
        var m = Mat(name, c);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * intensity);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(m);
        return m;
    }
}
