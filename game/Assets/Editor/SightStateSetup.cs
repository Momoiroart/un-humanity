// UN-HUMANITY — builds the Normalcy/Sight post-processing pair, the street
// point lights, and the state controller. One-shot, re-runnable.
//   unity command eval "return SightStateSetup.Build();"
//
// Guide011: post table (bloom .9/.8/.7, ACES, vignette .25, grain thin .15,
// exposure +.2 / sat -10 / contrast +10) and "real lights at the
// streetlights, vending machine, and kiosk; two survive into Sight".
// Blueprint: Sight adds chroma split, palette drain; ambient drains to wine.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SightStateSetup
{
    const string kScene = "Assets/Scenes/SC_02_StreetBlock.unity";

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        var notes = new List<string>();

        var vpNormalcy = MakeProfile("Assets/Settings/VP_Normalcy.asset", sight: false);
        var vpSight    = MakeProfile("Assets/Settings/VP_Sight.asset",    sight: true);
        notes.Add("profiles authored");

        foreach (var old in scene.GetRootGameObjects()
                 .Where(g => g.name == "PP_Normalcy" || g.name == "PP_Sight" || g.name == "StateController"))
            Object.DestroyImmediate(old);

        var volN = new GameObject("PP_Normalcy").AddComponent<Volume>();
        volN.isGlobal = true; volN.priority = 0; volN.profile = vpNormalcy; volN.weight = 1f;
        var volS = new GameObject("PP_Sight").AddComponent<Volume>();
        volS.isGlobal = true; volS.priority = 1; volS.profile = vpSight; volS.weight = 0f;

        // ── real lights at the lit props (Forward+ affords them) ──
        var layout = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "LAYOUT_StreetBlock");
        if (layout == null) return "FAILED: LAYOUT_StreetBlock not found — run StreetBlockLayout.Build first";

        var survive = new List<Light>();
        var die = new List<Light>();
        // materialize the prop roots FIRST — ChildLight destroys prior-run
        // lamp children, which would invalidate a live child iteration
        var propRoots = layout.GetComponentsInChildren<Transform>(true)
            .Where(t => t != null && (t.name.StartsWith("02_StreetLight")
                     || t.name == "11_VendingMachine"
                     || t.name == "16_TransitArchiveKiosk"))
            .ToList();
        foreach (var t in propRoots)
        {
            if (t.name.StartsWith("02_StreetLight"))
            {
                // lamp head: top of the pole, arm reaching over the road (+X)
                var head = ChildLight(t, "Lamp", t.position + new Vector3(2.3f, 6.3f, 0f),
                    new Color(1f, 0.80f, 0.58f), intensity: 3.2f, range: 14f);
                // z8 and z40 survive into Sight; z24 dies (mid-block goes dark)
                if (t.name.EndsWith("z24")) die.Add(head); else survive.Add(head);
            }
            else if (t.name == "11_VendingMachine")
                die.Add(ChildLight(t, "Glow", t.position + new Vector3(-0.7f, 1.2f, 0f),
                    new Color(0.93f, 0.55f, 0.51f), intensity: 1.6f, range: 4f));
            else if (t.name == "16_TransitArchiveKiosk")
                die.Add(ChildLight(t, "Glow", t.position + new Vector3(0.7f, 1.6f, 0f),
                    new Color(0.98f, 0.96f, 0.96f), intensity: 1.4f, range: 5f));
        }
        notes.Add($"lights: {survive.Count} survive, {die.Count} die under Sight");

        // dusk sky — the procedural blue skybox breaks the evening grade
        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.066f, 0.10f); // ink dusk
        }

        var sightRoot = scene.GetRootGameObjects()
            .FirstOrDefault(g => g.name.StartsWith("SIGHT_STATE"));
        // SIGHT_STATE was authored under the layout root:
        if (sightRoot == null && layout != null)
            sightRoot = layout.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name.StartsWith("SIGHT_STATE"))?.gameObject;

        var ctrl = new GameObject("StateController").AddComponent<SightState>();
        ctrl.normalcyVolume = volN;
        ctrl.sightVolume = volS;
        ctrl.sightRoot = sightRoot;
        ctrl.lightsThatSurvive = survive.ToArray();
        ctrl.lightsThatDie = die.ToArray();
        ctrl.blend = 0f;
        ctrl.Apply();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "state shift built: " + string.Join("; ", notes) +
               (sightRoot == null ? " WARNING: SIGHT_STATE root not found" : " (sight root wired)");
    }

    static Light ChildLight(Transform parent, string name, Vector3 worldPos, Color c, float intensity, float range)
    {
        var existing = parent.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = worldPos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None; // gray-box; cookies + soft shadows later
        return l;
    }

    static VolumeProfile MakeProfile(string path, bool sight)
    {
        var old = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (old != null) AssetDatabase.DeleteAsset(path);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);

        T Add<T>() where T : VolumeComponent
        {
            var c = profile.Add<T>(true);
            AssetDatabase.AddObjectToAsset(c, profile);
            return c;
        }

        var tone = Add<Tonemapping>();
        tone.mode.Override(TonemappingMode.ACES);

        var bloom = Add<Bloom>();
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(sight ? 1.0f : 0.8f);
        bloom.scatter.Override(0.7f);

        var vig = Add<Vignette>();
        vig.intensity.Override(sight ? 0.38f : 0.25f);
        vig.smoothness.Override(0.5f);

        var grain = Add<FilmGrain>();
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(sight ? 0.25f : 0.15f);

        var ca = Add<ColorAdjustments>();
        if (sight)
        {
            // palette drain: two hues survive — ink and rose
            ca.postExposure.Override(0.05f);
            ca.saturation.Override(-55f);
            ca.contrast.Override(22f);
            ca.colorFilter.Override(new Color(0.92f, 0.62f, 0.74f)); // wine/rose cast
        }
        else
        {
            ca.postExposure.Override(0.2f);
            ca.saturation.Override(-10f);
            ca.contrast.Override(10f);
        }

        if (sight)
        {
            var chroma = Add<ChromaticAberration>();
            chroma.intensity.Override(0.35f); // the sheet's "chroma split"
        }

        // Tilt-shift bokeh — the Octopath diorama's other half (Guide011:
        // 60-90 mm, f/2.8-4, focus on the play plane). Milder in Normalcy,
        // tighter under Sight as the framing closes in.
        var dof = Add<DepthOfField>();
        dof.mode.Override(DepthOfFieldMode.Bokeh);
        dof.focusDistance.Override(sight ? 23f : 25f);
        dof.focalLength.Override(sight ? 85f : 70f);
        dof.aperture.Override(sight ? 2.9f : 3.4f);

        AssetDatabase.SaveAssets();
        return profile;
    }
}
