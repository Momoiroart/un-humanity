// UN-HUMANITY — far-end skyline silhouette cards (blueprint asset schedule
// B: "Skyline silhouette card x3"). Closes the void band above the facades
// at the top of the diorama frame — the Octopath backdrop trick. Unlit,
// no shadows, staggered depths for parallax under the follow camera.
//   unity command eval "return SkylineSetup.Build();"

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SkylineSetup
{
    const string kScene = "Assets/Scenes/SC_02_StreetBlock.unity";
    const string kTex   = "Assets/Art/Sprites/T_Skyline_Silhouette.png";
    const string kRoot  = "BACKDROP_Skyline";

    public static string Build()
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(kTex);
        if (ti == null) return "FAILED: skyline texture missing";
        ti.textureType = TextureImporterType.Default;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Repeat; // cards tile it horizontally
        ti.SaveAndReimport();
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(kTex);

        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects().Where(g => g.name == kRoot))
            Object.DestroyImmediate(g);
        var root = new GameObject(kRoot).transform;

        // three depths: nearer = lower + slightly lighter, farther = taller + darker
        var rows = new (float z, float height, float width, float tint, float uTiling)[]
        {
            (54f, 14f, 90f,  0.55f, 3f),
            (66f, 20f, 120f, 0.35f, 4f),
            (80f, 28f, 160f, 0.20f, 5f),
        };
        int i = 0;
        foreach (var (z, height, width, tint, uTiling) in rows)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale("_BaseMap", new Vector2(uTiling, 1f));
            mat.SetColor("_BaseColor", new Color(tint, tint * 0.85f, tint, 1f));
            // alpha-clipped so the sky above the rooftops stays the clear color
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.4f);
            mat.EnableKeyword("_ALPHATEST_ON");
            var matPath = $"Assets/Art/Sprites/M_Skyline_{i}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null) AssetDatabase.DeleteAsset(matPath);
            AssetDatabase.CreateAsset(mat, matPath);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = $"SkylineCard_{i}_z{z:0}";
            Object.DestroyImmediate(quad.GetComponent<MeshCollider>());
            quad.transform.SetParent(root, false);
            // Unity quads face -Z: at the far end looking back down -Z at the camera
            quad.transform.position = new Vector3(0f, height * 0.5f - 1f, z);
            quad.transform.localScale = new Vector3(width, height, 1f);
            var mr = quad.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            i++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return $"skyline backdrop: {i} cards at the far end";
    }
}
