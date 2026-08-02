// UN-HUMANITY — background pedestrians on the far, unreachable strip.
// They walk the morning street beyond the boundary wall — proof the city
// is alive past where you can go. Under Sight they are GONE: the far
// street empties, and only the stop remains.

using UnityEngine;

public class BackgroundWalker : MonoBehaviour
{
    public float zMin = 52f;
    public float zMax = 86f;
    public float speed = 1.1f;
    public int direction = 1;
    public Transform spriteQuad;   // X-flips with direction
    public Renderer[] renderers;

    SightState sight;

    void Start()
    {
        sight = FindFirstObjectByType<SightState>();
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        // the far street only exists to plain eyes
        bool visible = sight == null || sight.blend < 0.5f;
        foreach (var r in renderers)
            if (r != null && r.enabled != visible) r.enabled = visible;
        if (!visible) return;

        var p = transform.position;
        p.z += direction * speed * Time.deltaTime;
        if (p.z > zMax) { p.z = zMax; direction = -1; }
        if (p.z < zMin) { p.z = zMin; direction = 1; }
        transform.position = p;

        if (spriteQuad != null)
        {
            var s = spriteQuad.localScale;
            s.x = Mathf.Abs(s.x); // facing along Z reads fine either way at distance
            spriteQuad.localScale = s;
        }
    }
}
