// UN-HUMANITY — the slice's story beats as dossier cards.
// S01 dispatch cold-open on play start; S05 resolution branch after a won
// QUEUE (gated by verdict, per UH-001 §7); S06 cover-life bookend.
// F / gamepad-south advances cards.

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnHumanity.Case;
using UnHumanity.Combat;

public class StoryUI : MonoBehaviour
{
    public enum Beat { Hidden, Dispatch, ResolutionChoice, Ending, Bookend, PrologueCard }

    // PrologueSequence sets this false in Awake so the case dispatch does
    // not fire on top of the prequel (all Awakes run before any Start)
    [HideInInspector] public bool autoDispatch = true;
    System.Action onCardAdvance;

    [Header("wired by StorySetup")]
    public QueueCombatUI combatUI;
    public CaseController caseController;
    public GameObject panelRoot;
    public Text title;
    public Text body;
    public Text prompt;
    public GameObject choicesRoot;
    public Button[] choiceButtons;   // A run the bus / B destroy / C let it wait
    public Text[] choiceLabels;

    public Beat Current { get; private set; } = Beat.Hidden;
    Outcome handledOutcome = Outcome.Ongoing;
    string endingText, endingTitle;

    static readonly Color Paper = new Color32(0xE7, 0xE9, 0xEC, 0xFF);
    static readonly Color Steel = new Color32(0x3A, 0x3F, 0x48, 0xFF);

    void Start()
    {
        if (Application.isPlaying && autoDispatch) ShowDispatch();
        else if (panelRoot != null) panelRoot.SetActive(false);
    }

    /// Generic dossier card driven by an external sequencer (Slice 0's
    /// prologue). F advances by invoking the callback, once.
    public void ShowCard(string t, string b, string p, System.Action onAdvance)
    {
        onCardAdvance = onAdvance;
        Show(Beat.PrologueCard, t, b, p);
    }

    void Update()
    {
        // watch the encounter for a terminal outcome
        var s = combatUI != null ? combatUI.StateForTests : null;
        if (s != null && s.Outcome != Outcome.Ongoing && s.Outcome != handledOutcome
            && Current == Beat.Hidden)
        {
            handledOutcome = s.Outcome;
            if (s.Outcome == Outcome.VictimEscorted || s.Outcome == Outcome.OrderReimposed)
                Invoke(nameof(BeginResolution), 2.4f);
            else if (s.Outcome == Outcome.VictimLost)
                Invoke(nameof(BeginFailure), 2.4f);
            // Withdrawn: no beat — the case simply stays open
        }

        if (Current == Beat.Hidden || Current == Beat.ResolutionChoice) return;
        var kb = Keyboard.current;
        var pad = Gamepad.current;
        bool advance = (kb != null && kb.fKey.wasPressedThisFrame)
                    || (pad != null && pad.buttonSouth.wasPressedThisFrame);
        if (!advance) return;

        SfxBoss.Play("ui_click");
        switch (Current)
        {
            case Beat.Dispatch: Hide(); break;
            case Beat.Ending: ShowBookend(); break;
            case Beat.Bookend: Hide(); break;
            case Beat.PrologueCard:
                var cb = onCardAdvance; onCardAdvance = null;
                Hide();   // clear the panel; the sequencer shows what's next
                cb?.Invoke();
                break;
        }
    }

    public void ShowDispatch()
    {
        Show(Beat.Dispatch, "FILE UH-001 — 05:47",
            "You already know what you are. You already know who we are. This is where we find out " +
            "whether you're any use to us. You work it alone — no handler in your ear, no friend at " +
            "your back. We are reading over your shoulder.\n\n" +
            "Route 9, northbound. Transit complaints: drivers skip the stop — \"someone's already waiting.\" " +
            "One commuter missing. Her phone hasn't left that stop in nine days.\n\n" +
            "Work it like this: examine what responds (F). Hold E to See what is actually there — Sight costs, watch the meter. " +
            "Evidence goes in the case file (TAB). The file must be CLASSIFIED before the thing at the end of the road can be resolved. " +
            "Understanding is the weapon. There is no other weapon.\n\n" +
            "Begin.",
            "F — ACCEPT THE CASE");
    }

    void BeginResolution()
    {
        combatUI.StopEncounter();
        var verdict = caseController != null ? caseController.File.Verdict : Verdict.Unclassified;
        Show(Beat.ResolutionChoice, "RESOLUTION — THE CALL IS YOURS",
            "The queue is held. What happens to the thing at the stop is a decision,\nnot a fight.", null);
        choicesRoot.SetActive(true);

        // A — end the wait: only a REMNANT reading makes this thinkable
        bool remnant = verdict == Verdict.Remnant;
        choiceButtons[0].interactable = remnant;
        choiceLabels[0].text = remnant
            ? "RUN THE 1974 BUS\n<size=16>end the wait</size>"
            : "RUN THE 1974 BUS\n<size=16>LOCKED — you never understood it</size>";
        choiceLabels[0].color = remnant ? Paper : Steel;
        choiceLabels[1].text = "DESTROY IT\n<size=16>the wait disperses</size>";
        choiceLabels[2].text = "LET IT WAIT\n<size=16>contain and walk away</size>";
    }

    void BeginFailure()
    {
        combatUI.StopEncounter();
        Show(Beat.Ending, "CASE FAILED",
            "For her it was twenty minutes.\n\nIt was not.\n\nThe Organization will send someone else.",
            "F — CONTINUE");
        endingTitle = null;
    }

    public void OnChoice(int index)
    {
        choicesRoot.SetActive(false);
        switch (index)
        {
            case 0:
                endingTitle = "THE WAIT ENDS";
                endingText = "23:40. A mothballed bus, depot dust still on it, runs Route 9 northbound. Once.\n\n" +
                             "It boards. The stop empties. She is recovered — aged, alive.\n\n" +
                             "The destination sign reads a street that no longer exists.";
                break;
            case 1:
                endingTitle = "IT DISPERSES";
                endingText = "The wait doesn't end. It scatters.\n\n" +
                             "Every stop on Route 9 is a little patient now.\n\n" +
                             "She is recovered. Worse off. Normalcy takes the hit.";
                break;
            default:
                endingTitle = "THE WAIT CONTINUES";
                endingText = "Signage reroutes the street. Humans stop coming.\n\n" +
                             "It waits, alone, correctly.\n\n" +
                             "The case stays open. The quietest option always does.";
                break;
        }
        Show(Beat.Ending, endingTitle, endingText, "F — CONTINUE");
    }

    void ShowBookend()
    {
        Show(Beat.Bookend, "COVER LIFE — 00:52",
            "Your bus home is the 00:58. You wait at your own stop on an ordinary street — " +
            "homework unstarted, phone at four percent, school in six hours.\n\n" +
            "The phone buzzes. One line, from the number you were made to save:\n" +
            "\"Told them you'd walk through it. Get some sleep. — see you at the gate.\"\n\n" +
            "The bench is just a bench. You checked. You check the schedule anyway. Twice.\n\n" +
            "FILE UH-001 · CLOSED. You're one of them now.",
            "F — CLOCK OUT");
    }

    void Show(Beat beat, string t, string b, string p)
    {
        Current = beat;
        panelRoot.SetActive(true);
        choicesRoot.SetActive(false);
        title.text = t;
        body.text = b;
        prompt.text = p ?? "";
    }

    void Hide()
    {
        Current = Beat.Hidden;
        panelRoot.SetActive(false);
    }
}
