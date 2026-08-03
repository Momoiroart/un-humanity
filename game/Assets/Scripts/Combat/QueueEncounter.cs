// UN-HUMANITY combat core — THE QUEUE, the scripted encounter engine.
// Round shape: the Waiter cheats first (unless order is enforced), the
// player acts (if their turn wasn't stolen), the victim's clock ticks.
// Damage does not exist here. You cannot win by hurting it.

namespace UnHumanity.Combat
{
    public sealed class QueueEncounter
    {
        public readonly CombatState State;

        public QueueEncounter(CombatState state) => State = state;

        /// The Waiter's opening script, then a cycle. Deterministic — the
        /// horror is legibility, not randomness.
        static IllegalMove ScriptFor(int round) => round switch
        {
            1 => IllegalMove.TurnTheft,
            2 => IllegalMove.CounterDeletion,
            3 => IllegalMove.Waiting,
            _ => (round % 2 == 0) ? IllegalMove.TurnTheft : IllegalMove.Waiting,
        };

        /// Phase 1 — the Waiter's non-turn. It never acts. It cheats.
        public void WaiterPhase()
        {
            var s = State;
            if (s.Outcome != Outcome.Ongoing) return;

            s.PlayerTurnStolenThisRound = false;

            // Suppression is consumed HERE, at the moment it denies a cheat —
            // an enforcement act cast on round N protects round N+1. (Bug
            // caught by tests: decrementing at end-of-round made every
            // suppression expire before it ever protected anything.)
            if (s.SuppressionRounds > 0)
            {
                s.SuppressionRounds--;
                s.ActiveIllegalMove = IllegalMove.None;
                s.Log.Add(new LogEvent(s.Waiter.Name,
                    "waits, held inside the schedule — its cheat this round is cancelled. Composure holds", zeroCost: true));
                return;
            }

            var move = ScriptFor(s.InternalRound);
            s.ActiveIllegalMove = move;
            // a cheat LANDS exactly here — it costs you composure, once per
            // fire (aftermath states cost streak progress, never composure)
            s.Composure--;
            switch (move)
            {
                case IllegalMove.TurnTheft:
                    s.PlayerTurnStolenThisRound = true;
                    s.Log.Add(new LogEvent(s.Waiter.Name,
                        "is ahead of you in the queue — your turn this round is stolen; anything you pick will fail. Composure −1", zeroCost: true));
                    break;
                case IllegalMove.CounterDeletion:
                    s.CounterVisible = false;
                    s.Log.Add(new LogEvent(s.Waiter.Name,
                        "erases the round counter — no round counts as clean until it is restored. Composure −1", zeroCost: true));
                    break;
                case IllegalMove.Waiting:
                    s.Player.HasWaitingStatus = true;
                    s.Log.Add(new LogEvent(s.Waiter.Name,
                        "sets [WAITING] — you may act after it acts. It does not act. Ordinary moves will fail. Composure −1", zeroCost: true));
                    break;
            }
        }

        /// Phase 2 — the player's turn. Returns false if the act was illegal
        /// under current state (stolen turn / WAITING gate / unpayable cost).
        public bool PlayerPhase(PlayerAction action)
        {
            var s = State;
            if (s.Outcome != Outcome.Ongoing) return false;

            if (s.PlayerTurnStolenThisRound)
            {
                s.Log.Add(new LogEvent("Queue", "your action fails — the turn was stolen this round. The round passes anyway; act next round"));
                return false;
            }
            if (s.Player.HasWaitingStatus && !action.PiercesWaiting)
            {
                s.Log.Add(new LogEvent("Queue", $"[WAITING] blocks {action.Name} — it needs a normal turn. Use a move that forces a schedule instead"));
                return false;
            }
            if (!action.CanExecute(s))
            {
                s.Log.Add(new LogEvent("Queue", $"{action.Name}: cost cannot be paid ({action.Cost})"));
                return false;
            }
            action.Execute(s);
            return true;
        }

        /// Phase 3 — the wait grinds on. Clocks tick, wins/losses resolve.
        public void EndRound()
        {
            var s = State;
            if (s.Outcome != Outcome.Ongoing) return;

            // she pays its time, every round, until she is out
            s.VictimClock--;
            if (s.VictimClock <= 0 && s.Outcome == Outcome.Ongoing)
            {
                s.Outcome = Outcome.VictimLost;
                s.Log.Add(new LogEvent("Resolution", "for her it was twenty minutes. It was not."));
                return;
            }

            // ejection resolves AFTER her clock — ties go to the loss that
            // matters. You never die here; you stop being able to stand in it.
            if (s.Composure <= 0 && s.Outcome == Outcome.Ongoing)
            {
                s.Outcome = Outcome.Withdrawn;
                s.WasEjected = true;
                s.Log.Add(new LogEvent("Queue",
                    "composure 0 — you can no longer stand in the queue. You are outside it now; the case stays open. You do not remember walking"));
                return;
            }

            // order streak: a clean, counted round moves the fight toward
            // resolution — but ONLY if the player has classified the thing.
            // Un-understood, the wait has no failure condition: it is,
            // functionally, invincible. Withdraw and learn.
            bool clean = s.CounterVisible && s.ActiveIllegalMove == IllegalMove.None
                         && !s.PlayerTurnStolenThisRound && !s.Player.HasWaitingStatus;
            s.OrderStreak = clean ? s.OrderStreak + 1 : 0;
            if (s.Knowledge.Classified && s.OrderStreak >= s.OrderStreakToWin)
            {
                s.Outcome = Outcome.OrderReimposed;
                s.Log.Add(new LogEvent("Resolution", "time holds. Long enough."));
                return;
            }

            if (s.Player.RadioCooldown > 0) s.Player.RadioCooldown--;
            s.InternalRound++;   // always. Deleting the display never stopped it.
        }

        /// Convenience: run one full round with the given player choice.
        public void RunRound(PlayerAction action)
        {
            WaiterPhase();
            PlayerPhase(action);
            EndRound();
        }

        /// The stop clue teaches you to read the queue: what it will do
        /// next round, if order isn't enforced. Null without the knowledge.
        public string ForecastNext()
        {
            if (!State.Knowledge.KnowsTheStop) return null;
            if (State.SuppressionRounds > 0) return "the queue holds — nothing bends next round";
            return ScriptFor(State.InternalRound + 1) switch
            {
                IllegalMove.TurnTheft => "the queue bends — it will be ahead of you again",
                IllegalMove.CounterDeletion => "the count is about to stop mattering",
                IllegalMove.Waiting => "it is preparing to make you [WAITING]",
                _ => null,
            };
        }
    }
}
