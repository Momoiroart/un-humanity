// UN-HUMANITY — SC_P3_Handoff: the "one week later" establisher. A quiet,
// damp, dusk street corner outside the UH-001 block — the calm before the
// exam. The prologue rig arrives here from the school, plays the summons over
// this shot, then hands to SC_02 (the bus-stop case). Built from the SC_HANDOFF
// kit (a pre-composed corner block + signage + wet-street props).
//   unity command eval "return HandoffLayout.Build();"

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HandoffLayout
{
    const string kScene = "Assets/Scenes/SC_P3_Handoff.unity";
    const string kKit = "Assets/Art/Prologue/SC_HANDOFF";
    const string kMatDir = "Assets/Art/Prologue/Materials";

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "HANDOFF" || g.name == "MOOD"))
            Object.DestroyImmediate(g);
        var root = new GameObject("HANDOFF").transform;

        // the pre-composed street corner (the establisher backdrop)
        P(root, "SM_H_Street_Corner_Block_A", new Vector3(0.8f, 0, 5f), 0, true);
        // signage — the neon confirms the location, faint hum in the dusk
        P(root, "SM_H_Neon_Sign_UH001_A", new Vector3(2.4f, 2.6f, 2.6f), -20);
        P(root, "SM_H_Signboard_Neon_A", new Vector3(2.2f, 1.4f, 0.6f), -20);
        P(root, "SM_H_Route_Sign_A", new Vector3(-1.9f, 0, 1.2f), 25);          // TO BUS STOP →
        P(root, "SM_H_Stop_Sign_A", new Vector3(-2.4f, 0, -0.4f), 10);
        P(root, "SM_H_Wire_Span_10m", new Vector3(0, 5.0f, 4f), 0);

        // wet-street dressing
        P(root, "PRP_H_Puddle_Decal_A", new Vector3(-0.4f, 0.02f, 1.5f), 0);
        P(root, "PRP_H_Puddle_Decal_A", new Vector3(1.0f, 0.02f, 3.5f), 40);
        P(root, "PRP_H_Grime_Decal_A", new Vector3(0.2f, 0.01f, 0.5f), 0);
        P(root, "PRP_H_Bollard_Wet_A", new Vector3(-1.6f, 0, -0.2f), 0);
        P(root, "PRP_H_Trash_Pile_A", new Vector3(2.0f, 0, 0.2f), 30);
        P(root, "PRP_H_Cable_Drape_A", new Vector3(1.8f, 3.2f, 3f), 0);

        // the player arrives at the near kerb (stays put — the beat is on rails)
        var spawn = new GameObject("Spawn"); spawn.transform.SetParent(root, false);
        spawn.transform.localPosition = new Vector3(-0.2f, 0.1f, -1.0f);

        // ── authored camera: a fixed establisher on the corner + the neon ──
        var camGo = new GameObject("SceneCam"); camGo.transform.SetParent(root, false);
        camGo.transform.position = new Vector3(-1.6f, 1.9f, -4.0f);
        camGo.transform.rotation = Quaternion.LookRotation(new Vector3(0.6f, 1.3f, 3.0f) - camGo.transform.position, Vector3.up);
        camGo.AddComponent<SceneCam>().fov = 50f;

        // ── collision: ground + a loose boundary (player is locked, but keep it safe) ──
        Bound(root, "Ground", new Vector3(0, -0.5f, 2f), new Vector3(12f, 1f, 12f));
        Bound(root, "Bound_Front", new Vector3(0, 1.5f, -2.2f), new Vector3(12f, 3f, 0.4f));

        // ── dusk mood (overcast, damp — the calm before protocol) + beats ──
        var mood = new GameObject("MOOD");
        var rm = mood.AddComponent<RoomMood>();
        rm.ambient = new Color(0.28f, 0.27f, 0.33f);
        rm.fog = new Color(0.32f, 0.32f, 0.40f); rm.fogDensity = 0.020f;
        rm.cameraBackground = new Color(0.16f, 0.16f, 0.24f);
        var beats = mood.AddComponent<PrologueBeats>();
        beats.sceneId = "handoff"; beats.nextScene = PrologueScenePaths.Street;

        // a soft key so the wet street reads under the dusk ambient
        var keyGo = new GameObject("HandoffKey"); keyGo.transform.SetParent(root, false);
        keyGo.transform.rotation = Quaternion.Euler(38f, -20f, 0f);
        var kl = keyGo.AddComponent<Light>(); kl.type = LightType.Directional;
        kl.color = new Color(0.7f, 0.72f, 0.85f); kl.intensity = 0.5f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        int n = root.GetComponentsInChildren<MeshRenderer>().Length;
        return $"SC_P3_Handoff established: {n} meshes + dusk mood + SceneCam + beats(handoff→Street)";
    }

    static void P(Transform parent, string mesh, Vector3 pos, float yaw, bool solid = false)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{kKit}/{mesh}.obj");
        if (src == null) { Debug.LogWarning($"[Handoff] missing {mesh}"); return; }
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
    }

    static void Bound(Transform parent, string name, Vector3 pos, Vector3 size)
    { var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos; var c = go.AddComponent<BoxCollider>(); c.size = size; }

    static Material MatFor(string mesh)
    {
        string key; Color c; bool emiss = false; float k = 0;
        if (mesh.Contains("Neon_Sign_UH001")) { key = "NeonUH"; c = new Color(1.0f, 0.23f, 0.42f); emiss = true; k = 1.6f; }   // UH-001 pink neon
        else if (mesh.Contains("Signboard_Neon")) { key = "NeonSign"; c = new Color(0.95f, 0.40f, 0.55f); emiss = true; k = 1.3f; }
        else if (mesh.Contains("Route_Sign")) { key = "Route"; c = new Color(0.30f, 0.55f, 0.45f); emiss = true; k = 0.4f; }
        else if (mesh.Contains("Stop_Sign")) { key = "Stop"; c = new Color(0.58f, 0.16f, 0.16f); }
        else if (mesh.Contains("Corner_Block")) { key = "Block"; c = new Color(0.32f, 0.29f, 0.30f); }
        else if (mesh.Contains("Puddle")) { key = "Puddle"; c = new Color(0.22f, 0.24f, 0.30f); emiss = true; k = 0.25f; }
        else if (mesh.Contains("Grime")) { key = "Grime"; c = new Color(0.18f, 0.17f, 0.18f); }
        else if (mesh.Contains("Wire") || mesh.Contains("Cable")) { key = "Wire"; c = new Color(0.13f, 0.13f, 0.15f); }
        else if (mesh.Contains("Bollard")) { key = "Bollard"; c = new Color(0.42f, 0.43f, 0.46f); }
        else if (mesh.Contains("Trash")) { key = "Trash"; c = new Color(0.28f, 0.26f, 0.24f); }
        else { key = "Grey"; c = new Color(0.35f, 0.35f, 0.38f); }

        var path = $"{kMatDir}/M_H_{key}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { System.IO.Directory.CreateDirectory(kMatDir); m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", mesh.Contains("Puddle") ? 0.6f : 0.1f);
        if (emiss) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * k); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
        EditorUtility.SetDirty(m);
        return m;
    }
}
