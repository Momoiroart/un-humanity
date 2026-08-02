// UN-HUMANITY — palette-locked horror flicker. In Normalcy the lamp burns
// steady sodium-warm; as Sight blends in, it drifts to the cold wine tint
// and starts an irregular Perlin flicker with occasional hard blackout
// blinks. Rose is NOT used here — the accent belongs to the anomaly alone.

using UnityEngine;

[ExecuteAlways]
public class LightFlicker : MonoBehaviour
{
    public Light target;

    [Header("Normalcy — steady")]
    public Color normalColor = new Color(1f, 0.80f, 0.58f);
    public float normalIntensity = 9f;

    [Header("Sight — flickering")]
    public Color sightColor = new Color(0.60f, 0.39f, 0.47f);   // pale wine
    public float sightIntensity = 7f;
    public float flickerSpeed = 16f;
    [Range(0f, 1f)] public float flickerDepth = 0.6f;
    [Range(0f, 0.5f)] public float blackoutBelow = 0.12f;       // noise under this = hard off

    [Range(0f, 1f)] public float blend;                          // driven by SightState

    float seed;

    void OnEnable() => seed = transform.position.x * 3.7f + transform.position.z * 1.3f;

    void Update()
    {
        if (target == null) return;
        float t = Time.realtimeSinceStartup;
        float noise = Mathf.PerlinNoise(t * flickerSpeed, seed);
        float flickered = noise < blackoutBelow
            ? 0f
            : sightIntensity * Mathf.Lerp(1f - flickerDepth, 1f, noise);

        target.color = Color.Lerp(normalColor, sightColor, blend);
        target.intensity = Mathf.Lerp(normalIntensity, flickered, blend);
    }
}
