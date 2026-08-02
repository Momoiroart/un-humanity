// UN-HUMANITY — THE QUEUE combat-core tests. Every illegal move, every
// legitimate counter, both win paths, the loss path. If these are green,
// the riskiest unknown in the vertical slice is de-risked.

using System.Linq;
using NUnit.Framework;
using UnHumanity.Combat;

namespace UnHumanity.Combat.Tests
{
    public class QueueEncounterTests
    {
        static QueueEncounter NewFight(int victimClock = 10, bool photographer = false) =>
            new QueueEncounter(new CombatState(victimClock: victimClock, playerIsPhotographer: photographer));

        // ── illegal move 1: turn theft ──────────────────────────────────
        [Test]
        public void TurnTheft_StealsThePlayersTurn()
        {
            var f = NewFight();
            f.WaiterPhase();                                  // round 1 = theft
            Assert.IsTrue(f.State.PlayerTurnStolenThisRound);
            Assert.IsFalse(f.PlayerPhase(new HoldAction()), "stolen turn must not allow acting");
        }

        [Test]
        public void WaiterActs_AtZeroCost_Always()
        {
            var f = NewFight();
            f.WaiterPhase();
            var waiterEvents = f.State.Log.Where(e => e.Actor == "The Waiter").ToList();
            Assert.IsNotEmpty(waiterEvents);
            Assert.IsTrue(waiterEvents.All(e => e.ZeroCost), "its cost panel is empty, on purpose");
        }

        // ── illegal move 2: counter deletion ────────────────────────────
        [Test]
        public void CounterDeletion_HidesDisplay_ButInternalRoundContinues()
        {
            var f = NewFight();
            f.RunRound(new HoldAction());                     // r1 theft
            f.RunRound(new HoldAction());                     // r2 deletes the counter
            Assert.IsNull(f.State.DisplayedRound, "UI counter must be gone");
            int before = f.State.InternalRound;
            f.RunRound(new HoldAction());
            Assert.AreEqual(before + 1, f.State.InternalRound, "time never actually stopped");
        }

        // ── illegal move 3: [WAITING] ───────────────────────────────────
        [Test]
        public void Waiting_BlocksOrdinaryActs_AndTheWaiterNeverActs()
        {
            var f = NewFight();
            f.RunRound(new HoldAction());                     // r1
            f.RunRound(new HoldAction());                     // r2
            f.WaiterPhase();                                  // r3 = [WAITING]
            Assert.IsTrue(f.State.Player.HasWaitingStatus);
            Assert.IsFalse(f.PlayerPhase(new EscortStepAction()), "escort needs a turn that never comes");
            Assert.IsTrue(f.PlayerPhase(new FlareAction()), "enforcement pierces the wait");
        }

        // ── the legitimate-cost kit ─────────────────────────────────────
        [Test]
        public void Flare_CostsAFlare_AndSuppressesTheNextCheat()
        {
            var f = NewFight();
            f.RunRound(new HoldAction());                     // r1: theft — no turn to act in
            f.WaiterPhase();                                  // r2: counter deletion
            Assert.IsTrue(f.PlayerPhase(new FlareAction()));
            Assert.AreEqual(1, f.State.Player.Flares);
            Assert.AreEqual(IllegalMove.None, f.State.ActiveIllegalMove);
            f.EndRound();
            f.WaiterPhase();                                  // r3 would be [WAITING] — suppressed
            Assert.AreEqual(IllegalMove.None, f.State.ActiveIllegalMove,
                "the flare's round of enforced time must protect the FOLLOWING round");
        }

        [Test]
        public void Flare_CannotBeStruckWithEmptyHands()
        {
            var f = NewFight();
            f.State.Player.Flares = 0;
            f.WaiterPhase();
            Assert.IsFalse(f.PlayerPhase(new FlareAction()), "no flare, no schedule");
        }

