// UN-HUMANITY — builds the combat-FX rig: a camera-facing quad floating
// at the manifested stop, four strip materials (Flash / Impact / Glitch /
// Order). Re-runnable.
//   unity command eval "return CombatFxSetup.Build();"

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CombatFxSetup
{
    const string kScene = "Assets/Scenes/SC_02_StreetBlock.unity";
    const string kDir = "Assets/Art/Sprites";
    static readonly string[] kFx = { "Flash", "Impact", "Glitch", "Order" };

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "COMBAT_FX"))
            Object.DestroyImmediate(g);

        var root = new GameObject("COMBAT_FX");
        var fx = root.AddComponent<CombatFx>();

        var quadGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadGo.name = "FxQuad";
        Object.DestroyImmediate(quadGo.GetComponent<MeshCollider>());
        quadGo.transform.SetParent(root.transform, false);
        quadGo.transform.position = new Vector3(0f, 1.5f, 46.2f);   // over the stop
        quadGo.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
        quadGo.AddComponent<BillboardY>();
        fx.quad = quadGo.GetComponent<MeshRenderer>();
        fx.quad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var sheets = new List<CombatFx.Fx>();
        foreach (var name in kFx)
        {
            var texPath = $"{kDir}/FX_{name}.png";
            var ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Default;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();

            var matPath = $"{kDir}/M_FX_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            mat.SetTextureScale("_BaseMap", new Vector2(0.25f, 1f));
            mat.SetFloat("_Surface", 1f);   // transparent
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            EditorUtility.SetDirty(mat);
            sheets.Add(new CombatFx.Fx { name = name, mat = mat });
        }
        fx.sheets = sheets.ToArray();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"combat FX rig built: {sheets.Count}/4 sheets at the stop";
    }
}
