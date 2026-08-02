// UN-HUMANITY combat core — domain model. Pure C#: no UnityEngine.
// Source: UH-001 §6 "THE QUEUE" — cheating-as-boss-design in a system with
// visible rules. Win condition is never damage; it is re-imposing time's
// rules long enough to escort the victim out (or holding order until the
// resolution fires).

using System.Collections.Generic;

namespace UnHumanity.Combat
{
    public enum Outcome
    {
        Ongoing,
        VictimEscorted,   // walked her down the escort lane — true win
        OrderReimposed,   // held the schedule long enough to disengage/resolve
        VictimLost,       // her clock ran out
        Withdrawn,        // stepped out of the field — no resolution, no shame
    }

    /// What the player UNDERSTANDS going into the fight. Knowledge is the
    /// unlock: engage blind and the Waiter is functionally invincible —
    /// you can poke at the wait, and you can leave.
    public struct EncounterKnowledge
    {
        public bool KnowsVictim;      // Victim clue  -> Escort available
        public bool KnowsSchedule;    // Archive clue -> Radio check-in works
        public bool KnowsTheStop;     // TheStop clue -> its next move is forecast
        public bool Classified;      // a verdict    -> the fight can RESOLVE at all

        public static EncounterKnowledge All => new EncounterKnowledge
        { KnowsVictim = true, KnowsSchedule = true, KnowsTheStop = true, Classified = true };

        public static EncounterKnowledge None => new EncounterKnowledge();
    }

    /// The Waiter's rule-breaks — each one reads as a VIOLATION in the UI.
    public enum IllegalMove
    {
        None,
        TurnTheft,        // "you're behind it in the queue" — takes your next turn
        CounterDeletion,  // the round counter UI is deleted; the fight has always been happening
        Waiting,          // [WAITING]: you may act only after it acts. It never acts.
    }

    public sealed class Combatant
    {
        public string Name;
        public bool IsPlayer;
        public bool HasWaitingStatus;   // [WAITING] affliction
        public int Flares;              // consumable — Organization kit
        public int RadioCooldown;       // rounds until check-in is available again
        public bool HasCamera;          // photographer occupation (proposal)
        public int FilmRemaining;       // the photograph's legitimate cost
        public bool PhotoEvidence;      // a developed photo of the bench — classification evidence

        public Combatant(string name, bool isPlayer)
        {
            Name = name;
            IsPlayer = isPlayer;
        }
    }

    /// One entry in the combat log — the UI renders these; tests assert on them.
    public readonly struct LogEvent
    {
        public readonly string Actor;
        public readonly string What;
        public readonly bool ZeroCost;   // the Waiter's cost panel is visibly empty, on purpose

        public LogEvent(string actor, string what, bool zeroCost = false)
        {
            Actor = actor;
            What = what;
            ZeroCost = zeroCost;
        }

        public override string ToString() => $"{Actor}: {What}{(ZeroCost ? " [NO COST]" : "")}";
    }

    public sealed class CombatState
    {
        public readonly Combatant Player;
        public readonly Combatant Waiter;
        public readonly Combatant Victim;
        public readonly List<LogEvent> Log = new();

        public int InternalRound = 1;          // never stops counting
        public bool CounterVisible = true;     // what the UI may show
        public int? DisplayedRound => CounterVisible ? InternalRound : (int?)null;

        public IllegalMove ActiveIllegalMove = IllegalMove.None;
        public bool PlayerTurnStolenThisRound;
        public int SuppressionRounds;          // >0: order enforced, Waiter cannot cheat

        public int VictimClock;                // rounds until she is lost
        public int EscortProgress;             // steps down the 6.2 m lane
        public readonly int EscortStepsNeeded;
        public int OrderStreak;                // consecutive clean rounds (counter visible, no cheat landed)
        public readonly int OrderStreakToWin;

        public Outcome Outcome = Outcome.Ongoing;
        public EncounterKnowledge Knowledge;

        public CombatState(int victimClock = 10, int escortSteps = 4, int orderStreakToWin = 3,
                           bool playerIsPhotographer = false, EncounterKnowledge? knowledge = null)
        {
            Knowledge = knowledge ?? EncounterKnowledge.All;
            Player = new Combatant("Agent", isPlayer: true) { Flares = 2, HasCamera = playerIsPhotographer, FilmRemaining = playerIsPhotographer ? 1 : 0 };
            Waiter = new Combatant("The Waiter", isPlayer: false);
            Victim = new Combatant("The Commuter", isPlayer: false);
            VictimClock = victimClock;
            EscortStepsNeeded = escortSteps;
            OrderStreakToWin = orderStreakToWin;
        }
    }
}
