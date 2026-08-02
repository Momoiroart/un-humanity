// UN-HUMANITY — THE QUEUE combat UI (gray-box, dossier identity).
// The UI is the horror delivery mechanism: the round counter deletes to
// "??", the queue chips reorder to put you behind it, and its cost panel
// is empty on purpose. All layout is built by CombatUISetup (editor);
// this component only binds state to widgets.
//
// Play: T starts/stops the encounter. Click actions; on a stolen turn any
// input simply lets the round pass — your turn already happened.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnHumanity.Combat;

public class QueueCombatUI : MonoBehaviour
{
    [Header("wired by CombatUISetup")]
    public GameObject panelsRoot;
    public Text roundValue;
    public Text queueOrder;
    public Text waiterStatus;
    public Image[] victimCells;
    public Text[] logLines;
    public Button[] actionButtons;
    public Text[] actionNames;
    public Text[] actionCosts;
    public GameObject outcomeBanner;
    public Text outcomeText;

    static readonly Color Paper = new Color32(0xE7, 0xE9, 0xEC, 0xFF);
    static readonly Color Fog = new Color32(0x97, 0x9D, 0xA8, 0xFF);
    static readonly Color Anomaly = new Color32(0xE4, 0x56, 0x8A, 0xFF);
    static readonly Color Steel = new Color32(0x3A, 0x3F, 0x48, 0xFF);

    public Text knowledgeLine;    // wired by CombatUISetup

    QueueEncounter fight;
    PlayerAction[] kit;

    public static bool CombatActive { get; private set; }
    public bool EncounterRunning => fight != null;
    public CombatState StateForTests => fight?.State;
    public SightState sightState; // held at full Sight during the encounter

