// UN-HUMANITY — SUPERSEDED. The v1 single-scene prologue was replaced by
// the multi-scene PrologueFlow (Slice 0 v2, 2026-08-04). This stub survives
// only to keep `PrologueSequence.Active` — the gate other systems
// (PlayerController, QueueTrigger, PlayerInteractor, CaseLogUI) still read.
// In the exam scene (SC_02) nothing sets it, so it stays false and the
// gates are harmless no-ops. Do not add beat logic here — the prologue
// lives in PrologueFlow now.

using UnityEngine;

public class PrologueSequence : MonoBehaviour
{
    public static bool Active { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Active = false;
}
