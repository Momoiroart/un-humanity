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

    [Header("Normalcy grade — a bright, ordinary morning")]
    public Color normalcyAmbient = new Color(0.60f, 0.53f, 0.57f);
    public Color normalcyFog = new Color(0.80f, 0.71f, 0.75f);
    public float normalcyFogDensity = 0.0035f;   // see far down the road

    [Header("Sight grade — the morning is a lie; it is always the wait")]
    public Color sightAmbient = new Color(0.055f, 0.016f, 0.035f);
    public Color sightFog = new Color(0.045f, 0.012f, 0.030f);
    public float sightFogDensity = 0.030f;

    [Header("Key light + sky (wired by SightStateSetup)")]
    public Light keyLight;
    public Camera worldCamera;
    public Color normalcyKeyColor = new Color(1f, 0.93f, 0.82f);
    public float normalcyKeyIntensity = 1.7f;
    public Color sightKeyColor = new Color(0.42f, 0.26f, 0.34f);
    public float sightKeyIntensity = 0.10f;      // the sun goes out
    public Color normalcySky = new Color(0.88f, 0.79f, 0.82f);   // pale morning, in-palette
    public Color sightSky = new Color(0.055f, 0.025f, 0.045f);

    [Range(0f, 1f)] public float blend;

    [Header("Sight cost — it drains while held (UH-001 shift trigger note)")]
    public float meterMax = 100f;
    public float drainPerSecond = 14f;
    public float regenPerSecond = 22f;
    public float lockoutUntil = 25f;    // once emptied, recover to this before re-use
    public float blendSpeed = 3f;
    public float Meter { get; private set; } = 100f;
    public bool LockedOut { get; private set; }

    /// Per-frame drive from the player: hold intent + dt. Handles the
    /// drain, the empty-lockout, and the blend easing, then applies.
    public void Tick(bool wantSight, float dt)
    {
        if (LockedOut && Meter >= lockoutUntil) LockedOut = false;
        bool effective = wantSight && !LockedOut && Meter > 0f;

        if (effective)
        {
            Meter = Mathf.Max(0f, Meter - drainPerSecond * dt);
            if (Meter <= 0f) { LockedOut = true; effective = false; }
        }
        else
        {
            Meter = Mathf.Min(meterMax, Meter + regenPerSecond * dt);
        }

        float target = effective ? 1f : 0f;
        float b = Mathf.MoveTowards(blend, target, blendSpeed * dt);
        if (!Mathf.Approximately(b, blend)) SetSight(b);
    }

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
        if (keyLight != null)
        {
            keyLight.intensity = Mathf.Lerp(normalcyKeyIntensity, sightKeyIntensity, blend);
            keyLight.color = Color.Lerp(normalcyKeyColor, sightKeyColor, blend);
        }
        if (worldCamera != null)
            worldCamera.backgroundColor = Color.Lerp(normalcySky, sightSky, blend);

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
