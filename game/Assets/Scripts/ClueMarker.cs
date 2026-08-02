// UN-HUMANITY — floating evidence marker. Fog diamond for nodes that
// respond in Normalcy (human testimony); rose for what only Sight reveals.
// Bobs gently, disappears once the clue is in the file.

using UnityEngine;

[ExecuteAlways]
public class ClueMarker : MonoBehaviour
{
    public ClueNode node;
    public CaseController controller;
    public Renderer quad;
    public Material fogMat;
    public Material roseMat;
    public float bobHeight = 0.08f;
    public float bobSpeed = 2.2f;

    Vector3 basePos;

    void OnEnable() => basePos = transform.localPosition;

    void Update()
    {
        if (node == null || controller == null || quad == null) return;

        bool sight = controller.SightActive;
        bool collected = controller.File != null && controller.File.Has(node.clueId);
        // markers exist only while Sight is held — the street never
        // advertises its evidence to plain eyes
        bool visible = !collected && sight;

        if (quad.enabled != visible) quad.enabled = visible;
        if (!visible) return;

        quad.sharedMaterial = sight ? roseMat : fogMat;
        float t = Time.realtimeSinceStartup;
        transform.localPosition = basePos + Vector3.up * (Mathf.Sin(t * bobSpeed) * bobHeight);

        // billboard toward the camera, rolled 45° into a diamond
        var cam = Camera.main;
        if (cam != null)
        {
            var to = transform.position - cam.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up)
                                   * Quaternion.Euler(0f, 0f, 45f);
        }
    }
}
