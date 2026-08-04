// UN-HUMANITY — an authored camera pose for a prologue location. When a
// scene contains one of these, PrologueFlow parks the rig camera on this
// object's transform (position + rotation) and disables the street
// follow-rig — a fixed, framed shot (interiors, cinematic beats) instead of
// a diorama that buries itself in a five-metre room. Scenes WITHOUT one keep
// the follow rig (the open street/yard, where following reads fine).
//
// Optional gentle lateral pan: lateralFollow > 0 lets the shot drift with
// the player's X, clamped, so a long corridor can breathe without cutting.

using UnityEngine;

public class SceneCam : MonoBehaviour
{
    public float fov = 42f;
    [Range(0f, 1f)] public float lateralFollow = 0f;  // 0 = locked off
    public float lateralClamp = 2.5f;                  // max metres of drift
}
