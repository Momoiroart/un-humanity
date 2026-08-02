// HD-2D billboard: face the camera on Y ONLY — never pitch (Guide011).
// The quad keeps a small backward lean so the key light rakes it like the
// environment (blueprint: quad leans back 3-5°).

using UnityEngine;

[ExecuteAlways]
public class BillboardY : MonoBehaviour
{
    [Range(0f, 8f)] public float leanBack = 4f;

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var to = transform.position - cam.transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up)
                           * Quaternion.Euler(-leanBack, 0f, 0f);
    }
}
