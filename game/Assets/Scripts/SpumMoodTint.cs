// UN-HUMANITY — couples an unlit SPUM cast unit to the scene mood. SPUM units
// draw on the UNLIT Sprites/Default shader, so they never sample
// RenderSettings.ambientLight and stay full-bright while every URP/Lit surface
// (and the quad NPCs) darkens with the room — the "pasted-on" look, worst in
// the drained crack and the dusk handoff. This tints each SpriteRenderer from a
// CACHED base colour by the scene ambient, so the cast drains with the world
// but never crushes to pure black. Play-mode only (no ExecuteAlways) — edit-time
// keeps the pristine baked sprites. SpumCast attaches it.

using System.Collections.Generic;
using UnityEngine;

public class SpumMoodTint : MonoBehaviour
{
    [Range(0f, 1f)] public float strength = 0.85f;   // how hard ambient pulls the tint
    [Range(0f, 1f)] public float floorLevel = 0.22f; // never darker than this (stay legible)
    public float refAmbient = 0.6f;                  // ambient at which the cast reads full

    readonly List<SpriteRenderer> rends = new();
    readonly List<Color> baseColors = new();

    void Awake()
    {
        foreach (var r in GetComponentsInChildren<SpriteRenderer>(true))
        {
            rends.Add(r);
            baseColors.Add(r.color);   // snapshot the pristine prefab colour once
        }
    }

    void LateUpdate()
    {
        if (rends.Count == 0) return;
        var a = RenderSettings.ambientLight;
        float tr = Map(a.r), tg = Map(a.g), tb = Map(a.b);
        for (int i = 0; i < rends.Count; i++)
        {
            var r = rends[i];
            if (r == null) continue;
            var b = baseColors[i];
            r.color = new Color(b.r * tr, b.g * tg, b.b * tb, b.a);   // alpha untouched (alpha-clip)
        }
    }

    float Map(float chan)
        => Mathf.Max(Mathf.Lerp(1f, Mathf.Clamp01(chan / refAmbient), strength), floorLevel);
}
