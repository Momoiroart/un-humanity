// UN-HUMANITY — places the gray-box people: witnesses on the street
// (Normalcy), the victim and the Waiter's negative silhouette at the
// manifested stop (SIGHT_STATE only). Lit billboard quads like the player.
//   unity command eval "return NPCSetup.Build();"

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NPCSetup
{
    const string kScene = "Assets/Scenes/SC_02_StreetBlock.unity";
    const string kDir = "Assets/Art/Sprites";

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "NPCS"))
            Object.DestroyImmediate(g);

        // import settings for the new sprites
        foreach (var n in new[] { "SPR_WitnessA", "SPR_WitnessB", "SPR_Victim", "SPR_Waiter" })
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath($"{kDir}/{n}.png");
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Default;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
        }

        var root = new GameObject("NPCS").transform;
        int placed = 0;

        GameObject Billboard(string sprite, Transform parent, Vector3 pos, string name)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{kDir}/{sprite}.png");
            var matPath = $"{kDir}/M_{sprite}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0f);
            mat.SetFloat("_SpecularHighlights", 0f);
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.EnableKeyword("_ALPHATEST_ON");
            EditorUtility.SetDirty(mat);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Sprite";
            Object.DestroyImmediate(quad.GetComponent<MeshCollider>());
            quad.transform.SetParent(go.transform, false);
            quad.transform.localScale = new Vector3(1.0f, 1.6875f, 1f);
            quad.transform.localPosition = new Vector3(0f, 1.6875f * 0.5f, 0f);
            var mr = quad.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            quad.AddComponent<BillboardY>();
            placed++;
            return go;
        }

        // witnesses — people, present in Normalcy, standing at their nodes
        Billboard("SPR_WitnessA", root, new Vector3(-6.9f, 0.411f, 14.4f), "NPC_WitnessA");
        Billboard("SPR_WitnessB", root, new Vector3(6.8f, 0.411f, 24.4f), "NPC_WitnessB");

        // the victim and the Waiter — SIGHT only, at the road-end stop
        var layout = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "LAYOUT_StreetBlock");
        var sightRoot = layout != null
            ? layout.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name.StartsWith("SIGHT_STATE"))?.gameObject
            : null;
        if (sightRoot != null)
        {
            foreach (var old in sightRoot.transform.Cast<Transform>()
                     .Where(t => t.name.StartsWith("NPC_")).ToList())
                Object.DestroyImmediate(old.gameObject);
            Billboard("SPR_Victim", sightRoot.transform, new Vector3(1.2f, 0.02f, 46.6f), "NPC_Victim");
            Billboard("SPR_Waiter", sightRoot.transform, new Vector3(-0.5f, 0.02f, 46.3f), "NPC_TheWaiter");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"NPCs placed: {placed} (witnesses in Normalcy; victim + the Waiter under Sight)"
             + (sightRoot == null ? " WARNING: no sight root" : "");
    }
}
