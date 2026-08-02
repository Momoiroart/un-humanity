// Octopath-class diorama rig: perspective + narrow FOV, shallow fixed
// pitch, fixed yaw — the camera translates with the player and never
// rotates. Follow lag per the blueprint: loose in Normalcy, tightening
// under Sight ("corridor framing tightens toward the anchor").

using UnityEngine;

[ExecuteAlways]
public class CameraRigFollow : MonoBehaviour
{
    public Transform target;
    public SightState sightState;

    [Header("Diorama framing (blueprint: ORTHO-BIAS · TILT 22 · FOV 26)")]
    public float pitch = 22f;
    public float distance = 25f;
    public float lookHeight = 1.0f;   // aim at sprite mid-height
    [Range(0f, 1f)] public float lateralFollow = 0.6f; // partial X follow keeps the street framed

    [Header("Follow smoothing (s) — Normalcy loose, Sight tight")]
    public float normalcySmooth = 0.45f;
    public float sightSmooth = 0.15f;

    Vector3 vel;

    void LateUpdate()
    {
        if (target == null) return;
        float blend = sightState != null ? sightState.blend : 0f;

        var aim = new Vector3(target.position.x * lateralFollow, lookHeight, target.position.z);
        var back = Quaternion.Euler(pitch, 0f, 0f) * Vector3.back * distance;
        var desired = aim + back;

        if (Application.isPlaying)
        {
            float smooth = Mathf.Lerp(normalcySmooth, sightSmooth, blend);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, smooth);
        }
        else
        {
            transform.position = desired;
        }
        transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
