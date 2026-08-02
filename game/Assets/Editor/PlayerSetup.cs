// UN-HUMANITY — builds the gray-box player (billboard sprite per Guide011's
// sprite rules) and the Octopath diorama follow rig. One-shot, re-runnable.
//   unity command eval "return PlayerSetup.Build();"

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PlayerSetup
{
    const string kScene   = "Assets/Scenes/SC_02_StreetBlock.unity";
    const string kSprite  = "Assets/Art/Sprites/SPR_Player_Placeholder.png";
    const string kMat     = "Assets/Art/Sprites/M_Sprite_Player.mat";
    const string kPrefab  = "Assets/Art/Sprites/Player.prefab";

    public static string Build()
    {
        // sprite import per Guide011: point filter, no compression, no mips
        var ti = (TextureImporter)AssetImporter.GetAtPath(kSprite);
        if (ti == null) return "FAILED: sprite not found at " + kSprite;
        ti.textureType = TextureImporterType.Default; // used on a lit quad, not SpriteRenderer
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.SaveAndReimport();

        // lit cutout material — sprites are lit and cast shadows (Guide011)
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(kSprite);
        var mat = AssetDatabase.LoadAssetAtPath<Material>(kMat);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, kMat);
        }
        mat.SetTexture("_BaseMap", tex);
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0f);
        mat.SetFloat("_SpecularHighlights", 0f);
        mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        mat.SetFloat("_AlphaClip", 1f);
        mat.SetFloat("_Cutoff", 0.5f);
        mat.EnableKeyword("_ALPHATEST_ON");
        // sheet is 4x2 — default view = idle frame 0 (bottom-left cell)
        mat.SetTextureScale("_BaseMap", new Vector2(0.25f, 0.5f));
        mat.SetTextureOffset("_BaseMap", Vector2.zero);
        EditorUtility.SetDirty(mat);

        // player prefab: controller root + leaning billboard quad
        var old = AssetDatabase.LoadAssetAtPath<GameObject>(kPrefab);
        if (old != null) AssetDatabase.DeleteAsset(kPrefab);

        var root = new GameObject("Player");
        var cc = root.AddComponent<CharacterController>();
        cc.height = 1.7f; cc.center = new Vector3(0f, 0.85f, 0f); cc.radius = 0.3f;
        cc.stepOffset = 0.5f;   // the kerb is 0.41 m — crossing road<->sidewalk must just work
        var pc = root.AddComponent<PlayerController>();

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "SpriteQuad";
        Object.DestroyImmediate(quad.GetComponent<MeshCollider>());
        quad.transform.SetParent(root.transform, false);
        // 32x54 px @ 32 ppu -> 1.0 x 1.6875 m, feet on the floor
        quad.transform.localScale = new Vector3(1.0f, 1.6875f, 1f);
        quad.transform.localPosition = new Vector3(0f, 1.6875f * 0.5f, 0f);
        var mr = quad.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        quad.AddComponent<BillboardY>();
        pc.spriteQuad = quad.transform;
        var anim = root.AddComponent<SpriteAnimator>();
        anim.controller = pc;
        anim.quad = mr;

        PrefabUtility.SaveAsPrefabAsset(root, kPrefab);
        Object.DestroyImmediate(root);

        // place in the scene at the spawn marker, wire rig + sight
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "Player"))
            Object.DestroyImmediate(g);

        var spawn = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == "01_PlayerSpawn");
        var pos = spawn != null ? spawn.position : new Vector3(-6f, 0.45f, 0.5f);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(kPrefab);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        inst.transform.position = pos + Vector3.up * 0.05f;

        var state = Object.FindFirstObjectByType<SightState>();
        var instPc = inst.GetComponent<PlayerController>();
        instPc.sightState = state;

        var cam = Camera.main;
        string camNote = "no Main Camera";
        if (cam != null)
        {
            var rig = cam.GetComponent<CameraRigFollow>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRigFollow>();
            rig.target = inst.transform;
            rig.sightState = state;
            rig.pitch = 18f;      // Octopath reference: shallow, ground-level intimacy
            rig.distance = 13f;
            cam.fieldOfView = 26f;
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 120f;
            camNote = "rig wired (pitch 18, FOV 26, dist 13)";
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return $"player built at {pos}, sight {(state != null ? "wired" : "MISSING")}, camera: {camNote}";
    }
}
