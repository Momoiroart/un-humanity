// UN-HUMANITY — Normalcy <-> Sight state controller (gray-box scaffold).
// Blueprint rev 0.3 state-shift matrix: ambient drains to wine, fog closes
// in, the Sight-only entities appear, and all but two street lights die.
//
// Authoring note: the slice scaffold toggles the SIGHT_STATE root's
// active flag. The shipping controller must move to spawn-on-activate per
// the sheet ("absent from the scene graph, not hidden") once the systems
// pass lands — players must not be able to collide with, photograph, or
// hear Sight entities in Normalcy.

using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class SightState : MonoBehaviour
{
    [Header("Wiring (set by SightStateSetup)")]
    public Volume normalcyVolume;
    public Volume sightVolume;
    public GameObject sightRoot;
    public Light[] lightsThatSurvive;
    public Light[] lightsThatDie;
    public LightFlicker[] flickers;

    [Header("Normalcy grade")]
    public Color normalcyAmbient = new Color(0.165f, 0.078f, 0.125f);
    public Color normalcyFog = new Color(0.090f, 0.051f, 0.078f);
    public float normalcyFogDensity = 0.012f;

    [Header("Sight grade — two hues survive: ink and rose")]
    public Color sightAmbient = new Color(0.110f, 0.031f, 0.071f);
    public Color sightFog = new Color(0.071f, 0.020f, 0.047f);
    public float sightFogDensity = 0.022f;

    [Range(0f, 1f)] public float blend;

    public void SetSight(float t)
    {
        blend = Mathf.Clamp01(t);
        Apply();
    }

    [ContextMenu("Toggle Sight")]
    public void ToggleSight() => SetSight(blend < 0.5f ? 1f : 0f);

    public void Apply()
    {
        if (normalcyVolume != null) normalcyVolume.weight = 1f - blend;
        if (sightVolume != null) sightVolume.weight = blend;

        RenderSettings.ambientLight = Color.Lerp(normalcyAmbient, sightAmbient, blend);
        RenderSettings.fogColor = Color.Lerp(normalcyFog, sightFog, blend);
        RenderSettings.fogDensity = Mathf.Lerp(normalcyFogDensity, sightFogDensity, blend);

        bool sight = blend > 0.5f;
        if (sightRoot != null && sightRoot.activeSelf != sight) sightRoot.SetActive(sight);
        if (lightsThatDie != null)
            foreach (var l in lightsThatDie)
                if (l != null) l.enabled = !sight;
        if (lightsThatSurvive != null)
            foreach (var l in lightsThatSurvive)
                if (l != null) l.enabled = true;
        if (flickers != null)
            foreach (var f in flickers)
                if (f != null) f.blend = blend;
    }
}
