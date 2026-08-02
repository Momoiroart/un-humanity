// UN-HUMANITY — SC_02_StreetBlock gray-box layout
// Source of truth: UN-HUMANITY Slice Blueprint rev 0.3 (master plan §1) +
// Guide011.md. Coordinates: X = street cross-section (south négative),
// Z = player travel axis (spawn Z=0 → far end Z=48), Y up. Road surface
// tops out at world Y=0 (modules sit at base −0.212).
//
// Cross-section per the sheet: south facade line | 4 m south sidewalk
// (player navmesh) | 8 m roadway | 4 m north sidewalk | north facade line.
// Sight-only entities live under the DISABLED root "SIGHT_STATE — spawned,
// not hidden" — the runtime state controller instantiates them; in Normalcy
// they must not exist in the active graph (blueprint note, §1).
//
// Run from the connected CLI:
//   unity command eval "StreetBlockLayout.Build(); return \"ok\";"
// Re-runnable: deletes and rebuilds the LAYOUT root each time.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StreetBlockLayout
{
    const string kPrefabs = "Assets/Art/StreetBlock/Prefabs";
    const string kScene   = "Assets/Scenes/SC_02_StreetBlock.unity";
    const string kRoot    = "LAYOUT_StreetBlock";

    static readonly List<string> Log = new();

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);

        foreach (var old in scene.GetRootGameObjects().Where(g => g.name == kRoot))
            Object.DestroyImmediate(old);

        var root = new GameObject(kRoot).transform;

        var env       = Group(root, "ENV");
        var road      = Group(env, "Road");
        var walkS     = Group(env, "Sidewalk_South (player)");
        var walkN     = Group(env, "Sidewalk_North");
        var facadeS   = Group(env, "Facades_South");
        var facadeN   = Group(env, "Facades_North");
        var props     = Group(root, "PROPS_Normalcy");
        var markers   = Group(root, "MARKERS");
        var sight     = Group(root, "SIGHT_STATE — spawned, not hidden");

        // ── Roadway: 6 × RoadModule_8x8 down the corridor ──
        for (int i = 0; i < 6; i++)
            Place(road, "SM_RoadModule_8x8", new Vector3(-4f, -0.212f, i * 8f), 0);

        // 20 · crosswalk decal set near spawn, 6 mm above the road surface
        Place(road, "SM_CrosswalkDecal_Set", new Vector3(-2.7f, 0.006f, 1f), 0, "20_CrosswalkDecals");

        // ── Kerbs + gutters, both edges, 12 × 4 m each ──
        for (int i = 0; i < 12; i++)
        {
            float z = i * 4f;
            Place(walkS, "SM_Curb_Straight_4m",  new Vector3(-4.311f, -0.212f, z), 0);
            Place(walkN, "SM_Curb_Straight_4m",  new Vector3( 4.000f, -0.212f, z), 180);
            Place(road,  "SM_GutterChannel_4m",  new Vector3(-4.000f, -0.212f, z), 0);
            Place(road,  "SM_GutterChannel_4m",  new Vector3( 3.500f, -0.212f, z), 180);
        }

        // ── Sidewalk slabs (tops flush with kerb at Y≈0.411) ──
        // Full 4 m width BOTH sides per the sheet; facade fronts sit on the
        // outermost slab row, so kerb → slabs → building line is continuous.
        for (int i = 0; i < 12; i++)
        {
            float z = i * 4f;
            foreach (float x in new[] { -8.3f, -7.3f, -6.3f, -5.3f })
                Place(walkS, "SM_SidewalkSlab_1x4", new Vector3(x, 0.255f, z), 0);
            foreach (float x in new[] { 4.311f, 5.311f, 6.311f, 7.311f })
                Place(walkN, "SM_SidewalkSlab_1x4", new Vector3(x, 0.255f, z), 0);
        }

        // ── Facade lines: alternate A (shop) / B (flats); north keeps a gap
        //    at Z 32-36 for 14 · the blocked maintenance alley ──
        // Fronts aligned to the building line (A and B differ in depth, so
        // anchor the street-facing face, not the back).
        for (int i = 0; i < 12; i++)
        {
            float z = i * 4f;
            string mesh = (i % 2 == 0) ? "SM_FacadeModule_A_Shop" : "SM_FacadeModule_B_Flats";
            var s = Place(facadeS, mesh, new Vector3(-8.90f, 0f, z), 90);
            AlignX(s, -8.03f, alignMax: true);   // front face flush at -8.03
            if (i != 8) // alley slot on the north line
            {
                var n = Place(facadeN, (i % 2 == 0) ? "SM_FacadeModule_B_Flats" : "SM_FacadeModule_A_Shop",
                      new Vector3(8.03f, 0f, z), -90);
                AlignX(n, 8.03f, alignMax: false); // front face flush at +8.03
            }
        }

        // ── Numbered street props (Normalcy state) ──
        // 02 · streetlights ×3, south kerb line, arm over the road
        foreach (float z in new[] { 8f, 24f, 40f })
            PlaceCenter(props, "SM_StreetLight_01", new Vector2(-4.7f, z), 0f, 90, $"02_StreetLight_z{z:0}");
        // 03 · closed shopfronts: corrugated shutters on south facades + alley gate
        Place(props, "SM_Shutter_Corrugated", new Vector3(-8.05f, 0f, 10f), 90, "03_Shutter_Shopfront_A");
        Place(props, "SM_Shutter_Corrugated", new Vector3(-8.05f, 0f, 26f), 90, "03_Shutter_Shopfront_B");
        Place(props, "SM_Shutter_Corrugated", new Vector3(8.03f, 0f, 33f), -90, "14_AlleyGate_WELDED — paint is fresh");
        // 10 · utility poles ×2, north side
        foreach (float z in new[] { 12f, 36f })
            PlaceCenter(props, "SM_UtilityPole_01", new Vector2(4.8f, z), 0f, -90, $"10_UtilityPole_z{z:0}");
        // 11 · vending machine, north corner, facing the road
        PlaceCenter(props, "SM_VendingMachine_01", new Vector2(7.2f, 5f), 0.411f, -90, "11_VendingMachine");
        // 12 · storm drains ×3 in the south gutter, flush with road surface
        foreach (float z in new[] { 6f, 22f, 38f })
            PlaceCenter(props, "SM_StormDrain_Grate", new Vector2(-3.75f, z), -0.135f, 0, $"12_StormDrain_z{z:0}");
        // 13 · route / reroute signs ×2
        PlaceCenter(props, "SM_RouteSign_01", new Vector2(-5.5f, 3f), 0.411f, 180, "13_RouteSign_South");
        PlaceCenter(props, "SM_RouteSign_01", new Vector2(5.5f, 30f), 0.411f, 0, "13_RerouteSign_North");
        // 19 · barricades ×2 (one steering from the crosswalk, one at the alley)
        PlaceCenter(props, "SM_Barricade_Stand", new Vector2(-5.8f, 1.5f), 0.411f, 0, "19_Barricade_Spawn");
        PlaceCenter(props, "SM_Barricade_Stand", new Vector2(5.0f, 31.4f), 0.411f, -90, "19_Barricade_Alley");
        // bollards ×6 (crosswalk ends + kiosk flank)
        foreach (var (x, z) in new[] { (-4.6f, 0.7f), (-4.6f, 5.4f), (4.6f, 0.7f), (4.6f, 5.4f), (-4.6f, 27f), (-4.6f, 29f) })
            PlaceCenter(props, "SM_Bollard_01", new Vector2(x, z), 0.411f, 0);
        // 16 · transit archive kiosk, south sidewalk
        PlaceCenter(props, "SM_TransitKiosk_01", new Vector2(-6.8f, 28f), 0.411f, 90, "16_TransitArchiveKiosk");
        // 17 · trash bins ×2 (lid open)
        PlaceCenter(props, "SM_TrashBin_LidOpen", new Vector2(-7.4f, 18f), 0.411f, 30, "17_TrashBin_South");
        PlaceCenter(props, "SM_TrashBin_LidOpen", new Vector2(7.2f, 39f), 0.411f, -120, "17_TrashBin_North");

        // ── Markers (gameplay anchors, no meshes) ──
        Marker(markers, "01_PlayerSpawn", new Vector3(-6f, 0.411f, 0.5f));
        var spawnCol = Marker(markers, "SpawnCollider", new Vector3(-6f, 1.4f, 0f));
        AddBox(spawnCol, new Vector3(4f, 2.8f, 0.5f));
        var exitS = Marker(markers, "BlockExitCollider_South", new Vector3(-6f, 1.4f, 48f));
        AddBox(exitS, new Vector3(4f, 2.8f, 0.5f));
        var exitR = Marker(markers, "BlockExitCollider_Road", new Vector3(0f, 1.4f, 48f));
        AddBox(exitR, new Vector3(8f, 2.8f, 0.5f));
        Marker(markers, "06_WitnessA_DialogueNode", new Vector3(-6.4f, 0.411f, 14f));
        Marker(markers, "09_WitnessB_DialogueNode", new Vector3(6.3f, 0.411f, 24f));
        Marker(markers, "CameraRig_FollowTarget", new Vector3(-6f, 1.2f, 0.5f));

        // ── SIGHT STATE (blueprint rose entries — root stays disabled) ──
        // The stop manifests at 41 m (forced approach) on the player's side.
        PlaceCenter(sight, "SM_BusStopSign_01", new Vector2(-4.8f, 41.2f), 0.411f, 90, "07_BusStopSign_1974");
        PlaceCenter(sight, "SM_TimetableCase_01", new Vector2(-7.7f, 41.8f), 0.411f, 90, "07_Timetable_Route9");
        PlaceCenter(sight, "SM_Bench_Aged", new Vector2(-6.5f, 41.5f), 0.411f, -90, "08_Bench_Aged");
        Marker(sight, "04_VictimPosition_Seated", new Vector3(-6.5f, 0.85f, 41.2f));
        var anchor = Marker(sight, "05_WaiterAnchor_NEGATIVE_SILHOUETTE — DO NOT REMOVE", new Vector3(-6.2f, 0.411f, 41.5f));
        AddSphere(anchor, 3.0f, "AnomalyField_Inner_r3.0");
        AddSphere(anchor, 5.5f, "AnomalyField_Outer_r5.5");
        Marker(sight, "15_Sediment_NewspaperStrata_Oct1974", new Vector3(-7.8f, 0.411f, 42.6f));
        Marker(sight, "18_ShelterCanopyFootprint", new Vector3(-6.6f, 0.411f, 40.6f));
        Marker(sight, "TimeResidue_QueueMarks_Start", new Vector3(-5.6f, 0.411f, 39.5f));
        sight.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        int placed = root.GetComponentsInChildren<MeshRenderer>(true).Length;
        long tris = 0;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.LongLength / 3;

        var warn = Log.Count > 0 ? (" WARNINGS: " + string.Join(" | ", Log)) : "";
        return $"layout rebuilt: {placed} renderers, {tris} tris, sight-state disabled root ready.{warn}";
    }

    // ── helpers ──────────────────────────────────────────────────────────
    static Transform Group(Transform parent, string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    static GameObject Load(string mesh)
    {
        var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{kPrefabs}/{mesh}.prefab");
        if (p == null) Log.Add($"prefab missing: {mesh}");
        return p;
    }

    /// Place so the instance's world bounds MIN corner lands on targetMin.
    static GameObject Place(Transform parent, string mesh, Vector3 targetMin, float rotY, string name = null)
    {
        var prefab = Load(mesh);
        if (prefab == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, rotY, 0));
        var b = WorldBounds(go);
        go.transform.position += targetMin - b.min;
        if (name != null) go.name = name;
        return go;
    }

    /// Place by bounds CENTER on X/Z with the bounds base at baseY.
    static GameObject PlaceCenter(Transform parent, string mesh, Vector2 centerXZ, float baseY, float rotY, string name = null)
    {
        var prefab = Load(mesh);
        if (prefab == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, rotY, 0));
        var b = WorldBounds(go);
        go.transform.position += new Vector3(centerXZ.x - b.center.x, baseY - b.min.y, centerXZ.y - b.center.z);
        if (name != null) go.name = name;
        return go;
    }

    /// Shift on X so the bounds' max (or min) face lands exactly on x.
    static void AlignX(GameObject go, float x, bool alignMax)
    {
        if (go == null) return;
        var b = WorldBounds(go);
        float delta = x - (alignMax ? b.max.x : b.min.x);
        go.transform.position += new Vector3(delta, 0f, 0f);
    }

    static Bounds WorldBounds(GameObject go)
    {
        var rends = go.GetComponentsInChildren<MeshRenderer>(true);
        if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = rends[0].bounds;
        foreach (var r in rends.Skip(1)) b.Encapsulate(r.bounds);
        return b;
    }

    static GameObject Marker(Transform parent, string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        return go;
    }

    static void AddBox(GameObject go, Vector3 size)
    {
        var c = go.AddComponent<BoxCollider>();
        c.size = size;
        c.isTrigger = true;
    }

    static void AddSphere(GameObject go, float radius, string childName)
    {
        var child = new GameObject(childName);
        child.transform.SetParent(go.transform, false);
        var c = child.AddComponent<SphereCollider>();
        c.radius = radius;
        c.isTrigger = true;
    }
}
