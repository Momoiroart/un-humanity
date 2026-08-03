// UN-HUMANITY — per-scene mood. The mood law writes GLOBAL RenderSettings,
// which is per-scene; this component stamps a location's ambient/fog when
// its scene becomes the active one, so a warm bedroom and a bright street
// keep their own looks without a shared SightState. Re-applies on enable
// so an additively-loaded location re-asserts its mood after a transition.

using UnityEngine;

[ExecuteAlways]
public class RoomMood : MonoBehaviour
{
    public Color ambient = new Color(0.42f, 0.36f, 0.34f);
    public Color fog = new Color(0.30f, 0.26f, 0.28f);
    public float fogDensity = 0.006f;
    public bool fogEnabled = true;
    public Color cameraBackground = new Color(0.14f, 0.12f, 0.14f);

    public void Apply()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambient;
        RenderSettings.fog = fogEnabled;
        RenderSettings.fogColor = fog;
        RenderSettings.fogDensity = fogDensity;
        var cam = Camera.main;
        if (cam != null && cam.clearFlags == CameraClearFlags.SolidColor)
            cam.backgroundColor = cameraBackground;
    }

    void OnEnable() => Apply();
    void Start() => Apply();
}
