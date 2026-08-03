// UN-HUMANITY — the sound board. One 2D one-shot channel + two beds:
// the morning street (fades out as Sight takes hold) and the Sight dread
// bed (fades in). Cues are wired by AudioSetup from Assets/Audio, named
// SFX_<cue>.wav. Missing cues fail silent — audio must never break play.

using UnityEngine;

public class SfxBoss : MonoBehaviour
{
    [System.Serializable]
    public class Cue
    {
        public string name;
        public AudioClip clip;
        public float volume = 0.7f;
    }

    public static SfxBoss I { get; private set; }

    public Cue[] cues;
    public AudioSource oneShot;      // 2D, no loop
    public AudioSource sightLoop;    // dread bed, loops, volume = blend
    public AudioSource morningLoop;  // street tone, loops, volume = 1-blend

    public float sightBedVolume = 0.55f;
    public float morningBedVolume = 0.30f;

    void Awake() => I = this;
    void OnEnable() => I = this;

    public static void Play(string cueName, float volumeScale = 1f)
    {
        if (I == null || I.oneShot == null || !Application.isPlaying) return;
        // several cues may share a name (sibling takes) — pick one at
        // random so repeated events never sound stamped
        Cue chosen = null;
        int seen = 0;
        foreach (var c in I.cues)
        {
            if (c == null || c.clip == null || c.name != cueName) continue;
            seen++;
            if (Random.Range(0, seen) == 0) chosen = c;   // reservoir pick
        }
        if (chosen != null) I.oneShot.PlayOneShot(chosen.clip, chosen.volume * volumeScale);
    }

    /// Crossfade the beds with the Sight blend. Called from SightState.
    public static void SetSightBlend(float blend)
    {
        if (I == null || !Application.isPlaying) return;
        if (I.sightLoop != null)
        {
            I.sightLoop.volume = blend * I.sightBedVolume;
            if (blend > 0.01f && !I.sightLoop.isPlaying && I.sightLoop.clip != null) I.sightLoop.Play();
            if (blend <= 0.01f && I.sightLoop.isPlaying) I.sightLoop.Pause();
        }
        if (I.morningLoop != null)
        {
            I.morningLoop.volume = (1f - blend) * I.morningBedVolume;
            if (blend < 0.99f && !I.morningLoop.isPlaying && I.morningLoop.clip != null) I.morningLoop.Play();
            if (blend >= 0.99f && I.morningLoop.isPlaying) I.morningLoop.Pause();
        }
    }
}
