// UN-HUMANITY combat core — the player's kit. Every action pays a
// legitimate, visible cost; that is the entire thesis. "Identify the rule
// being broken, then pay a legitimate cost to enforce it." (UH-001 §6)

namespace UnHumanity.Combat
{
    public abstract class PlayerAction
    {
        public abstract string Name { get; }
        /// Human-readable cost — the UI's cost panel is never empty for the player.
        public abstract string Cost { get; }
        /// [WAITING] blocks ordinary acts. Enforcement acts pierce it — they
        /// impose a schedule from outside the Waiter's rule.
        public virtual bool PiercesWaiting => false;
        public abstract bool CanExecute(CombatState s);
        public abstract void Execute(CombatState s);

        protected static void Suppress(CombatState s, int rounds, string why)
        {
            if (s.SuppressionRounds < rounds) s.SuppressionRounds = rounds;
            s.ActiveIllegalMove = IllegalMove.None;
            s.Player.HasWaitingStatus = false;
            s.Log.Add(new LogEvent("Order", why));
        }
    }

    /// Organization kit: a lit flare has a schedule — it burns, then it ends.
    public sealed class FlareAction : PlayerAction
    {
        public override string Name => "Flare";
        public override string Cost => "1 flare";
        public override bool PiercesWaiting => true;
        public override bool CanExecute(CombatState s) => s.Player.Flares > 0;
        public override void Execute(CombatState s)
        {
            s.Player.Flares--;
            Suppress(s, 1, "the flare burns on its own clock — one round of enforced time");
            s.Log.Add(new LogEvent(s.Player.Name, $"strikes a flare ({s.Player.Flares} left)"));
        }
    }

    /// "A scheduled event forces a schedule." Restores the deleted counter.
    /// Locked until the archive clue — you cannot invoke a schedule you
    /// don't know existed.
    public sealed class RadioCheckInAction : PlayerAction
    {
        public const int Cooldown = 3;
        public override string Name => "Radio check-in";
        public override string Cost => $"action · {Cooldown}-round cooldown";
        public override bool PiercesWaiting => true;
        public override bool CanExecute(CombatState s) =>
            s.Knowledge.KnowsSchedule && s.Player.RadioCooldown == 0;
        public override void Execute(CombatState s)
        {
            s.Player.RadioCooldown = Cooldown;
            s.CounterVisible = true;
            Suppress(s, 1, "dispatch expects the next check-in — the schedule exists again");
            s.Log.Add(new LogEvent(s.Player.Name, "radios a scheduled check-in; the turn counter returns"));
        }
    }

    /// Photographer occupation (GDD §10 open proposal): a photograph proves
    /// a moment happened. Costs real film; produces classification evidence.
    public sealed class PhotographAction : PlayerAction
    {
        public override string Name => "Photograph";
        public override string Cost => "1 exposure of film · needs light";
        public override bool PiercesWaiting => true;
        public override bool CanExecute(CombatState s) => s.Player.HasCamera && s.Player.FilmRemaining > 0;
        public override void Execute(CombatState s)
        {
            s.Player.FilmRemaining--;
            s.Player.PhotoEvidence = true;
            Suppress(s, 2, "the flash fixes one moment in sequence — order holds while it develops");
            s.Log.Add(new LogEvent(s.Player.Name, "fires the flash — a photograph proves a moment happened"));
        }
    }

    /// Walk her out. Only possible while order holds — you cannot escort
    /// someone through a wait that never ends.
    public sealed class EscortStepAction : PlayerAction
    {
        public override string Name => "Escort";
        public override string Cost => "full turn";
        // escort needs order to visibly hold THIS round — and it needs you
        // to have UNDERSTOOD her (victim clue). You can't walk out someone
        // you haven't seen.
        public override bool CanExecute(CombatState s) =>
            s.Knowledge.KnowsVictim
            && s.ActiveIllegalMove == IllegalMove.None && !s.Player.HasWaitingStatus;
        public override void Execute(CombatState s)
        {
            s.EscortProgress++;
            s.Log.Add(new LogEvent(s.Player.Name,
                $"walks the commuter toward the exit ({s.EscortProgress}/{s.EscortStepsNeeded})"));
            if (s.EscortProgress >= s.EscortStepsNeeded)
            {
                s.Outcome = Outcome.VictimEscorted;
                s.Log.Add(new LogEvent("Resolution", "she is out of the queue. Aged, alive."));
            }
        }
    }

    /// Do nothing — wait. Always legal. Always exactly what it wants.
    public sealed class HoldAction : PlayerAction
    {
        public override string Name => "Hold";
        public override string Cost => "your time";
        public override bool PiercesWaiting => true;
        public override bool CanExecute(CombatState s) => true;
        public override void Execute(CombatState s) =>
            s.Log.Add(new LogEvent(s.Player.Name, "waits. It approves."));
    }

    /// Step out of the field. Always available, pierces [WAITING] — you can
    /// always stop waiting YOURSELF. No resolution; the wait continues
    /// without you. This is how a blind engagement survives.
    public sealed class WithdrawAction : PlayerAction
    {
        public override string Name => "Withdraw";
        public override string Cost => "the case stays open";
        public override bool PiercesWaiting => true;
        public override bool CanExecute(CombatState s) => true;
        public override void Execute(CombatState s)
        {
            s.Outcome = Outcome.Withdrawn;
            s.Log.Add(new LogEvent(s.Player.Name, "steps out of the queue. It does not follow. It does not need to."));
        }
    }
}
