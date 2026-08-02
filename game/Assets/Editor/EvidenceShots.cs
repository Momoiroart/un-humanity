// UN-HUMANITY — takes the in-engine evidence photographs for the object
// clues (bench, sediment, archive), shot under Sight so the record shows
// what the file describes. People-clues use WITHHELD cards instead.
//   unity command eval "return EvidenceShots.Build();"

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Linq;

public static class EvidenceShots
{
    const string kScene = "Assets/Scenes/SC_02_StreetBlock.unity";
    const string kDir = "Assets/Art/Evidence";

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        Directory.CreateDirectory(Path.GetFullPath(kDir));

        var state = Object.FindFirstObjectByType<SightState>();
        if (state == null) return "no SightState";
        float prevBlend = state.blend;
        state.SetSight(1f);   // photograph the truth

        var camGo = new GameObject("~EvidenceCam");
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 34f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 100f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.03f, 0.05f);
        var rt = new RenderTexture(640, 400, 24);
        cam.targetTexture = rt;
        var tex = new Texture2D(640, 400, TextureFormat.RGB24, false);
        cam.transform.position = new Vector3(0, 3, 38); cam.Render(); cam.Render(); // warm-up

        void Shot(string name, Vector3 pos, Vector3 look)
        {
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.LookRotation(look - pos, Vector3.up);
            cam.Render(); cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 640, 400), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.GetFullPath($"{kDir}/EV_{name}.png"), tex.EncodeToPNG());
        }

        Shot("Bench", new Vector3(-1.8f, 1.35f, 44.6f), new Vector3(0.5f, 0.7f, 46.8f));
        Shot("Sediment", new Vector3(0.2f, 1.9f, 46.2f), new Vector3(1.5f, 0.3f, 47.7f));
        Shot("Archive", new Vector3(-5.0f, 1.55f, 26.9f), new Vector3(-6.9f, 1.45f, 28.1f));

        state.SetSight(prevBlend);
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(camGo);

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        int fixed_ = 0;
        foreach (var p in Directory.GetFiles(Path.GetFullPath(kDir), "EV_*.png")
                 .Select(f => kDir + "/" + Path.GetFileName(f)))
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Default;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
            fixed_++;
        }
        return $"evidence photos shot under Sight; {fixed_} textures imported";
    }
}
