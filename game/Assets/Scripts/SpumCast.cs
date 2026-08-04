// UN-HUMANITY — the bridge from a SPUM-generated unit to an HD-2D cast member
// in our scenes. SPUM saves each character the owner builds as a prefab in
// Resources/SPUM/SPUM_Units/<code>; this loads one, sizes it to our sprite
// metric, primes its animator, and billboards it toward the diorama camera.
//
// Owner workflow (see PROGRESS.md): generate each role in SPUM_Scene, save it,
// note the unit code, then map role→code in SpumRoster below. The prologue
// layouts and PrologueFlow reference the ROLE, never the raw code, so a
// re-generated unit is a one-line swap here.

using UnityEngine;

public static class SpumCast
{
    // role → saved SPUM unit code. Fill these in after the owner's generation
    // session (empty = fall back to the placeholder quad billboard).
    public static class Roster
    {
        public const string Protagonist = "SPUM_20260804184019057";   // "The Stranger"
        public const string Friend      = "";   // "The Contact" (green-hoodie Painter)
        public const string Witness     = "";   // civilian set
        public const string Victim      = "";   // "Mira Bennett"
        public const string Waiter      = "";   // the bus-stop anomaly
    }

    // our sprite metric: 54 px character at 32 px/m ≈ 1.69 m tall (matches the
    // quad billboards). SPUM units author at their own scale, so normalise.
    const float kTargetHeight = 1.69f;

    /// Instantiate a SPUM unit as a billboarded cast member. Returns the root,
    /// or null if the code is empty / the unit isn't in Resources (caller then
    /// falls back to the placeholder quad).
    public static GameObject Place(Transform parent, string unitCode, Vector3 localPos, string name = null)
    {
        if (string.IsNullOrEmpty(unitCode)) return null;
        // SPUM saves units to Assets/SPUM/Resources/Units/<code>.prefab (v1.8.8),
        // so Resources.Load resolves them under "Units/".
        var prefab = Resources.Load<GameObject>($"Units/{unitCode}");
        if (prefab == null) { Debug.LogWarning($"[SpumCast] unit '{unitCode}' not in SPUM/Resources/Units"); return null; }

        var go = Object.Instantiate(prefab, parent);
        go.name = name ?? unitCode;
        go.transform.localPosition = localPos;
        NormaliseHeight(go);

        // the baked sprite hierarchy shows immediately; the animator is primed
        // at runtime (SpumIdle.Start), so scene-build time stays side-effect-free
        if (Application.isPlaying) InitAndIdle(go);
        else if (go.GetComponent<SpumIdle>() == null) go.AddComponent<SpumIdle>();

        if (go.GetComponent<SpumBillboard>() == null) go.AddComponent<SpumBillboard>();
        if (go.GetComponent<SpumMoodTint>() == null) go.AddComponent<SpumMoodTint>();
        return go;
    }

    /// Prime the animator override controller and start a looping idle.
    public static void InitAndIdle(GameObject unit)
    {
        var spum = unit != null ? unit.GetComponent<SPUM_Prefabs>() : null;
        if (spum == null) return;
        if (!spum.allListsHaveItemsExist()) spum.PopulateAnimationLists();
        spum.OverrideControllerInit();
        if (spum.IDLE_List != null && spum.IDLE_List.Count > 0)
            spum.PlayAnimation(PlayerState.IDLE, 0);
    }

    static void NormaliseHeight(GameObject go)
    {
        if (!TryMeasure(go, out var b)) return;
        float h = b.size.y;
        if (h > 0.01f) go.transform.localScale *= (kTargetHeight / h);
        // re-plant: put the lowest drawn pixel on the parent's foot plane, so
        // the cast stands ON the floor (scaling is about the SPUM pivot, not
        // the feet, so it drifts otherwise). Yaw-only billboard keeps it there.
        if (TryMeasure(go, out b))
        {
            float footY = go.transform.parent != null ? go.transform.parent.position.y : 0f;
            go.transform.position += Vector3.up * (footY - b.min.y);
        }
    }

    // world AABB of only renderers that actually draw (skip disabled / inactive /
    // null-sprite / zero-size, which sit at the origin and pollute the bounds)
    static bool TryMeasure(GameObject go, out Bounds b)
    {
        b = new Bounds(); bool any = false;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (r is SpriteRenderer sr && sr.sprite == null) continue;
            if (r.bounds.size == Vector3.zero) continue;
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        return any;
    }
}
