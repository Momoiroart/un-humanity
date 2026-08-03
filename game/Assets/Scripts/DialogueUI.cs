// UN-HUMANITY — the conversation box (Undertale-style, bottom of screen).
// Drives a DialogueRunner: speaker tab, body, [F] advance, two choice
// buttons at a branch. Per-line voice bark. On finish, hands the node to
// CaseController.TryCollect so the witness's evidence lands exactly as a
// plain examine would — dialogue IS the collection, not a detour.
//
// Every input-contention fix from the integration review lives here:
//  • static DialogueActive → the modal chain (chrome, TAB, combat) sees us
//  • open-frame + resolve-frame latches → one F never leaks into two systems
//  • the finish-path TryCollect defers one frame → the record can't be
//    closed by the same F that ended the conversation
//  • at a choice, the EventSystem owns input; we stop polling advance

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnHumanity.Case;

public class DialogueUI : MonoBehaviour
{
    [Header("wired by CaseSetup")]
    public CaseController caseController;
    public GameObject panelRoot;
    public Text speakerTab;
    public Text body;
    public GameObject advanceChip;
    public GameObject choiceRoot;
    public Button choiceA, choiceB;
    public Text choiceALabel, choiceBLabel;

    // per-speaker voice bark cue names (registered in AudioSetup)
    public string barkYou = "bark_player";
    public string barkOldMan = "bark_oldman";
    public string barkCommuter = "bark_commuter";

    public static bool DialogueActive { get; private set; }
    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    DialogueRunner runner;
    ClueNode node;
    int openFrame = -1;
    int resolveFrame = -1;
    bool pendingCollect;

    void Start() { if (panelRoot != null) panelRoot.SetActive(false); }

    /// True once per node — the caller (PlayerInteractor) checks this so a
    /// re-examine goes straight to the record instead of replaying.
    public bool HasTalked(ClueNode n) => n != null && n.hasTalked;

    public void Begin(Conversation convo, ClueNode forNode)
    {
        runner = new DialogueRunner(convo);
        node = forNode;
        prompt = convo.ChoicePrompt;
        if (choiceALabel != null) choiceALabel.text = convo.OptionA;
        if (choiceBLabel != null) choiceBLabel.text = convo.OptionB;
        DialogueActive = true;
        openFrame = Time.frameCount;
        pendingCollect = false;
        panelRoot.SetActive(true);
        Render();
    }

    void Update()
    {
        // the finish-path collect runs a frame LATE, so the F/Esc that ended
        // the conversation is already stale before the record sees input
        if (pendingCollect && Time.frameCount > resolveFrame)
        {
            pendingCollect = false;
            var n = node;
            Close();
            if (n != null && caseController != null) caseController.TryCollect(n);
            return;
        }
        if (!IsOpen || runner == null) return;

        // at a choice, the EventSystem drives the buttons — we do not poll
        if (runner.AtChoice) return;

        var kb = Keyboard.current;
        var pad = Gamepad.current;
        bool onOpenFrame = Time.frameCount == openFrame || Time.frameCount == resolveFrame;
        bool advance = !onOpenFrame &&
            ((kb != null && kb.fKey.wasPressedThisFrame)
             || (pad != null && pad.buttonSouth.wasPressedThisFrame));
        bool skip = !onOpenFrame && kb != null && kb.escapeKey.wasPressedThisFrame;

        if (skip || advance)
        {
            SfxBoss.Play("dialogue_advance");
            runner.Advance();
            if (runner.IsDone) FinishConversation();
            else Render();
        }
    }

    // choice button hooks (wired by CaseSetup)
    public void OnChooseA() => Resolve(true);
    public void OnChooseB() => Resolve(false);

    void Resolve(bool optionA)
    {
        if (runner == null || !runner.AtChoice) return;
        SfxBoss.Play("dialogue_choice");
        runner.Choose(optionA);
        resolveFrame = Time.frameCount;   // swallow this frame's advance
        Render();
    }

    void Render()
    {
        if (runner.AtChoice)
        {
            body.text = prompt ?? "…";
            speakerTab.text = "YOU";
            if (advanceChip != null) advanceChip.SetActive(false);
            if (choiceRoot != null) choiceRoot.SetActive(true);
            var es = EventSystem.current;
            if (es != null && choiceA != null) es.SetSelectedGameObject(choiceA.gameObject);
            return;
        }
        if (choiceRoot != null) choiceRoot.SetActive(false);
        if (advanceChip != null) advanceChip.SetActive(true);
        var line = runner.Current;
        speakerTab.text = line.Speaker;
        body.text = line.Text;
        SfxBoss.Play(BarkFor(line.Speaker));
    }

    string prompt;

    void FinishConversation()
    {
        if (node != null) node.hasTalked = true;
        pendingCollect = true;
        resolveFrame = Time.frameCount;   // collect fires next frame
        if (advanceChip != null) advanceChip.SetActive(false);
    }

    void Close()
    {
        DialogueActive = false;
        runner = null;
        node = null;
        if (panelRoot != null) panelRoot.SetActive(false);
        var es = EventSystem.current;
        if (es != null) es.SetSelectedGameObject(null);
    }

    string BarkFor(string speaker)
    {
        switch (speaker)
        {
            case "OLD MAN": return barkOldMan;
            case "COMMUTER": return barkCommuter;
            default: return barkYou;
        }
    }
}
