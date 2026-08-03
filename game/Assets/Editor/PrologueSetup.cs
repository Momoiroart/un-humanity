// UN-HUMANITY — builds SLICE 0's rig: the friend NPC near the spawn and
// the PrologueSequence controller wired to story / dialogue / Sight / the
// case HUD. Re-runnable; run AFTER the case + player chain so its refs
// resolve.
//   unity command eval "return PrologueSetup.Build();"

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PrologueSetup
{
    const string kScene = "Assets/Scenes/SC_02_StreetBlock.unity";
    const string kDir = "Assets/Art/Sprites";

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "PROLOGUE"))
            Object.DestroyImmediate(g);

        var root = new GameObject("PROLOGUE").transform;

        // import the friend sprite
        var ti = (TextureImporter)AssetImporter.GetAtPath($"{kDir}/SPR_Friend.png");
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Default;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
        }

        // the friend — a lit billboard quad, a couple metres ahead of spawn
        // (spawn is (0,0.05,3); the stop is at Z≈46, far out of reach)
        var friend = Billboard("SPR_Friend", root, new Vector3(1.6f, 0.411f, 6.5f), "NPC_Friend");

        var seqGo = new GameObject("PrologueSequence");
        seqGo.transform.SetParent(root, false);
        var seq = seqGo.AddComponent<PrologueSequence>();
        seq.story = Object.FindFirstObjectByType<StoryUI>();
        seq.dialogue = Object.FindFirstObjectByType<DialogueUI>();
        seq.sightState = Object.FindFirstObjectByType<SightState>();
        seq.friend = friend;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        bool ok = seq.story != null && seq.dialogue != null && seq.sightState != null;
        return $"Slice 0 rig built: friend placed, sequence wired"
             + (ok ? "" : " WARNING: missing refs (run after case+player chain)");
    }

    static GameObject Billboard(string sprite, Transform parent, Vector3 pos, string name)
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
        mat.SetFloat("_Smoothness", 0f);
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
        quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
        quad.AddComponent<BillboardY>();
        return go;
    }
}
