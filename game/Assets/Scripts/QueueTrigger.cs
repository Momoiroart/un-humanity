// UN-HUMANITY — the anomaly field engages THE QUEUE. Cross into the
// Waiter's outer field (r=5.5) with Sight held and the encounter begins,
// carrying exactly as much knowledge as the case file holds. Investigate →
// classify → engage is the intended path; engaging blind is allowed — and
// the fight will teach you why it isn't a fight.

using UnityEngine;
using UnHumanity.Case;
using UnHumanity.Combat;

public class QueueTrigger : MonoBehaviour
{
    public CaseController caseController;
    public QueueCombatUI combatUI;
    public SightState sightState;
    public Transform player;
    public Vector3 anchorPosition = new Vector3(-6.2f, 0.4f, 41.5f);
    public float engageRadius = 5.5f;

    bool insideField;

    void Update()
    {
        if (player == null || combatUI == null || sightState == null) return;

        bool sightHeld = sightState.blend > 0.6f;
        float d = Vector3.Distance(player.position, anchorPosition);
        bool inField = d <= engageRadius;

        if (inField && sightHeld && !insideField && !combatUI.EncounterRunning
            && !DialogueUI.DialogueActive       // never over a conversation
            && !PrologueSequence.Active)         // nor during Slice 0's crack
            Engage();

        // re-engage requires stepping out of the field first
        insideField = inField && (sightHeld || insideField);
        if (!inField) insideField = false;
    }

    void Engage()
    {
        insideField = true;
        var file = caseController != null ? caseController.File : null;
        var k = new EncounterKnowledge();
        bool hasPhoto = false;
        if (file != null)
        {
            k.KnowsVictim = file.Has(ClueId.Victim);
            k.KnowsSchedule = file.Has(ClueId.Archive);
            k.KnowsTheStop = file.Has(ClueId.TheStop);
            k.Classified = file.Verdict != Verdict.Unclassified;
            // her record IS the developed photo — the case seeds the act
            hasPhoto = file.Has(ClueId.Victim);
        }
        combatUI.StartEncounter(k, photographer: false, hasPhoto: hasPhoto);
    }
}
