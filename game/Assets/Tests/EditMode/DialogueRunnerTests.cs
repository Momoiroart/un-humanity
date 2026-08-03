// UN-HUMANITY — DialogueRunner domain tests. The conversation walker with
// its single branch point; the piece Slice 0 will reuse unchanged.

using NUnit.Framework;
using UnHumanity.Case;

namespace UnHumanity.Combat.Tests
{
    public class DialogueRunnerTests
    {
        [Test]
        public void OldMan_LinearWalk_EndsDone()
        {
            var r = new DialogueRunner(WitnessDialogue.OldMan());
            int n = 0;
            while (!r.IsDone) { Assert.IsFalse(r.AtChoice, "no choice in the old man's line"); r.Advance(); n++; }
            Assert.AreEqual(6, n, "all six lines walked");
        }

        [Test]
        public void Commuter_StopsAtChoice_AfterLead()
        {
            var convo = WitnessDialogue.Commuter();
            var r = new DialogueRunner(convo);
            r.Advance(); r.Advance(); r.Advance();   // three lead lines
            Assert.IsTrue(r.AtChoice, "lead exhausted → choice");
            Assert.AreEqual("What do you ask?", convo.ChoicePrompt);
            Assert.IsTrue(convo.HasChoice);
        }

        [Test]
        public void Advance_AtChoice_IsNoOp()
        {
            var r = new DialogueRunner(WitnessDialogue.Commuter());
            r.Advance(); r.Advance(); r.Advance();
            Assert.IsTrue(r.AtChoice);
            r.Advance();   // must do nothing — the UI has to Choose
            Assert.IsTrue(r.AtChoice, "advance cannot skip a choice");
        }

        [Test]
        public void ChooseA_SplicesTheManBranch_ThenClose()
        {
            var r = new DialogueRunner(WitnessDialogue.Commuter());
            r.Advance(); r.Advance(); r.Advance();
            r.Choose(optionA: true);
            Assert.IsFalse(r.AtChoice);
            StringAssert.Contains("Short", r.Current.Text);   // the man branch
            r.Advance(); r.Advance();                          // two branch lines
            StringAssert.Contains("alarm", r.Current.Text);    // into the shared close
        }

        [Test]
        public void ChooseB_SplicesTheStopBranch()
        {
            var r = new DialogueRunner(WitnessDialogue.Commuter());
            r.Advance(); r.Advance(); r.Advance();
            r.Choose(optionA: false);
            StringAssert.Contains("wasn't here yesterday", r.Current.Text);
        }

        [Test]
        public void TheContradiction_IsReal()
        {
            // the whole point: A says tall+old, B says short+young
            var a = WitnessDialogue.OldMan();
            var b = WitnessDialogue.Commuter();
            bool aTall = a.Lead[1].Text.Contains("Tall");
            bool bShort = b.BranchA[0].Text.Contains("Short");
            Assert.IsTrue(aTall && bShort, "the witnesses must disagree, both certain");
        }

        [Test]
        public void EmptyLeadWithNoChoice_IsImmediatelyClose()
        {
            var c = new Conversation();
            c.Close.Add(new DialogueLine("X", "only line"));
            var r = new DialogueRunner(c);
            Assert.IsFalse(r.IsDone);
            Assert.IsFalse(r.AtChoice);
            Assert.AreEqual("only line", r.Current.Text);
            r.Advance();
            Assert.IsTrue(r.IsDone);
        }
    }
}
