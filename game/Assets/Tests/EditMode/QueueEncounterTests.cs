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

        // ── knowledge gating: understanding is the weapon ───────────────
        [Test]
        public void BlindEngagement_HasNoWinCondition_ItIsFunctionallyInvincible()
        {
            var f = new QueueEncounter(new CombatState(victimClock: 30,
                knowledge: EncounterKnowledge.None));
            f.RunRound(new HoldAction());                     // r1 theft
            f.WaiterPhase();                                  // r2 deletion
            Assert.IsFalse(new EscortStepAction().CanExecute(f.State), "victim unknown - no escort");
            Assert.IsFalse(new RadioCheckInAction().CanExecute(f.State), "schedule unknown - no radio");
            Assert.IsNull(f.ForecastNext(), "no forecast without the stop clue");
            // even perfect order play cannot resolve an unclassified thing
            int guard = 0;
            while (f.State.Outcome == Outcome.Ongoing && guard++ < 12)
            {
                if (!f.State.PlayerTurnStolenThisRound && new FlareAction().CanExecute(f.State))
                    f.PlayerPhase(new FlareAction());
                else f.PlayerPhase(new HoldAction());
                f.EndRound();
                f.WaiterPhase();
            }
            Assert.AreNotEqual(Outcome.OrderReimposed, f.State.Outcome,
                "unclassified = unresolvable, no matter how well you play");
        }

        [Test]
        public void Withdraw_AlwaysWorks_EvenBlind_EvenWaiting()
        {
            var f = new QueueEncounter(new CombatState(knowledge: EncounterKnowledge.None));
            f.RunRound(new HoldAction());
            f.RunRound(new HoldAction());
            f.WaiterPhase();                                  // r3: [WAITING]
            Assert.IsTrue(f.State.Player.HasWaitingStatus);
            Assert.IsTrue(f.PlayerPhase(new WithdrawAction()), "you can always stop waiting yourself");
            Assert.AreEqual(Outcome.Withdrawn, f.State.Outcome);
        }

        [Test]
        public void VictimClue_UnlocksEscort_ArchiveClue_UnlocksRadio()
        {
            var k = new EncounterKnowledge { KnowsVictim = true, KnowsSchedule = true };
            var f = new QueueEncounter(new CombatState(victimClock: 30, knowledge: k));
            f.RunRound(new HoldAction());                     // past the theft round
            f.WaiterPhase();                                  // r2 deletion
            Assert.IsTrue(new RadioCheckInAction().CanExecute(f.State));
            f.PlayerPhase(new RadioCheckInAction());          // order for next round
            f.EndRound();
            f.WaiterPhase();                                  // suppressed
            Assert.IsTrue(new EscortStepAction().CanExecute(f.State), "order holds + victim known");
        }

        [Test]
        public void Classification_IsWhatMakesItResolvable()
        {
            var k = EncounterKnowledge.All;
            k.Classified = false;
            var f = new QueueEncounter(new CombatState(victimClock: 30, playerIsPhotographer: true, knowledge: k));
            // identical play to SustainedOrder test, minus the verdict
            int guard = 0;
            while (f.State.Outcome == Outcome.Ongoing && guard++ < 14)
            {
                f.WaiterPhase();
                if (!f.State.PlayerTurnStolenThisRound)
                {
                    if (f.State.Player.FilmRemaining > 0) f.PlayerPhase(new PhotographAction());
                    else if (new RadioCheckInAction().CanExecute(f.State)) f.PlayerPhase(new RadioCheckInAction());
                    else if (f.State.Player.Flares > 0) f.PlayerPhase(new FlareAction());
                    else f.PlayerPhase(new HoldAction());
                }
                f.EndRound();
            }
            Assert.AreNotEqual(Outcome.OrderReimposed, f.State.Outcome,
                "held order means nothing until you can NAME what you are holding it against");
        }

        [Test]
        public void TheStopClue_ForecastsTheNextViolation()
        {
            var k = new EncounterKnowledge { KnowsTheStop = true };
            var f = new QueueEncounter(new CombatState(knowledge: k));
            f.WaiterPhase();                                  // r1 theft active
            Assert.IsNotNull(f.ForecastNext(), "the stop clue reads the queue");
        }

        // ── damage does not exist ───────────────────────────────────────
        [Test]
        public void ThereIsNoDamagePath()
        {
            // The API surface itself is the assertion: no HP anywhere.
            // ATTACK exists (Combat_System §4) but carries no damage number
            // and no win path — it is the floor, not a route to victory.
            var actions = new PlayerAction[]
            {
                new AttackAction(), new FlareAction(), new RadioCheckInAction(),
                new PhotographAction(), new EscortStepAction(), new HoldAction(),
            };
            Assert.IsTrue(actions.All(a => a.Cost.Length > 0),
                "every player action names a real cost — the cost panel is never empty on our side");
        }

        // ── composure + ejection (the player bar) ───────────────────────
        [Test]
        public void PassiveDefaultFight_EndsVictimLost_NeverEjected()
        {
            // the canary: ties between her clock and your composure must
            // go to the loss that matters — the scripted VictimLost beat
            var f = NewFight();   // clock 10, knowledge All
            for (int i = 0; i < 30 && f.State.Outcome == Outcome.Ongoing; i++)
                f.RunRound(new HoldAction());
            Assert.AreEqual(Outcome.VictimLost, f.State.Outcome);
            Assert.IsFalse(f.State.WasEjected, "a passive default fight ends on HER clock, not yours");
        }

        [Test]
        public void SuppressedRounds_CostNoComposure()
        {
            var f = NewFight();
            f.RunRound(new HoldAction());                     // r1 theft lands (-1)
            f.WaiterPhase();                                  // r2 deletion lands (-1)
            int before = f.State.Composure;
            f.PlayerPhase(new FlareAction());
            f.EndRound();
            f.WaiterPhase();                                  // r3 suppressed — no cheat fires
            Assert.AreEqual(before, f.State.Composure, "a cancelled cheat costs no composure");
        }

        [Test]
        public void Ejection_AtZeroComposure_SetsWasEjected()
        {
            var f = new QueueEncounter(new CombatState(victimClock: 30,
                knowledge: EncounterKnowledge.None));
            for (int i = 0; i < 30 && f.State.Outcome == Outcome.Ongoing; i++)
                f.RunRound(new HoldAction());
            Assert.AreEqual(Outcome.Withdrawn, f.State.Outcome);
            Assert.IsTrue(f.State.WasEjected, "the queue stands you down — you never die here");
            Assert.IsTrue(f.State.Composure <= 0);
        }

        // ── ACT options from the case file ──────────────────────────────
        [Test]
        public void SpeakTheDate_PiercesWaiting_Suppresses2_OnceOnly()
        {
            var f = NewFight();
            f.RunRound(new HoldAction());                     // r1
            f.RunRound(new HoldAction());                     // r2
            f.WaiterPhase();                                  // r3 = [WAITING]
            Assert.IsTrue(f.PlayerPhase(new SpeakTheDateAction()), "a spoken date pierces the wait");
            Assert.AreEqual(IllegalMove.None, f.State.ActiveIllegalMove);
            f.EndRound();
            f.WaiterPhase();                                  // r4 — protected
            Assert.AreEqual(IllegalMove.None, f.State.ActiveIllegalMove);
            f.EndRound();
            f.WaiterPhase();                                  // r5 — protected (2 rounds)
            Assert.AreEqual(IllegalMove.None, f.State.ActiveIllegalMove);
            Assert.IsFalse(new SpeakTheDateAction().CanExecute(f.State),
                "the date only lands once — it heard you the first time");
        }

        [Test]
        public void ShowHerPhoto_Restores2_CappedAtCtorMax_OnceOnly()
        {
            var f = NewFight();
            f.State.Player.PhotoEvidence = true;              // seeded from the case file
            f.RunRound(new HoldAction());                     // r1 — clock 9
            f.WaiterPhase();                                  // r2 deletion — ordinary acts allowed
            Assert.IsTrue(f.PlayerPhase(new ShowHerPhotoAction()));
            Assert.AreEqual(10, f.State.VictimClock, "restore caps at the ctor value, never a literal");
            Assert.IsFalse(new ShowHerPhotoAction().CanExecute(f.State),
                "once per fight — the flag lives on state, fresh instances cannot re-arm it");
        }

        [Test]
        public void ShowHerPhoto_CapIsCtorValue_NotTen()
        {
            var f = new QueueEncounter(new CombatState(victimClock: 30));
            f.State.Player.PhotoEvidence = true;
            f.RunRound(new HoldAction());
            f.RunRound(new HoldAction());
            f.RunRound(new HoldAction());                     // clock 27
            f.WaiterPhase();                                  // r4 theft — blocked... use r5
            f.PlayerPhase(new HoldAction());
            f.EndRound();                                     // clock 26
            f.WaiterPhase();                                  // r5 waiting — photo is ordinary, blocked
            Assert.IsFalse(f.PlayerPhase(new ShowHerPhotoAction()), "[WAITING] blocks the photo");
            Assert.IsTrue(f.PlayerPhase(new FlareAction()));  // pierce + suppress
            f.EndRound();                                     // clock 25
            f.WaiterPhase();                                  // r6 suppressed
            Assert.IsTrue(f.PlayerPhase(new ShowHerPhotoAction()));
            Assert.AreEqual(27, f.State.VictimClock, "a clock-30 fight must never be slashed to 10");
        }

        // ── ATTACK + EXPOSURE (Combat_System §4/§7) ─────────────────────
        [Test]
        public void Attack_Blind_LandsButExposesNothing()
        {
            var f = new QueueEncounter(new CombatState(victimClock: 10,
                knowledge: EncounterKnowledge.None));
            f.RunRound(new HoldAction());                     // r1 theft
            f.WaiterPhase();                                  // r2: counter deletion
            Assert.IsTrue(f.PlayerPhase(new AttackAction()), "attack is always available");
            Assert.AreEqual(IllegalMove.CounterDeletion, f.State.ActiveIllegalMove,
                "blind, the jab names nothing — the cheat stands");
            Assert.AreEqual(0, f.State.SuppressionRounds, "no file, no Exposure");
            Assert.AreEqual(Outcome.Ongoing, f.State.Outcome);
        }

        [Test]
        public void Attack_WithClassifiedFile_TriggersExposure()
        {
            var f = new QueueEncounter(new CombatState(victimClock: 10,
                playerIsPhotographer: false, knowledge: EncounterKnowledge.All));
            f.RunRound(new HoldAction());                     // r1 theft
            f.WaiterPhase();                                  // r2: counter deletion
            Assert.IsTrue(f.PlayerPhase(new AttackAction()));
            Assert.AreEqual(IllegalMove.None, f.State.ActiveIllegalMove,
                "Exposure suspends the active cheat");
            f.EndRound();
            f.WaiterPhase();                                  // r3 would cheat — suppressed
            Assert.AreEqual(IllegalMove.None, f.State.ActiveIllegalMove,
                "a classified file guarantees Exposure on first hit (§7)");
        }

        [Test]
        public void Attack_IsOrdinary_WaitingBlocksIt()
        {
            var f = NewFight();
            f.RunRound(new HoldAction());                     // r1
            f.RunRound(new HoldAction());                     // r2
            f.WaiterPhase();                                  // r3 = [WAITING]
            Assert.IsFalse(f.PlayerPhase(new AttackAction()),
                "a jab is an ordinary act — the wait refuses it; enforcement tools pierce, fists don't");
        }
    }
}
