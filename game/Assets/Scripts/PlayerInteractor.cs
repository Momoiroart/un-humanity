// UN-HUMANITY — proximity interaction. F / gamepad-south examines the
// nearest clue node that currently responds (Sight reveals the rest).

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    public CaseController caseController;

    public ClueNode Current { get; private set; }

    void Update()
    {
        Current = null;
        if (QueueCombatUI.CombatActive) return;   // no examining mid-QUEUE
        if (caseController == null) return;

        // an open record captures F: close it, don't re-collect
        if (caseController.reading != null && caseController.reading.IsOpen)
        {
            var kbd = Keyboard.current;
            var gp = Gamepad.current;
            if ((kbd != null && (kbd.fKey.wasPressedThisFrame || kbd.escapeKey.wasPressedThisFrame))
                || (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.buttonEast.wasPressedThisFrame)))
                caseController.reading.Close();
            return;
        }

        float best = float.MaxValue;
        bool sight = caseController.SightActive;
        foreach (var node in ClueNode.All)
        {
            if (node == null || !node.isActiveAndEnabled) continue;
            if (!sight && !node.respondsInNormalcy) continue;   // scenery until Sight
            float d = Vector3.Distance(transform.position, node.transform.position);
            if (d <= node.radius && d < best) { best = d; Current = node; }
        }

        if (Current == null) return;
        var kb = Keyboard.current;
        var pad = Gamepad.current;
        bool pressed = (kb != null && kb.fKey.wasPressedThisFrame)
                    || (pad != null && pad.buttonSouth.wasPressedThisFrame);
        if (pressed) caseController.TryCollect(Current);
    }
}
