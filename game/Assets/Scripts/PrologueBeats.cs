// UN-HUMANITY — a per-location marker. Each prologue scene carries one of
// these so the persistent PrologueFlow knows which beat-script to run when
// that scene becomes active, and which scene follows. Beat CONTENT lives in
// PrologueFlow (centralized), keyed by sceneId — the scene just declares
// its identity and its successor.

using UnityEngine;

public class PrologueBeats : MonoBehaviour
{
    public string sceneId;      // "bedroom" | "walk" | "school"
    public string nextScene;    // asset path of the scene to load after
}
