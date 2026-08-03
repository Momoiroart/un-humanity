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
    public GameObject engageHint;   // "T — engage" chip; hidden while fighting
    public Text kitStrip;           // occupation-power flavor above the action bar

    // ── the familiar command grammar (Undertale/E33 shell) ──
    // Tier-1 is always the same four verbs; submenus swap in place.
    public GameObject rowTop, rowAct, rowKit, rowWithdraw;   // act = PROCEDURE, kit = SKILL
    public Button[] topButtons;   // the SKILL / PROCEDURE / WITHDRAW openers
    public Text[] topNames;
    public Button[] backButtons;
    public string[] menuHints = new string[3];   // fallback / procedure / skill (wired by setup)

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
        // Esc / pad-East back out of a submenu; at tier-1 they do nothing
        // (never StopEncounter, never Withdraw)
        var pad = Gamepad.current;
        bool backPressed = (kb != null && kb.escapeKey.wasPressedThisFrame)
                        || (pad != null && pad.buttonEast.wasPressedThisFrame);
        if (CombatActive && backPressed && rowTop != null && !rowTop.activeSelf)
            OnBack();
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
            new AttackAction(),   // index 6 — tier-1 ATTACK
        };
        lastFlavor = null;
        CombatActive = true;
        CancelInvoke(nameof(StopEncounter));   // a withdraw-then-quick-restart must not kill the new fight
        var openRecord = FindFirstObjectByType<ReadingUI>();
        if (openRecord != null && openRecord.IsOpen) openRecord.Close();   // no orphaned record over the stage
        panelsRoot.SetActive(true);
        outcomeBanner.SetActive(false);
        if (engageHint != null) engageHint.SetActive(false);
        ShowMenuRow(rowTop);
        SfxBoss.Play("combat_start");
        fight.WaiterPhase();
        RefreshAll();
        PlayWaiterCue();
    }

    // ── menu navigation ──
    string lastFlavor;

    /// Tier-1's strip is ambient intel: the last action's cost line over a
    /// live kit readout — the CHECK payload with zero clicks.
    string ComposeTopStrip()
    {
        if (fight == null) return menuHints != null && menuHints.Length > 0 ? menuHints[0] ?? "" : "";
        var s = fight.State;
        string live =
            $"FLARE ×{s.Player.Flares}" +
            (s.Player.HasCamera ? $" · FILM ×{s.Player.FilmRemaining}" : " · no camera") +
            " · RADIO " + (s.Knowledge.KnowsSchedule ? (s.Player.RadioCooldown > 0 ? $"cooldown {s.Player.RadioCooldown}" : "ready") : "LOCKED") +
            " · ESCORT " + (s.Knowledge.KnowsVictim ? $"{s.EscortProgress}/{s.EscortStepsNeeded}" : "LOCKED");
        return string.IsNullOrEmpty(lastFlavor) ? live : lastFlavor + "\n" + live;
    }

    void ShowMenuRow(GameObject row)
    {
        if (rowTop != null) rowTop.SetActive(row == rowTop);
        if (rowAct != null) rowAct.SetActive(row == rowAct);
        if (rowKit != null) rowKit.SetActive(row == rowKit);
        if (rowWithdraw != null) rowWithdraw.SetActive(row == rowWithdraw);
        // pad/keyboard users get a live selection on every tier swap
        if (Application.isPlaying && row != null)
        {
            var first = row.GetComponentInChildren<Button>();
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (first != null && es != null) es.SetSelectedGameObject(first.gameObject);
        }
        if (kitStrip == null || menuHints == null) return;
        if (row == rowTop) kitStrip.text = ComposeTopStrip();
        else if (row == rowAct && menuHints.Length > 1) kitStrip.text = menuHints[1] ?? "";
        else if (row == rowKit && menuHints.Length > 2) kitStrip.text = menuHints[2] ?? "";
    }

    /// Tier-1 opener hook: 1 = PROCEDURE, 2 = SKILL, 3 = WITHDRAW (confirm
    /// row). ATTACK is a real action and binds OnAction(6) directly.
    public void OnTop(int i)
    {
        if (fight == null || fight.State.Outcome != Outcome.Ongoing) return;
        SfxBoss.Play("ui_click");
        switch (i)
        {
            case 1: ShowMenuRow(rowAct); break;
            case 2: ShowMenuRow(rowKit); break;
            case 3: ShowMenuRow(rowWithdraw); break;
        }
    }

    public void OnBack()
    {
        SfxBoss.Play("ui_click");
        ShowMenuRow(rowTop);
    }

    public void StopEncounter()
    {
        fight = null;
        CombatActive = false;
        if (panelsRoot != null) panelsRoot.SetActive(false);
        if (engageHint != null) engageHint.SetActive(true);
    }

    /// Button hook. On a stolen turn the attempt fails inside the engine and
    /// the round passes anyway — that is the design, not a bug.
    static readonly string[] kActionCues = { "flare", "radio", "photograph", "escort", "hold", "withdraw", "photograph" };
    public string[] actionFlavor;   // serialized; wired by CombatUISetup (occupation-power voice)

    public void OnAction(int index)
    {
        if (fight == null || fight.State.Outcome != Outcome.Ongoing) return;
        SfxBoss.Play(kActionCues[index]);
        if (kitStrip != null && actionFlavor != null && index < actionFlavor.Length)
            kitStrip.text = actionFlavor[index];
        if (actionFlavor != null && index < actionFlavor.Length)
            lastFlavor = actionFlavor[index];
        fight.PlayerPhase(kit[index]);
        fight.EndRound();
        if (fight.State.Outcome == Outcome.Ongoing) fight.WaiterPhase();
        ShowMenuRow(rowTop);
        RefreshAll();
        PlayWaiterCue();
    }

    /// The cheat is HEARD, not just printed — one cue per illegal move.
    void PlayWaiterCue()
    {
        if (fight == null) return;
        var s = fight.State;
        if (s.Outcome == Outcome.VictimEscorted || s.Outcome == Outcome.OrderReimposed) { SfxBoss.Play("win"); return; }
        if (s.Outcome == Outcome.VictimLost) { SfxBoss.Play("lose"); return; }
        if (s.Outcome != Outcome.Ongoing) return;
        switch (s.ActiveIllegalMove)
        {
            case IllegalMove.TurnTheft: SfxBoss.Play("turn_theft"); break;
            case IllegalMove.CounterDeletion: SfxBoss.Play("counter_delete"); break;
            case IllegalMove.Waiting: SfxBoss.Play("waiting_lock"); break;
        }
    }

    public void RefreshAll()
    {
        var s = fight.State;

        // round counter — deletion renders as ?? in anomaly rose
        bool deleted = s.DisplayedRound == null;
        roundValue.text = deleted ? "??" : s.DisplayedRound.Value.ToString("00");
        roundValue.color = deleted ? Anomaly : Paper;

        // queue order — theft puts your chip behind its silhouette. The
        // stolen slot reads as YOURS, TAKEN (steel) so two waiter chips
        // never look like a binding bug; rose is reserved for ?? deletion
        // (palette law: one accent per screen).
        queueOrder.text = s.PlayerTurnStolenThisRound
            ? "QUEUE   <color=#E7E9EC>THE WAITER</color> ▸ <color=#3A3F48>YOURS — TAKEN</color> ▸ <color=#979DA8>YOU</color>"
            : "QUEUE   <color=#E7E9EC>THE WAITER</color> ▸ <color=#979DA8>YOU</color>";

        waiterStatus.text = s.ActiveIllegalMove switch
        {
            IllegalMove.TurnTheft => "it is ahead of you. It was always ahead of you.",
            IllegalMove.CounterDeletion => "the count is gone. The wait has no edges.",
            IllegalMove.Waiting => "[WAITING] — act after it acts. It does not act.",
            _ => "it waits.",
        };
        var forecast = fight.ForecastNext();
        if (forecast != null)
            waiterStatus.text += $"\n<color=#E7E9EC>{forecast}</color>";

        if (knowledgeLine != null)
        {
            // the weakpoint, in words: what it IS, how it cheats, what ends it
            knowledgeLine.text = s.Knowledge.Classified
                ? "TEMPORAL RESIDUAL. Cheat: a wait that pays no time. Weakpoint: sustained order — hold it, and the wait ends."
                : "<color=#E7E9EC>UNCLASSIFIED. You don't know what this is. Nothing here can end. Withdrawing is not losing. Finish the file.</color>";
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
                    ? $"{e.Actor}: {e.What}  <color=#E7E9EC>[NO COST]</color>"
                    : $"{e.Actor}: {e.What}";
                logLines[i].color = (i == n - 1) ? Paper : Fog;   // newest line, not last slot
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
            s.Knowledge.Classified ? "EXPOSURE ready — the file names the cheat"
                : "it will not notice — finish the file",
        };
        // outcome ends the menu conversation: back to tier-1, everything
        // read-only under the banner
        bool ongoing = s.Outcome == Outcome.Ongoing;
        if (!ongoing && rowTop != null && !rowTop.activeSelf) ShowMenuRow(rowTop);
        if (topButtons != null) foreach (var tb in topButtons) if (tb != null) tb.interactable = ongoing;
        if (backButtons != null) foreach (var bb in backButtons) if (bb != null) bb.interactable = ongoing;
        // stolen-round telegraph on tier-1: the openers dim to Fog — the
        // row itself says this round is already gone (ATTACK dims via the
        // action loop below)
        if (topNames != null)
            foreach (var tn in topNames)
                if (tn != null) tn.color = s.PlayerTurnStolenThisRound ? Fog : Paper;

        // the ambient kit readout tracks every state change at tier-1
        if (rowTop != null && rowTop.activeSelf && kitStrip != null)
            kitStrip.text = ComposeTopStrip();

        for (int i = 0; i < actionButtons.Length; i++)
        {
            actionCosts[i].text = costs[i];
            // three visually distinct tiers:
            //   locked  — knowledge/resource gate: disabled tint + steel text
            //   denied  — turn stolen / [WAITING]: dimmed but LIVE (clicking
            //             passes the round — that is the design)
            //   usable  — paper-bright
            bool locked = !kit[i].CanExecute(s);
            bool denied = s.PlayerTurnStolenThisRound
                          || (s.Player.HasWaitingStatus && !kit[i].PiercesWaiting);
            actionButtons[i].interactable = s.Outcome == Outcome.Ongoing && !locked;
            actionNames[i].color = locked ? Steel : (denied ? Fog : Paper);
            actionCosts[i].color = locked ? Steel : Fog;
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
