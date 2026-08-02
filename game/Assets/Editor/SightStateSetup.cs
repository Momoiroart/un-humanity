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
        var flickers = new List<LightFlicker>();
        var warmSodium = new Color(1f, 0.80f, 0.58f);
        var paleWine = new Color(0.60f, 0.39f, 0.47f);
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
                // downward spot at the arm's end — a real sodium pool on the road
                var head = ChildLight(t, "Lamp", t.position + new Vector3(2.2f, 6.3f, 0f),
                    warmSodium, intensity: 60f, range: 13f);
                head.type = LightType.Spot;
                head.spotAngle = 52f;
                head.innerSpotAngle = 24f;
                head.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                // z8 and z40 survive into Sight — cold, flickering, blackout-prone;
                // z24 dies (mid-block goes fully dark)
                if (t.name.EndsWith("z24")) { head.enabled = false; die.Add(head); }
                else
                {
                    survive.Add(head);
                    var fl = head.gameObject.AddComponent<LightFlicker>();
                    fl.target = head;
                    fl.normalColor = warmSodium; fl.normalIntensity = 0f;   // morning: lamps are off
                    fl.sightColor = paleWine; fl.sightIntensity = 46f;
                    fl.flickerSpeed = 16f; fl.flickerDepth = 0.6f; fl.blackoutBelow = 0.12f;
                    flickers.Add(fl);
                }
            }
            else if (t.name == "11_VendingMachine")
                ChildLight(t, "Glow", t.position + new Vector3(-0.7f, 1.2f, 0f),
                    new Color(0.93f, 0.55f, 0.51f), intensity: 1.6f, range: 4f);
            else if (t.name == "16_TransitArchiveKiosk")
                ChildLight(t, "Glow", t.position + new Vector3(0.7f, 1.6f, 0f),
                    new Color(0.98f, 0.96f, 0.96f), intensity: 1.4f, range: 5f);
        }
        notes.Add($"lights: {survive.Count} survive (flickering), {die.Count} die under Sight");

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

        // The anomaly's glow — the ONLY true-rose light in the scene, at the
        // stop that shouldn't exist. Lives under the sight root, so in
        // Normalcy it is absent, not hidden. Slow wrong-pulse via flicker.
        if (sightRoot != null)
        {
            var oldGlow = sightRoot.transform.Find("AnomalyGlow");
            if (oldGlow != null) Object.DestroyImmediate(oldGlow.gameObject);
            var glowGo = new GameObject("AnomalyGlow");
            glowGo.transform.SetParent(sightRoot.transform, false);
            glowGo.transform.position = new Vector3(0f, 1.8f, 46.4f);   // mid-road, where the road was going
            var glow = glowGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color32(0xE4, 0x56, 0x8A, 0xFF);   // the anomaly's rose
            glow.intensity = 3.5f;
            glow.range = 9f;
            glow.shadows = LightShadows.None;
            var pulse = glowGo.AddComponent<LightFlicker>();
            pulse.target = glow;
            pulse.normalColor = glow.color; pulse.normalIntensity = 3.5f;
            pulse.sightColor = glow.color; pulse.sightIntensity = 4.2f;
            pulse.flickerSpeed = 1.6f;      // slow breathing, not electrical
            pulse.flickerDepth = 0.35f;
            pulse.blackoutBelow = 0f;
            flickers.Add(pulse);
            notes.Add("anomaly glow placed at the stop (rose, sight-only)");
        }

        var keyGo = GameObject.Find("Directional Key");
        if (keyGo != null)
        {
            var kl = keyGo.GetComponent<Light>();
            kl.intensity = 1.7f;
            kl.color = new Color(1f, 0.93f, 0.82f);
            keyGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);   // morning sun, higher
        }

        var ctrl = new GameObject("StateController").AddComponent<SightState>();
        ctrl.keyLight = keyGo != null ? keyGo.GetComponent<Light>() : null;
        ctrl.worldCamera = cam;
        ctrl.normalcyVolume = volN;
        ctrl.sightVolume = volS;
        ctrl.sightRoot = sightRoot;
        ctrl.lightsThatSurvive = survive.ToArray();
        ctrl.lightsThatDie = die.ToArray();
        ctrl.flickers = flickers.ToArray();
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
            ca.postExposure.Override(0.4f);
            ca.saturation.Override(-4f);
            ca.contrast.Override(8f);
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
