// UN-HUMANITY — street kit import rules (Guide011.md, "Import settings")
// Applies to everything under Assets/Art/StreetBlock/Meshes.
// Scale factor 1 is VERIFIED for this kit (OBJ in true meters, 2026-08-01).

using UnityEditor;
using UnityEngine;

public class StreetKitImportRules : AssetPostprocessor
{
    const string kMeshRoot = "Assets/Art/StreetBlock/Meshes";
    const string kMatRoot  = "Assets/Art/StreetBlock/Materials";

    bool IsStreetKit => assetPath.Replace('\\', '/').StartsWith(kMeshRoot);

    void OnPreprocessModel()
    {
        if (!IsStreetKit) return;
        var mi = (ModelImporter)assetImporter;
        mi.globalScale        = 1f;                                // verified: kit is true meters
        mi.useFileScale       = true;
        mi.meshCompression    = ModelImporterMeshCompression.Off;
        mi.isReadable         = false;
        mi.importNormals      = ModelImporterNormals.Import;       // hard edges intended
        mi.importTangents     = ModelImporterTangents.None;        // no normal maps anywhere
        mi.addCollider        = false;                             // box colliders authored on prefabs
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        mi.importBlendShapes  = false;
        mi.importCameras      = false;
        mi.importLights       = false;
    }

    // Map each MTL material (SURF_X) to the shared project material (M_X).
    Material OnAssignMaterialModel(Material sourceMaterial, Renderer renderer)
    {
        if (!IsStreetKit) return null;
        string n = sourceMaterial.name;
        if (n.StartsWith("SURF_")) n = n.Substring(5);
        var mat = AssetDatabase.LoadAssetAtPath<Material>($"{kMatRoot}/M_{n}.mat");
        if (mat == null)
            Debug.LogWarning($"[StreetKit] no shared material for '{sourceMaterial.name}' ({assetPath}) — run StreetKitSetup.RunPhase1 first.");
        return mat; // null lets Unity fall back to a default material (visible = fix me)
    }
}
