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
        focus.transform.position = new Vector3(0f, 1.0f, 43.2f); // frames player + the road-end stop

        var trigGo = new GameObject("QueueTrigger");
        var trig = trigGo.AddComponent<QueueTrigger>();
        trig.caseController = Object.FindFirstObjectByType<CaseController>();
        trig.combatUI = Object.FindFirstObjectByType<QueueCombatUI>();
        trig.sightState = Object.FindFirstObjectByType<SightState>();
        var player = GameObject.Find("Player");
        trig.player = player != null ? player.transform : null;
        // engage on the INNER field only (r=3.0); the outer 5.5-12 m band is
        // the dread zone — ProximityDread ramps the horror as you close in
        trig.anchorPosition = new Vector3(0f, 0.4f, 46.4f);
        trig.engageRadius = 3.0f;

        if (trig.combatUI != null) trig.combatUI.sightState = trig.sightState;
        var rig = Object.FindFirstObjectByType<CameraRigFollow>();
        if (rig != null) rig.combatFocus = focus.transform;

        // UI needs an EventSystem or no button in the game is clickable
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // proximity dread — the closer you carry Sight toward it, the worse
        var dread = Object.FindFirstObjectByType<ProximityDread>();
        if (dread == null)
        {
            var sc = Object.FindFirstObjectByType<SightState>();
            dread = sc.gameObject.AddComponent<ProximityDread>();
        }
        dread.sightState = trig.sightState;
        dread.anchor = trig.anchorPosition;
        dread.player = trig.player;
        dread.sightVolume = Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None)
            .FirstOrDefault(v => v.name == "PP_Sight");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        bool ok = trig.caseController != null && trig.combatUI != null
               && trig.sightState != null && trig.player != null && rig != null;
        return "queue trigger wired at the anomaly field (r=5.5, Sight-gated)"
             + (ok ? "" : " WARNING: missing refs");
    }
}