    void Start()
    {
        if (panelsRoot != null) panelsRoot.SetActive(false);
        if (outcomeBanner != null) outcomeBanner.SetActive(false);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.tKey.wasPressedThisFrame)
        {
            if (fight == null) StartEncounter(photographer: true);   // debug: full knowledge
            else StopEncounter();
        }
        // the stop only exists under Sight — combat holds the world there
        if (fight != null && sightState != null && Application.isPlaying)
            sightState.SetSight(Mathf.MoveTowards(sightState.blend, 1f, 2.5f * Time.deltaTime));
    }

    public void StartEncounter(bool photographer) =>
        StartEncounter(EncounterKnowledge.All, photographer);

    public void StartEncounter(EncounterKnowledge knowledge, bool photographer)
    {
        fight = new QueueEncounter(new CombatState(victimClock: 10,
            playerIsPhotographer: photographer, knowledge: knowledge));
        kit = new PlayerAction[]
        {
            new FlareAction(), new RadioCheckInAction(), new PhotographAction(),
            new EscortStepAction(), new HoldAction(), new WithdrawAction(),
        };
        CombatActive = true;
        panelsRoot.SetActive(true);
        outcomeBanner.SetActive(false);
        fight.WaiterPhase();
        RefreshAll();
    }

    public void StopEncounter()
    {
        fight = null;
        CombatActive = false;
        if (panelsRoot != null) panelsRoot.SetActive(false);
    }

    /// Button hook. On a stolen turn the attempt fails inside the engine and
    /// the round passes anyway — that is the design, not a bug.
    public void OnAction(int index)
    {
        if (fight == null || fight.State.Outcome != Outcome.Ongoing) return;
        fight.PlayerPhase(kit[index]);
        fight.EndRound();
        if (fight.State.Outcome == Outcome.Ongoing) fight.WaiterPhase();
        RefreshAll();
    }

    public void RefreshAll()
    {
        var s = fight.State;

        // round counter — deletion renders as ?? in anomaly rose
        bool deleted = s.DisplayedRound == null;
        roundValue.text = deleted ? "??" : s.DisplayedRound.Value.ToString("00");
        roundValue.color = deleted ? Anomaly : Paper;

        // queue order — theft puts your chip behind its silhouette
        queueOrder.text = s.PlayerTurnStolenThisRound
            ? "QUEUE   <color=#E4568A>THE WAITER</color> ▸ <color=#E4568A>THE WAITER</color> ▸ <color=#979DA8>YOU</color>"
            : "QUEUE   <color=#E4568A>THE WAITER</color> ▸ <color=#E7E9EC>YOU</color>";

        waiterStatus.text = s.ActiveIllegalMove switch
        {
            IllegalMove.TurnTheft => "it is ahead of you. It was always ahead of you.",
            IllegalMove.CounterDeletion => "the count is gone. The wait has no edges.",
            IllegalMove.Waiting => "[WAITING] — act after it acts. It does not act.",
            _ => "it waits.",
        };
        var forecast = fight.ForecastNext();
        if (forecast != null)
            waiterStatus.text += $"\n<color=#E4568A>{forecast}</color>";

        if (knowledgeLine != null)
        {
            knowledgeLine.text = s.Knowledge.Classified
                ? "FILE CLASSIFIED — the wait can be made to END"
                : "<color=#E4568A>UNCLASSIFIED — nothing you do here can resolve it</color>";
        }

        for (int i = 0; i < victimCells.Length; i++)
        {
            bool lit = i < s.VictimClock;
            victimCells[i].color = lit ? Fog : new Color(0.06f, 0.05f, 0.07f, 1f);
        }

        var recent = new List<LogEvent>(s.Log);
        int n = Mathf.Min(logLines.Length, recent.Count);
        for (int i = 0; i < logLines.Length; i++)
        {
            if (i < n)
            {
                var e = recent[recent.Count - n + i];
                logLines[i].text = e.ZeroCost
                    ? $"{e.Actor}: {e.What}  <color=#E4568A>[NO COST]</color>"
                    : $"{e.Actor}: {e.What}";
                logLines[i].color = (i == logLines.Length - 1) ? Paper : Fog;
            }
            else logLines[i].text = "";
        }

        // action bar — names, live costs, gating; locked = knowledge missing
        string[] costs =
        {
            $"1 flare · {s.Player.Flares} left",
            !s.Knowledge.KnowsSchedule ? "LOCKED — the transit archive"
                : (s.Player.RadioCooldown > 0 ? $"cooldown {s.Player.RadioCooldown}" : "ready · 3-round cooldown"),
            s.Player.HasCamera ? $"film {s.Player.FilmRemaining}" : "no camera",
            !s.Knowledge.KnowsVictim ? "LOCKED — the commuter"
                : $"{s.EscortProgress}/{s.EscortStepsNeeded} · needs order",
            "your time",
            "the case stays open",
        };
        for (int i = 0; i < actionButtons.Length; i++)
        {
            actionCosts[i].text = costs[i];
            bool usable = s.Outcome == Outcome.Ongoing
                          && !s.PlayerTurnStolenThisRound
                          && kit[i].CanExecute(s)
                          && (!s.Player.HasWaitingStatus || kit[i].PiercesWaiting);
            actionButtons[i].interactable = s.Outcome == Outcome.Ongoing;
            actionNames[i].color = usable ? Paper : Steel;
            actionCosts[i].color = usable ? Fog : Steel;
        }

        if (s.Outcome != Outcome.Ongoing)
        {
            outcomeBanner.SetActive(true);
            outcomeText.text = s.Outcome switch
            {
                Outcome.VictimEscorted => "SHE IS OUT OF THE QUEUE — AGED, ALIVE",
                Outcome.OrderReimposed => "TIME HOLDS. LONG ENOUGH.",
                Outcome.VictimLost => "FOR HER IT WAS TWENTY MINUTES. IT WAS NOT.",
                Outcome.Withdrawn => "YOU STEP OUT. THE WAIT CONTINUES WITHOUT YOU.",
                _ => "",
            };
            if (s.Outcome == Outcome.Withdrawn && Application.isPlaying)
                Invoke(nameof(StopEncounter), 2.2f);
        }
    }
}
