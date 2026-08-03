// UN-HUMANITY — proximity horror. Carrying Sight toward the stop ramps
// the wrongness: chroma split widens, grain thickens, the vignette closes,
// fog presses in. Outside the dread band it's just Sight; at the field's
// edge it is almost unbearable. Play-mode only (edits a runtime profile
// clone, never the asset).

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ProximityDread : MonoBehaviour
{
    public SightState sightState;
    public Volume sightVolume;
    public Transform player;
    public Vector3 anchor = new Vector3(0f, 0.4f, 46.4f);
    public float dreadOuter = 24f;   // ramp starts
    public float dreadInner = 3f;    // full horror at the field's edge

    ChromaticAberration chroma;
    FilmGrain grain;
    Vignette vignette;
    bool bound;
    bool stung;
    float stingCooldownUntil;

    void Bind()
    {
        if (bound || sightVolume == null) return;
        var p = sightVolume.profile;   // runtime instance clone
        p.TryGet(out chroma);
        p.TryGet(out grain);
        p.TryGet(out vignette);
        bound = true;
    }

    void Update()
    {
        if (!Application.isPlaying || sightState == null || player == null) return;
        Bind();
        if (chroma == null) return;

        float d = Vector3.Distance(player.position, anchor);
        float t = Mathf.Clamp01(1f - (d - dreadInner) / Mathf.Max(0.01f, dreadOuter - dreadInner));
        float s = sightState.blend * t;   // only matters while Seeing

        chroma.intensity.value = Mathf.Lerp(0.35f, 1.0f, s);
        if (grain != null) grain.intensity.value = Mathf.Lerp(0.25f, 0.65f, s);
        if (vignette != null) vignette.intensity.value = Mathf.Lerp(0.38f, 0.56f, s);

        // one sting as the dread peaks — then silence until you back off
        if (s > 0.7f && !stung && Time.time > stingCooldownUntil)
        {
            SfxBoss.Play("dread_sting");
            stung = true;
            stingCooldownUntil = Time.time + 18f;
        }
        if (s < 0.45f) stung = false;
        RenderSettings.fogDensity = Mathf.Lerp(
            Mathf.Lerp(sightState.normalcyFogDensity, sightState.sightFogDensity, sightState.blend),
            0.045f, s);
    }
}
