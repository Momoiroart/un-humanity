// UN-HUMANITY — makes a SPUM unit read as an HD-2D billboard: the whole
// pixel character turns to face the diorama camera on the horizontal plane
// (yaw only, stays upright), the same trick BillboardY does for our quad
// sprites. Add this to an instantiated SPUM unit root (SpumCast does it).

using UnityEngine;

[ExecuteAlways]
public class SpumBillboard : MonoBehaviour
{
    Camera cam;

    void OnEnable() { cam = Camera.main; }

    void LateUpdate()
    {
        if (cam == null || !cam.isActiveAndEnabled) cam = Camera.main;
        if (cam == null) return;
        // point the unit's +Z away from the camera so its front face (drawn on
        // the XY plane) turns toward the lens; flatten to keep it standing up
        Vector3 away = transform.position - cam.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude > 1e-4f)
            transform.rotation = Quaternion.LookRotation(away, Vector3.up);
    }
}
