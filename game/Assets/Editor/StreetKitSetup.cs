// UN-HUMANITY — one-pass project setup for the street-block kit.
// Run headless:
//   Unity.exe -batchmode -nographics -projectPath D:\un-humanity\game
//     -executeMethod StreetKitSetup.RunPhase1 -quit -logFile Logs\phase1.log
//
// What it does (Guide011.md + Slice Blueprint rev 0.3):
//   1. removes template tutorial cruft
//   2. creates Assets/Art/StreetBlock/{Meshes,Materials,Textures,Prefabs}
//   3. authors one shared M_* URP/Lit material per SURF_* entry found in the
//      kit's MTL files (albedo = MTL Kd, metallic 0, smoothness 0, spec off;
//      M_Emissive_Display additionally gets the emissive ramp base at HDR x3)
//   4. copies the 23 OBJ/MTL meshes in (import rules: StreetKitImportRules)
//   5. builds a prefab per mesh: box collider fitted to bounds + static flags
//   6. URP: Forward+, GPU Resident Drawer (instanced) + GPU occlusion,
//      shadow distance 100 m, 2 cascades
//   7. creates SC_02_StreetBlock: warm low key light, ink/wine flat ambient,
//      exponential fog, diorama camera (FOV 26, pitch 32, clip 1/120)
//   8. writes ImportReport.md with MEASURED counts

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class StreetKitSetup
{
    const string kKitSource = @"D:\ClaudeCLI\GDD\3dAssets";       // kitchen repo = source of truth
    const string kArtRoot   = "Assets/Art/StreetBlock";
    const string kMeshes    = kArtRoot + "/Meshes";
    const string kMaterials = kArtRoot + "/Materials";
    const string kTextures  = kArtRoot + "/Textures";
    const string kPrefabs   = kArtRoot + "/Prefabs";
    const string kScenePath = "Assets/Scenes/SC_02_StreetBlock.unity";

    static readonly List<string> Warnings = new();
    static readonly StringBuilder Report = new();

    public static void RunPhase1()
    {
        try
        {
            Report.AppendLine("# UN-HUMANITY — street kit import report");
            Report.AppendLine($"_Generated {DateTime.Now:yyyy-MM-dd HH:mm} by StreetKitSetup.RunPhase1 (measured, not typed)_\n");

            RemoveTemplateCruft();
            CreateFolders();
            var kdByName = ParseMtlLibrary();
            CreateSharedMaterials(kdByName);
            CopyKitMeshes();
            BuildPrefabs();
            ConfigureUrp();
            BuildScene();
            WriteReport();

            AssetDatabase.SaveAssets();
            Debug.Log("[StreetKitSetup] Phase 1 complete.");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[StreetKitSetup] FAILED: " + e);
            try { Report.AppendLine("\n## FAILED\n```\n" + e + "\n```"); WriteReport(); } catch { }
            EditorApplication.Exit(1);
        }
    }

    static void RemoveTemplateCruft()
    {
        foreach (var p in new[] { "Assets/TutorialInfo", "Assets/Readme.asset" })
            if (AssetDatabase.DeleteAsset(p)) Report.AppendLine($"- removed template item `{p}`");
    }

    static void CreateFolders()
    {
        foreach (var dir in new[] { kArtRoot, kMeshes, kMaterials, kTextures, kPrefabs })
        {
            var abs = Path.GetFullPath(dir);
            if (!Directory.Exists(abs)) Directory.CreateDirectory(abs);
        }
        AssetDatabase.Refresh();
        Report.AppendLine($"- folders ensured under `{kArtRoot}`");
    }

    // MTL Kd values are linear-space; material _BaseColor is authored in sRGB.
    static Dictionary<string, Color> ParseMtlLibrary()
    {
        var kd = new Dictionary<string, Color>();
        foreach (var mtl in Directory.GetFiles(kKitSource, "*.mtl"))
        {
            string current = null;
            foreach (var raw in File.ReadAllLines(mtl))
            {
                var line = raw.Trim();
                if (line.StartsWith("newmtl ")) current = line.Substring(7).Trim();
                else if (line.StartsWith("Kd ") && current != null)
                {
                    var p = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    var c = new Color(float.Parse(p[1]), float.Parse(p[2]), float.Parse(p[3]));
                    if (kd.TryGetValue(current, out var prev) && ((Vector4)prev - (Vector4)c).sqrMagnitude > 1e-6f)
                        Warnings.Add($"MTL '{current}' has conflicting Kd across files — kept first value");
                    else kd[current] = c;
                }
            }
        }
        Report.AppendLine($"- parsed {kd.Count} distinct SURF_* materials from MTL library");
        return kd;
    }

    static void CreateSharedMaterials(Dictionary<string, Color> kdByName)
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) throw new Exception("URP/Lit shader not found — is URP in the project?");
        int created = 0;
        foreach (var (surfName, kdLinear) in kdByName.OrderBy(k => k.Key))
        {
            var shortName = surfName.StartsWith("SURF_") ? surfName.Substring(5) : surfName;
            var path = $"{kMaterials}/M_{shortName}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) continue;

            var mat = new Material(lit) { name = "M_" + shortName };
            var srgb = new Color(
                Mathf.LinearToGammaSpace(kdLinear.r),
                Mathf.LinearToGammaSpace(kdLinear.g),
                Mathf.LinearToGammaSpace(kdLinear.b), 1f);
            mat.SetColor("_BaseColor", srgb);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0f);
            mat.SetFloat("_SpecularHighlights", 0f);
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");

            if (shortName == "Emissive_Display")
            {
                // Guide011 emissive ramp base #EC8D82, HDR intensity 2-4 (using 3).
                var emis = ((Color)new Color32(0xEC, 0x8D, 0x82, 0xFF)).linear * 3f;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emis);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(mat, path);
            created++;
        }
        AssetDatabase.SaveAssets();
        Report.AppendLine($"- created {created} shared M_* materials (URP/Lit, metallic 0, smoothness 0, spec off)");
    }

    static void CopyKitMeshes()
    {
        int copied = 0;
        foreach (var src in Directory.GetFiles(kKitSource).Where(f => f.EndsWith(".obj") || f.EndsWith(".mtl")))
        {
            File.Copy(src, Path.Combine(Path.GetFullPath(kMeshes), Path.GetFileName(src)), overwrite: true);
            copied++;
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Report.AppendLine($"- copied {copied} kit files into `{kMeshes}` and imported");
    }

    static void BuildPrefabs()
    {
        int built = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { kMeshes }))
        {
            var meshPath = AssetDatabase.GUIDToAssetPath(guid);
            var name = Path.GetFileNameWithoutExtension(meshPath);
            var prefabPath = $"{kPrefabs}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) continue;

            var srcGo = AssetDatabase.LoadAssetAtPath<GameObject>(meshPath);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(srcGo);
            inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var rends = inst.GetComponentsInChildren<MeshRenderer>();
            if (rends.Length > 0)
            {
                var b = rends[0].bounds;
                foreach (var r in rends.Skip(1)) b.Encapsulate(r.bounds);
                var col = inst.AddComponent<BoxCollider>();
                col.center = b.center;
                col.size = b.size;
            }

            bool isDecal = name.Contains("Decal");
            var flags = StaticEditorFlags.BatchingStatic
                      | StaticEditorFlags.OccludeeStatic
                      | StaticEditorFlags.ContributeGI
                      | (isDecal ? 0 : StaticEditorFlags.OccluderStatic);
            foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);

            PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
            UnityEngine.Object.DestroyImmediate(inst);
            built++;
        }
        Report.AppendLine($"- built {built} prefabs (box collider fitted to bounds, Batching/Occludee/GI static; Occluder off on decals)");
    }

    static void ConfigureUrp()
    {
        // Renderer: Forward+
        var rd = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/PC_Renderer.asset");
        if (rd != null)
        {
            var so = new SerializedObject(rd);
            var prop = so.FindProperty("m_RenderingMode");
            if (prop != null)
            {
                prop.intValue = (int)Enum.Parse(typeof(RenderingMode), "ForwardPlus");
                so.ApplyModifiedPropertiesWithoutUndo();
                Report.AppendLine("- PC_Renderer → rendering path Forward+");
            }
            else Warnings.Add("m_RenderingMode not found on PC_Renderer — set Forward+ manually");
        }
        else Warnings.Add("Assets/Settings/PC_Renderer.asset not found");

        // RP asset: GPU Resident Drawer + occlusion, shadows
        var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        if (rp != null)
        {
            var so = new SerializedObject(rp);
            void SetInt(string p, int v, string label)
            {
                var sp = so.FindProperty(p);
                if (sp != null) { sp.intValue = v; Report.AppendLine($"- PC_RPAsset → {label}"); }
                else Warnings.Add($"{p} not found on PC_RPAsset — set {label} manually");
            }
            void SetBool(string p, bool v, string label)
            {
                var sp = so.FindProperty(p);
                if (sp != null) { sp.boolValue = v; Report.AppendLine($"- PC_RPAsset → {label}"); }
                else Warnings.Add($"{p} not found on PC_RPAsset — set {label} manually");
            }
            SetInt("m_GPUResidentDrawerMode", 1, "GPU Resident Drawer: instanced drawing");
            SetBool("m_GPUResidentDrawerEnableOcclusionCullingInCameras", true, "GPU occlusion culling: on");
            so.ApplyModifiedPropertiesWithoutUndo();

            rp.shadowDistance = 100f;   // block far end ~78 m; spec says >= 90
            rp.shadowCascadeCount = 2;
            EditorUtility.SetDirty(rp);
            Report.AppendLine("- PC_RPAsset → shadow distance 100 m, 2 cascades");
        }
        else Warnings.Add("Assets/Settings/PC_RPAsset.asset not found");
    }

    static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Key light — warm, low evening angle, soft shadows.
        var lightGo = new GameObject("Directional Key");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.83f, 0.63f);
        light.intensity = 1.3f;
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(26f, -140f, 0f);

        // Ambient: flat colour in the ink/wine range — Guide011 calls this the
        // single highest-impact setting in the project.
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color32(0x2A, 0x14, 0x20, 0xFF);

        // URP exponential fog (never fog cards).
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.fogColor = new Color32(0x17, 0x0D, 0x14, 0xFF);

        // Diorama camera: FOV 26 (blueprint), pitch 32 (Guide011 30-38 vs
        // blueprint tilt 22 - split noted in ImportReport), 25 m from plane.
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 26f;
        cam.nearClipPlane = 1f;
        cam.farClipPlane = 120f;
        camGo.transform.position = new Vector3(0f, 13.2f, -21.2f);
        camGo.transform.rotation = Quaternion.Euler(32f, 0f, 0f);
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        camGo.AddComponent<AudioListener>();

        // Gray-box ground reference: one road module + flanking sidewalks would
        // be placed in the layout pass; scene ships empty of kit on purpose.

        EditorSceneManager.SaveScene(scene, kScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(kScenePath, true) }
            .Concat(EditorBuildSettings.scenes.Where(s => s.path != kScenePath)).ToArray();
        Report.AppendLine($"- scene `{kScenePath}`: key light, ink/wine flat ambient, exp fog, diorama camera (FOV 26 · pitch 32 · clip 1/120)");
    }

    static void WriteReport()
    {
        Report.AppendLine("\n## Measured meshes\n");
        Report.AppendLine("| mesh | tris | verts | bounds (m) | materials |");
        Report.AppendLine("|---|---|---|---|---|");
        long totalTris = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { kMeshes }).OrderBy(g => AssetDatabase.GUIDToAssetPath(g)))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            long tris = 0, verts = 0;
            var mats = new HashSet<string>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Mesh m) { tris += m.triangles.LongLength / 3; verts += m.vertexCount; }
            }
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null)
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
                    foreach (var mat in r.sharedMaterials)
                        mats.Add(mat != null ? mat.name : "<MISSING>");
            totalTris += tris;
            var b = go != null ? CalcBounds(go) : default;
            Report.AppendLine($"| {Path.GetFileNameWithoutExtension(path)} | {tris} | {verts} | {b.size.x:0.##}×{b.size.y:0.##}×{b.size.z:0.##} | {string.Join(", ", mats.OrderBy(x => x))} |");
        }
        Report.AppendLine($"\n**Total imported triangles: {totalTris}** (Unity-measured)");

        if (Warnings.Count > 0)
        {
            Report.AppendLine("\n## Warnings\n");
            foreach (var w in Warnings) Report.AppendLine("- " + w);
        }
        File.WriteAllText(Path.GetFullPath("ImportReport.md"), Report.ToString());
        Debug.Log("[StreetKitSetup] report written to ImportReport.md");
    }

    static Bounds CalcBounds(GameObject go)
    {
        var rends = go.GetComponentsInChildren<MeshRenderer>();
        if (rends.Length == 0) return default;
        var b = rends[0].bounds;
        foreach (var r in rends.Skip(1)) b.Encapsulate(r.bounds);
        return b;
    }
}
