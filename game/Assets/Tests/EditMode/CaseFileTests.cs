// UN-HUMANITY — case-file domain tests: sight gating, the 4-clue
// classification gate, and the one-verdict-per-case rule.

using NUnit.Framework;
using UnHumanity.Case;

namespace UnHumanity.Case.Tests
{
    public class CaseFileTests
    {
        [Test]
        public void SceneryNodes_RefuseInNormalcy_RespondUnderSight()
        {
            var f = CaseUH001.NewCaseFile();
            Assert.IsFalse(f.Collect(ClueId.Bench, underSight: false), "the street holds its secrets");
            Assert.AreEqual(0, f.FoundCount);
            Assert.IsTrue(f.Collect(ClueId.Bench, underSight: true));
            Assert.AreEqual(1, f.FoundCount);
        }

        [Test]
        public void ExactlyOneClueRespondsInNormalcy_TheWitnesses()
        {
            var f = CaseUH001.NewCaseFile();
            int normalcyClues = 0;
            foreach (var c in f.Catalog.Values)
                if (c.RespondsInNormalcy) normalcyClues++;
            Assert.AreEqual(1, normalcyClues, "only human testimony responds before Sight");
            Assert.IsTrue(f.Collect(ClueId.Witnesses, underSight: false));
        }

        [Test]
        public void CollectingTwice_AddsNothing_ButSightUpgradesANormalcyRead()
        {
            var f = CaseUH001.NewCaseFile();
            Assert.IsTrue(f.Collect(ClueId.Witnesses, underSight: false));
            Assert.IsFalse(f.Collect(ClueId.Witnesses, underSight: false), "dedup");
            Assert.IsFalse(f.Get(ClueId.Witnesses).UnderSight);
            Assert.IsTrue(f.Collect(ClueId.Witnesses, underSight: true), "sight re-read upgrades");
            Assert.IsTrue(f.Get(ClueId.Witnesses).UnderSight);
            Assert.AreEqual(1, f.FoundCount, "an upgrade is not a new clue");
        }

        [Test]
        public void ClassificationArms_AtFourClues_NotBefore()
        {
            var f = CaseUH001.NewCaseFile();
            f.Collect(ClueId.Witnesses, false);
            f.Collect(ClueId.Bench, true);
            f.Collect(ClueId.Sediment, true);
            Assert.IsFalse(f.CanClassify, "three is not enough to call it");
            Assert.IsFalse(f.Classify(Verdict.Remnant));
            f.Collect(ClueId.Archive, true);
            Assert.IsTrue(f.CanClassify);
        }

        [Test]
        public void OneVerdictPerCase_TheForkIsPermanent()
        {
            var f = CaseUH001.NewCaseFile();
            f.Collect(ClueId.Witnesses, false);
            f.Collect(ClueId.Bench, true);
            f.Collect(ClueId.Sediment, true);
            f.Collect(ClueId.Archive, true);
            Assert.IsTrue(f.Classify(Verdict.UnHumanity));
            Assert.AreEqual(Verdict.UnHumanity, f.Verdict);
            Assert.IsFalse(f.Classify(Verdict.Remnant), "no take-backs");
            Assert.AreEqual(Verdict.UnHumanity, f.Verdict);
        }

        [Test]
        public void EveryClue_HasASightReading()
        {
            var f = CaseUH001.NewCaseFile();
            foreach (var c in f.Catalog.Values)
                Assert.IsFalse(string.IsNullOrEmpty(c.SightText), c.Title + " must read under Sight");
        }
    }
}
