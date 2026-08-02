// UN-HUMANITY — pole props had full-bounds box colliders, so a street
// light's overhead arm became an invisible wall across the kerb. This
// slims their colliders to the actual pole base (found from the mesh's
// low vertices). One-shot, edits the prefab assets in place.
//   unity command eval "return PrefabColliderFix.Build();"

using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PrefabColliderFix
{
    static readonly string[] PoleProps =
    {
        "SM_StreetLight_01", "SM_UtilityPole_01", "SM_BusStopSign_01", "SM_RouteSign_01",
    };

    public static string Build()
    {
        int fixedCount = 0;
        foreach (var name in PoleProps)
        {
            var path = $"Assets/Art/StreetBlock/Prefabs/{name}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // pole base = centroid of vertices near the floor
                Vector3 sum = Vector3.zero; int n = 0; float topY = 0f;
                foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh = mf.sharedMesh;
                    if (mesh == null) continue;
                    foreach (var v in mesh.vertices)
                    {
                        var w = mf.transform.TransformPoint(v);
                        topY = Mathf.Max(topY, w.y);
                        if (w.y < 0.6f) { sum += w; n++; }
                    }
                }
                if (n == 0) continue;
                var basePos = sum / n;

                var col = root.GetComponent<BoxCollider>();
                if (col == null) col = root.AddComponent<BoxCollider>();
                col.center = new Vector3(basePos.x, topY * 0.5f, basePos.z);
                col.size = new Vector3(0.5f, topY, 0.5f);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                fixedCount++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        return $"slimmed {fixedCount} pole colliders to their actual poles";
    }
}
