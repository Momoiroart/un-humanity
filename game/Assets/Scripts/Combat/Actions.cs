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

    /// ATTACK — the floor (Combat_System §4). A snapshot jab: modest,
    /// human, always available, no matchup lock, no evidence requirement.
    /// It can never win — but from a correctly diagnosed matchup it is the
    /// EXPOSURE delivery verb (§7): the protagonist's craft is Time, the
    /// Waiter cheats Time, and a classified file names the cheat — so the
    /// jab suspends the next illegal move. Blind, it lands on nothing it
    /// needs. Ordinary act: [WAITING] blocks it.
    public sealed class AttackAction : PlayerAction
    {
        public override string Name => "Snapshot jab";
        public override string Cost => "always yours";
        public override bool CanExecute(CombatState s) => true;
        public override void Execute(CombatState s)
        {
            if (s.Knowledge.Classified)
            {
                Suppress(s, 1, "EXPOSURE — the cheat is named from the file. It cannot cheat next round; its hold keeps draining");
                s.Log.Add(new LogEvent(s.Player.Name, "jabs through the frame — its next cheat is suspended (1 round). The file gave the strike its name"));
            }
            else
            {
                s.Log.Add(new LogEvent(s.Player.Name, "jabs. It lands. No effect — without a complete file the strike has no name, and it does not notice"));
            }
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
            Suppress(s, 1, "the flare burns on its own clock — no cheat next round (1-round block)");
            s.Log.Add(new LogEvent(s.Player.Name, $"strikes a flare — its next cheat is blocked for 1 round ({s.Player.Flares} left)"));
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
            Suppress(s, 1, "dispatch expects the next check-in — the schedule exists again. No cheat next round");
            s.Log.Add(new LogEvent(s.Player.Name, "radios a scheduled check-in — the round counter returns and its next cheat is blocked (1 round)"));
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
            Suppress(s, 2, "the flash fixes the sequence — no cheats for 2 rounds while it develops");
            s.Log.Add(new LogEvent(s.Player.Name, "fires the flash — cheats blocked for 2 rounds; the photo is filed as evidence (1 film spent)"));
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
                $"walks the commuter one step toward the exit ({s.EscortProgress}/{s.EscortStepsNeeded} — at {s.EscortStepsNeeded} she is out)"));
            if (s.EscortProgress >= s.EscortStepsNeeded)
            {
                s.Outcome = Outcome.VictimEscorted;
                s.Log.Add(new LogEvent("Resolution", "she is out of the queue — the encounter is won. Aged, alive."));
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
            s.Log.Add(new LogEvent(s.Player.Name, "waits — the round passes, nothing spent, nothing gained. It approves"));
    }

    /// ACT, from the case file: tell it the date. Route 9's night service
    /// ended October 1974 — a spoken date is a schedule, and it cannot
    /// out-wait a schedule. Once per fight; it heard you the first time.
    public sealed class SpeakTheDateAction : PlayerAction
    {
        public override string Name => "Speak the date";
        public override string Cost => "once per fight";
        public override bool PiercesWaiting => true;
        public override bool CanExecute(CombatState s) =>
            s.Knowledge.KnowsSchedule && s.Knowledge.KnowsTheStop && !s.SpokeTheDate;
        public override void Execute(CombatState s)
        {
            s.SpokeTheDate = true;
            Suppress(s, 2, "the date is spoken — the wait hears its own ending. No cheats for 2 rounds");
            s.Log.Add(new LogEvent(s.Player.Name,
                "speaks today's date and the scheduled departure — [WAITING] breaks. No cheats for 2 rounds"));
        }
    }

    /// ACT, from the case file: show the commuter her own photograph.
    /// A photo proves a moment happened — she remembers she was leaving.
    /// Once per fight; after that it is only paper.
    public sealed class ShowHerPhotoAction : PlayerAction
    {
        public override string Name => "Show her the photo";
        public override string Cost => "her clock +2 · once";
        public override bool CanExecute(CombatState s) =>
            s.Player.PhotoEvidence && s.Knowledge.KnowsVictim && !s.ShowedHerPhoto;
        public override void Execute(CombatState s)
        {
            s.ShowedHerPhoto = true;
            s.VictimClock = System.Math.Min(s.VictimClock + 2, s.VictimClockMax);
            s.Log.Add(new LogEvent(s.Player.Name,
                $"shows her the photograph — her clock recovers 2 (now {s.VictimClock}). She remembers she was going somewhere"));
        }
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
            s.Log.Add(new LogEvent(s.Player.Name, "steps out of the queue — encounter over, nothing lost, the case stays open. It does not follow. It does not need to"));
        }
    }
}
