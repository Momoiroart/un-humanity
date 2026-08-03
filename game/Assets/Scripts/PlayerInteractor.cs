// UN-HUMANITY — proximity interaction. F / gamepad-south examines the
// nearest clue node that currently responds (Sight reveals the rest).

using UnityEngine;
using UnityEngine.InputSystem;
using UnHumanity.Case;

public class PlayerInteractor : MonoBehaviour
{
    public CaseController caseController;

    public ClueNode Current { get; private set; }

    public DialogueUI dialogue;   // wired by CaseSetup
    public StoryUI story;         // wired by CaseSetup — no examine over a card

    void Update()
    {
        Current = null;
        // combat, an open conversation, or a story card all own the frame
        if (QueueCombatUI.CombatActive) return;
        if (DialogueUI.DialogueActive) return;   // the box owns F/Esc while talking
        if (story != null && story.Current != StoryUI.Beat.Hidden) return;
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
        if (!pressed) return;

        // a witness talks the FIRST time (and only in Normalcy — under Sight
        // the seat is empty and the Sight reading is the whole content).
        // After that, F opens the record like any other clue.
        if (Current.talks && !Current.hasTalked && !sight && dialogue != null)
        {
            var convo = Current.clueId == ClueId.Witnesses && Current.name.Contains("WitnessB")
                ? WitnessDialogue.Commuter()
                : WitnessDialogue.OldMan();
            dialogue.Begin(convo, Current);
            return;
        }
        caseController.TryCollect(Current);
    }
}