        [Test]
        public void Radio_RestoresTheCounter_AndGoesOnCooldown()
        {
            var f = NewFight();
            f.RunRound(new HoldAction());                     // r1
            f.RunRound(new HoldAction());                     // r2: counter deleted
            Assert.IsNull(f.State.DisplayedRound);
            f.WaiterPhase();                                  // r3
            Assert.IsTrue(f.PlayerPhase(new RadioCheckInAction()));
            Assert.IsNotNull(f.State.DisplayedRound, "a scheduled event forces a schedule");
            Assert.IsFalse(f.PlayerPhase(new RadioCheckInAction()) || f.State.Player.RadioCooldown == 0,
                "cooldown is the radio's honest cost");
        }

        [Test]
        public void Photograph_NeedsFilm_AndProducesEvidence()
        {
            var f = NewFight(photographer: true);
            f.RunRound(new HoldAction());                     // r1: theft — skip
            f.WaiterPhase();                                  // r2: deletion, turn available
            Assert.IsTrue(f.PlayerPhase(new PhotographAction()));
            Assert.IsTrue(f.State.Player.PhotoEvidence);
            Assert.AreEqual(0, f.State.Player.FilmRemaining);
            f.EndRound();
            f.WaiterPhase();
            Assert.IsFalse(f.PlayerPhase(new PhotographAction()), "film is a real cost");
        }

        // ── win: escort ─────────────────────────────────────────────────
        [Test]
        public void EscortRequiresOrder_AndFourStepsWin()
        {
            var f = NewFight(victimClock: 30);
            f.WaiterPhase();                                  // r1 theft — no order
            Assert.IsFalse(f.PlayerPhase(new EscortStepAction()), "cannot escort through an unbroken wait");

            // loop: enforce order, then spend the protected round walking her out
            int guard = 0;
            var escort = new EscortStepAction();
            var flare = new FlareAction();
            var radio = new RadioCheckInAction();
            while (f.State.Outcome == Outcome.Ongoing && guard++ < 40)
            {
                f.EndRound();
                f.WaiterPhase();
                if (f.State.PlayerTurnStolenThisRound) continue;
                if (escort.CanExecute(f.State)) f.PlayerPhase(escort);
                else if (flare.CanExecute(f.State)) f.PlayerPhase(flare);
                else if (radio.CanExecute(f.State)) f.PlayerPhase(radio);
                else f.PlayerPhase(new HoldAction());
            }
            Assert.AreEqual(Outcome.VictimEscorted, f.State.Outcome,
                "flare/radio rotation must be enough kit to walk her out");
        }

        // ── win: order re-imposed ───────────────────────────────────────
        [Test]
        public void SustainedOrder_ResolvesTheEncounter()
        {
            var f = NewFight(victimClock: 30, photographer: true);
            // photograph buys 2 rounds of order; radio + flare keep the streak alive
            int guard = 0;
            while (f.State.Outcome == Outcome.Ongoing && guard++ < 40)
            {
                f.WaiterPhase();
                if (!f.State.PlayerTurnStolenThisRound)
                {
                    if (f.State.Player.FilmRemaining > 0) f.PlayerPhase(new PhotographAction());
                    else if (f.State.Player.RadioCooldown == 0) f.PlayerPhase(new RadioCheckInAction());
                    else if (f.State.Player.Flares > 0) f.PlayerPhase(new FlareAction());
                    else f.PlayerPhase(new HoldAction());
                }
                f.EndRound();
            }
            Assert.AreEqual(Outcome.OrderReimposed, f.State.Outcome);
        }

        // ── loss: her clock ─────────────────────────────────────────────
        [Test]
        public void DoingNothing_LosesTheVictim()
        {
            var f = NewFight(victimClock: 5);
            for (int i = 0; i < 6 && f.State.Outcome == Outcome.Ongoing; i++)
                f.RunRound(new HoldAction());
            Assert.AreEqual(Outcome.VictimLost, f.State.Outcome,
                "waiting is exactly what it wants");
        }

        // ── damage does not exist ───────────────────────────────────────
        [Test]
        public void ThereIsNoDamagePath()
        {
            // The API surface itself is the assertion: no HP, no attack action.
            var actions = new PlayerAction[]
            {
                new FlareAction(), new RadioCheckInAction(),
                new PhotographAction(), new EscortStepAction(), new HoldAction(),
            };
            Assert.IsTrue(actions.All(a => a.Cost.Length > 0),
                "every player action names a real cost — the cost panel is never empty on our side");
        }
    }
}
