// UN-HUMANITY — skyline cards read as morning haze (atmospheric
// perspective: farther = lighter) and fall to near-black under Sight.
// The silhouette texture is white; this tint owns the color entirely.

using UnityEngine;

[ExecuteAlways]
public class SkylineStateTint : MonoBehaviour
{
    public Color morning = new Color(0.68f, 0.60f, 0.65f);
    public Color sightDark = new Color(0.035f, 0.018f, 0.032f);

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    SightState sight;
    Renderer rend;
    MaterialPropertyBlock mpb;

    public void Apply()
    {
        if (rend == null) rend = GetComponent<Renderer>();
        if (rend == null) return;
        if (sight == null) sight = FindFirstObjectByType<SightState>();
        if (mpb == null) mpb = new MaterialPropertyBlock();
        float b = sight != null ? sight.blend : 0f;
        rend.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColor, Color.Lerp(morning, sightDark, b));
        rend.SetPropertyBlock(mpb);
    }

    void Update() => Apply();

    public static void ApplyAll()
    {
        foreach (var t in FindObjectsByType<SkylineStateTint>(FindObjectsSortMode.None))
            t.Apply();
    }
}
