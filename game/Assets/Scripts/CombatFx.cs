// UN-HUMANITY — combat effects. One billboard quad at the stop plays a
// 4-frame strip per event: your actions visibly DO something, its cheats
// visibly land. Fire-and-forget; missing sheets fail silent.

using UnityEngine;

public class CombatFx : MonoBehaviour
{
    [System.Serializable]
    public class Fx
    {
        public string name;
        public Material mat;   // unlit transparent, 4-frame strip, tiling x = 0.25
    }

    public static CombatFx I { get; private set; }

    public Fx[] sheets;
    public MeshRenderer quad;
    public float frameTime = 0.09f;

    Material live;   // runtime instance — never mutate the shared asset
    float t = -1f;

    void Awake() { I = this; if (quad != null) quad.enabled = false; }
    void OnEnable() { I = this; }

    public static void Play(string fxName)
    {
        if (I == null || I.quad == null || !Application.isPlaying) return;
        foreach (var s in I.sheets)
        {
            if (s == null || s.mat == null || s.name != fxName) continue;
            I.live = new Material(s.mat);
            I.quad.material = I.live;
            I.quad.enabled = true;
            I.t = 0f;
            return;
        }
    }

    void Update()
    {
        if (t < 0f || live == null) return;
        t += Time.deltaTime;
        int frame = (int)(t / Mathf.Max(0.01f, frameTime));
        if (frame > 3)
        {
            quad.enabled = false;
            t = -1f;
            return;
        }
        live.mainTextureOffset = new Vector2(frame * 0.25f, 0f);
    }
}
