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
    public GameObject rowTop, rowAct, rowKit, rowItem, rowWithdraw;   // act=ACT, kit=SKILL, item=ITEM
    public Image[] holdSegments;      // ITS HOLD — drains as order is sustained
    public Image[] composureCells;    // COMPOSURE — your ability to stand in it
    public Image[] lockIcons;         // per-action padlock, shown when knowledge-locked
    public Button[] topButtons;   // the SKILL / PROCEDURE / WITHDRAW openers
    public Text[] topNames;
    public Button[] backButtons;
    public string[] menuHints = new string[4];   // fallback / act / skill / item (wired by setup)

    static readonly Color Paper = new Color32(0xE7, 0xE9, 0xEC, 0xFF);
    static readonly Color Fog = new Color32(0x97, 0x9D, 0xA8, 0xFF);
    static readonly Color Anomaly = new Color32(0xE4, 0x56, 0x8A, 0xFF);
    static readonly Color Steel = new Color32(0x3A, 0x3F, 0x48, 0xFF);
    static readonly Color Wine = new Color32(0x5E, 0x20, 0x36, 0xFF);

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
        StartEncounter(EncounterKnowledge.All, photographer, hasPhoto: true);

    public void StartEncounter(EncounterKnowledge knowledge, bool photographer, bool hasPhoto = false)
    {
        fight = new QueueEncounter(new CombatState(victimClock: 10,
            playerIsPhotographer: photographer, knowledge: knowledge));
        kit = new PlayerAction[]
        {
            new FlareAction(), new RadioCheckInAction(), new PhotographAction(),
            new EscortStepAction(), new HoldAction(), new WithdrawAction(),
            new AttackAction(),        // 6 — tier-1 ATTACK
            new SpeakTheDateAction(),  // 7 — ACT, from the case file
            new ShowHerPhotoAction(),  // 8 — ACT, from the case file
        };
        fight.State.Player.PhotoEvidence = hasPhoto;
        lastFlavor = null;
        hoverDesc = null;
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
    string hoverDesc;

    /// One arbiter owns the strip. Line 1: hover description > last action
    /// flavor > the row's hint. Line 2: the live kit readout, never dropped.
    public void SetHover(string desc) { hoverDesc = desc; UpdateStrip(); }
    public void ClearHover() { hoverDesc = null; UpdateStrip(); }

    GameObject CurrentRow()
    {
        if (rowAct != null && rowAct.activeSelf) return rowAct;
        if (rowKit != null && rowKit.activeSelf) return rowKit;
        if (rowItem != null && rowItem.activeSelf) return rowItem;
        if (rowWithdraw != null && rowWithdraw.activeSelf) return rowWithdraw;
        return rowTop;
    }

    void UpdateStrip()
    {
        if (kitStrip == null) return;
        string line1 = hoverDesc;
        if (string.IsNullOrEmpty(line1))
        {
            var row = CurrentRow();
            if (row == rowAct && menuHints.Length > 1) line1 = menuHints[1];
            else if (row == rowKit && menuHints.Length > 2) line1 = menuHints[2];
            else if (row == rowItem && menuHints.Length > 3) line1 = menuHints[3];
            else line1 = lastFlavor ?? (menuHints.Length > 0 ? menuHints[0] : "");
        }
        string line2 = "";
        if (fight != null)
        {
            var s = fight.State;
            line2 =
                $"FLARE ×{s.Player.Flares}" +
                (s.Player.HasCamera ? $" · FILM ×{s.Player.FilmRemaining}" : " · no camera") +
                " · RADIO " + (s.Knowledge.KnowsSchedule ? (s.Player.RadioCooldown > 0 ? $"cooldown {s.Player.RadioCooldown}" : "ready") : "LOCKED") +
                " · ESCORT " + (s.Knowledge.KnowsVictim ? $"{s.EscortProgress}/{s.EscortStepsNeeded}" : "LOCKED") +
                (s.SuppressionRounds > 0 ? $" · ORDER HOLDS {s.SuppressionRounds}R" : "");
        }
        kitStrip.text = string.IsNullOrEmpty(line2) ? (line1 ?? "") : (line1 ?? "") + "\n" + line2;
    }

    void ShowMenuRow(GameObject row)
    {
        if (rowTop != null) rowTop.SetActive(row == rowTop);
        if (rowAct != null) rowAct.SetActive(row == rowAct);
        if (rowKit != null) rowKit.SetActive(row == rowKit);
        if (rowItem != null) rowItem.SetActive(row == rowItem);
        if (rowWithdraw != null) rowWithdraw.SetActive(row == rowWithdraw);
        hoverDesc = null;   // selection moves; stale descriptions must not linger
        // pad/keyboard users get a live selection on every tier swap
        if (Application.isPlaying && row != null)
        {
            var first = row.GetComponentInChildren<Button>();
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (first != null && es != null) es.SetSelectedGameObject(first.gameObject);
        }
        UpdateStrip();
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
            case 4: ShowMenuRow(rowItem); break;
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
    static readonly string[] kActionCues = { "flare", "radio", "photograph", "escort", "hold", "withdraw", "photograph", "radio", "record_open" };
    static readonly string[] kActionFx = { "Flash", "Order", "Flash", "Order", "Order", null, "Impact", "Order", null };
    public string[] actionFlavor;   // serialized; wired by CombatUISetup (occupation-power voice)

    public void OnAction(int index)
    {
        if (fight == null || fight.State.Outcome != Outcome.Ongoing) return;
        SfxBoss.Play(kActionCues[index]);
        if (kitStrip != null && actionFlavor != null && index < actionFlavor.Length)
            kitStrip.text = actionFlavor[index];
        if (actionFlavor != null && index < actionFlavor.Length)
            lastFlavor = actionFlavor[index];
        bool acted = fight.PlayerPhase(kit[index]);
        if (acted && index < kActionFx.Length && kActionFx[index] != null)
            CombatFx.Play(kActionFx[index]);
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
        if (s.Outcome == Outcome.VictimEscorted || s.Outcome == Outcome.OrderReimposed) { SfxBoss.Play("win"); CombatFx.Play("Order"); return; }
        if (s.Outcome == Outcome.VictimLost) { SfxBoss.Play("lose"); return; }
        if (s.Outcome != Outcome.Ongoing) return;
        switch (s.ActiveIllegalMove)
        {
            case IllegalMove.TurnTheft: SfxBoss.Play("turn_theft"); CombatFx.Play("Glitch"); break;
            case IllegalMove.CounterDeletion: SfxBoss.Play("counter_delete"); CombatFx.Play("Glitch"); break;
            case IllegalMove.Waiting: SfxBoss.Play("waiting_lock"); CombatFx.Play("Glitch"); break;
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
        // cost lines fit their buttons (~20 chars); the hover description
        // carries the full explanation
        string[] costs =
        {
            $"1 flare · {s.Player.Flares} left",
            !s.Knowledge.KnowsSchedule ? "LOCKED: archive clue"
                : (s.Player.RadioCooldown > 0 ? $"cooldown {s.Player.RadioCooldown}" : "ready · 3R cooldown"),
            s.Player.HasCamera ? $"film {s.Player.FilmRemaining}" : "no camera",
            !s.Knowledge.KnowsVictim ? "LOCKED: her record"
                : $"{s.EscortProgress}/{s.EscortStepsNeeded} · needs order",
            "your time",
            "the case stays open",
            s.Knowledge.Classified ? "EXPOSURE ready" : "finish the file",
            !(s.Knowledge.KnowsSchedule && s.Knowledge.KnowsTheStop) ? "needs archive + stop"
                : (s.SpokeTheDate ? "spoken · once only" : "once per fight"),
            !(s.Player.PhotoEvidence && s.Knowledge.KnowsVictim) ? "needs her record"
                : (s.ShowedHerPhoto ? "she has seen it" : "her clock +2 · once"),
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

        // the two bars — pure views of engine state
        if (holdSegments != null)
        {
            // full while unclassified: functionally invincible, made visible
            int lit = s.Knowledge.Classified
                ? Mathf.Max(0, s.OrderStreakToWin - s.OrderStreak)
                : s.OrderStreakToWin;
            for (int i = 0; i < holdSegments.Length; i++)
                if (holdSegments[i] != null)
                    holdSegments[i].color = i < lit ? Wine : new Color(0.06f, 0.05f, 0.07f, 1f);
        }
        if (composureCells != null)
            for (int i = 0; i < composureCells.Length; i++)
                if (composureCells[i] != null)
                    composureCells[i].color = i < s.Composure ? Fog : new Color(0.06f, 0.05f, 0.07f, 1f);

        // the strip tracks every state change
        UpdateStrip();

        for (int i = 0; i < actionButtons.Length; i++)
        {
            if (actionButtons[i] == null) continue;   // HOLD lives in the engine, not the menu
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
            if (lockIcons != null && i < lockIcons.Length && lockIcons[i] != null
                && lockIcons[i].enabled != locked)
                lockIcons[i].enabled = locked;
        }

        if (s.Outcome != Outcome.Ongoing)
        {
            outcomeBanner.SetActive(true);
            outcomeText.text = s.Outcome switch
            {
                Outcome.VictimEscorted => "SHE IS OUT OF THE QUEUE — AGED, ALIVE",
                Outcome.OrderReimposed => "TIME HOLDS. LONG ENOUGH.",
                Outcome.VictimLost => "FOR HER IT WAS TWENTY MINUTES. IT WAS NOT.",
                Outcome.Withdrawn => s.WasEjected
                    ? "THE QUEUE STANDS YOU DOWN. IT KEEPS YOUR PLACE."
                    : "YOU STEP OUT. THE WAIT CONTINUES WITHOUT YOU.",
                _ => "",
            };
            if (s.Outcome == Outcome.Withdrawn && Application.isPlaying)
                Invoke(nameof(StopEncounter), 2.2f);
        }
    }
}
