using UnityEngine;

// An EDITOR preview camera dropped at a scene's authored SceneCam pose so the
// intended HD-2D framing is visible in the Editor / Game view without entering
// play mode. At runtime it disables its own Camera + AudioListener so the
// persistent prologue rig stays the only renderer (no double-camera conflict).
// The prologue scenes ship without a Camera on purpose — this is a convenience
// so the owner can see what each shot actually looks like.
[RequireComponent(typeof(Camera))]
public class PreviewCam : MonoBehaviour
{
    void Awake()
    {
        if (!Application.isPlaying) return;
        // Only step aside when the persistent prologue rig is actually present
        // (the real multi-scene flow). If this scene is played standalone, STAY
        // enabled so there's still a camera (otherwise Unity warns "No cameras
        // rendering" and the screen is black).
        bool rigPresent = FindObjectOfType<PrologueFlow>() != null;
        if (!rigPresent) return;
        var cam = GetComponent<Camera>(); if (cam != null) cam.enabled = false;
        var al = GetComponent<AudioListener>(); if (al != null) al.enabled = false;
        var vol = GetComponent<UnityEngine.Rendering.Volume>(); if (vol != null) vol.enabled = false;
    }
}
