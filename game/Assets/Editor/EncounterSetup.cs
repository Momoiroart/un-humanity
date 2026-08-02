// UN-HUMANITY — wires THE QUEUE into the world: the anomaly-field trigger
// at the Waiter anchor, the combat camera focus, and Sight-hold during the
// encounter. Re-runnable.
//   unity command eval "return EncounterSetup.Build();"

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EncounterSetup
{
    const string kScene = "Assets/Scenes/SC_02_StreetBlock.unity";

    public static string Build()
    {
        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects()
                 .Where(g => g.name == "QueueTrigger" || g.name == "CombatCamFocus"))
            Object.DestroyImmediate(g);

        var focus = new GameObject("CombatCamFocus");
        focus.transform.position = new Vector3(-4.2f, 1.0f, 39.6f); // frames player + the stop

        var trigGo = new GameObject("QueueTrigger");
        var trig = trigGo.AddComponent<QueueTrigger>();
        trig.caseController = Object.FindFirstObjectByType<CaseController>();
        trig.combatUI = Object.FindFirstObjectByType<QueueCombatUI>();
        trig.sightState = Object.FindFirstObjectByType<SightState>();
        var player = GameObject.Find("Player");
        trig.player = player != null ? player.transform : null;
        trig.anchorPosition = new Vector3(-6.2f, 0.4f, 41.5f);
        trig.engageRadius = 5.5f;

        if (trig.combatUI != null) trig.combatUI.sightState = trig.sightState;
        var rig = Object.FindFirstObjectByType<CameraRigFollow>();
        if (rig != null) rig.combatFocus = focus.transform;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        bool ok = trig.caseController != null && trig.combatUI != null
               && trig.sightState != null && trig.player != null && rig != null;
        return "queue trigger wired at the anomaly field (r=5.5, Sight-gated)"
             + (ok ? "" : " WARNING: missing refs");
    }
}
