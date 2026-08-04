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
        public const string Protagonist = "";   // "The Stranger"
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

        var spum = go.GetComponent<SPUM_Prefabs>();
        if (spum != null)
        {
            if (!spum.allListsHaveItemsExist()) spum.PopulateAnimationLists();
            spum.OverrideControllerInit();
        }
        if (go.GetComponent<SpumBillboard>() == null) go.AddComponent<SpumBillboard>();
        return go;
    }

    /// Play a looping idle at runtime (call from Start/OnEnable in play mode).
    public static void Idle(GameObject unit)
    {
        var spum = unit != null ? unit.GetComponent<SPUM_Prefabs>() : null;
        if (spum != null && spum.IDLE_List != null && spum.IDLE_List.Count > 0)
            spum.PlayAnimation(PlayerState.IDLE, 0);
    }

    static void NormaliseHeight(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        float h = b.size.y;
        if (h > 0.01f)
        {
            float k = kTargetHeight / h;
            go.transform.localScale *= k;
        }
    }
}
