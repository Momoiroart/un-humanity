// UN-HUMANITY — dialogue domain. Pure C#, engine-free, EditMode-testable.
// A conversation is a flat line list with at most ONE branch point: reach
// the choice index, pick A or B, and the chosen branch's lines splice in
// before the shared closing lines. Serves UH-001's witnesses and, later,
// Slice 0's friend — the same runner, different data.

using System.Collections.Generic;

namespace UnHumanity.Case
{
    public readonly struct DialogueLine
    {
        public readonly string Speaker;
        public readonly string Text;
        public DialogueLine(string speaker, string text) { Speaker = speaker; Text = text; }
    }

    public sealed class Conversation
    {
        public readonly List<DialogueLine> Lead = new();      // before the choice
        public readonly string ChoicePrompt;                  // null = no choice
        public readonly string OptionA, OptionB;
        public readonly List<DialogueLine> BranchA = new();
        public readonly List<DialogueLine> BranchB = new();
        public readonly List<DialogueLine> Close = new();     // after the branch

        public Conversation(string choicePrompt = null, string optionA = null, string optionB = null)
        {
            ChoicePrompt = choicePrompt;
            OptionA = optionA;
            OptionB = optionB;
        }

        public bool HasChoice => !string.IsNullOrEmpty(ChoicePrompt);
    }

    /// Walks a Conversation. State: which segment we're in and the index
    /// within it. AtChoice is true exactly when the lead is exhausted and a
    /// branch has not been chosen yet.
    public sealed class DialogueRunner
    {
        enum Seg { Lead, Branch, Close, Done }

        readonly Conversation c;
        Seg seg;
        int i;
        List<DialogueLine> branch;   // the chosen branch, once picked

        public DialogueRunner(Conversation conversation)
        {
            c = conversation;
            seg = Seg.Lead;
            i = 0;
            if (c.Lead.Count == 0) EnterAfterLead();
        }

        public bool IsDone => seg == Seg.Done;

        /// True when the runner is waiting on a choice (lead done, no branch
        /// picked). The UI shows the two options and calls Choose().
        public bool AtChoice => seg == Seg.Lead && i >= c.Lead.Count && c.HasChoice && branch == null;

        /// The line to display now. Undefined at a choice (AtChoice) or when
        /// done — callers check those first.
        public DialogueLine Current
        {
            get
            {
                switch (seg)
                {
                    case Seg.Lead: return c.Lead[i];
                    case Seg.Branch: return branch[i];
                    case Seg.Close: return c.Close[i];
                    default: return default;
                }
            }
        }

        /// Advance past the current line. No-op at a choice (the UI must call
        /// Choose first) or when done.
        public void Advance()
        {
            if (AtChoice || IsDone) return;
            i++;
            switch (seg)
            {
                case Seg.Lead:
                    if (i >= c.Lead.Count) EnterAfterLead();
                    break;
                case Seg.Branch:
                    if (i >= branch.Count) EnterClose();
                    break;
                case Seg.Close:
                    if (i >= c.Close.Count) seg = Seg.Done;
                    break;
            }
        }

        /// Pick a branch at the choice point. optionA = true chooses A.
        public void Choose(bool optionA)
        {
            if (!AtChoice) return;
            branch = optionA ? c.BranchA : c.BranchB;
            seg = Seg.Branch;
            i = 0;
            if (branch.Count == 0) EnterClose();
        }

        void EnterAfterLead()
        {
            // no choice on this conversation → straight to close
            if (!c.HasChoice) EnterClose();
            // else: sit at the choice; AtChoice becomes true
        }

        void EnterClose()
        {
            seg = Seg.Close;
            i = 0;
            if (c.Close.Count == 0) seg = Seg.Done;
        }
    }

    /// UH-001's two witnesses, as data. The contradiction is the content:
    /// the old man is certain the man is TALL and OLD; the commuter is
    /// certain he is SHORT and YOUNG. Both true to them. Nothing is there.
    public static class WitnessDialogue
    {
        public static Conversation OldMan()
        {
            var c = new Conversation();
            c.Lead.Add(new DialogueLine("YOU", "Um — excuse me. Sorry. The man at the stop back there. Did you see him?"));
            c.Lead.Add(new DialogueLine("OLD MAN", "See him? I've walked this street every morning of my life. Tall fellow. Taller than you."));
            c.Lead.Add(new DialogueLine("YOU", "Tall. Okay. And — how old would you say?"));
            c.Lead.Add(new DialogueLine("OLD MAN", "Old. My age, easy. Grey coat, grey hat, grey everything. Been there since dawn."));
            c.Lead.Add(new DialogueLine("YOU", "Since dawn. Right. Thank you. That — that helps."));
            c.Lead.Add(new DialogueLine("OLD MAN", "Tell him the 9 skips this stop half the time. He'll wait all day, poor soul."));
            return c;
        }

        public static Conversation Commuter()
        {
            var c = new Conversation("What do you ask?", "About the man waiting there", "About the stop itself");
            c.Lead.Add(new DialogueLine("YOU", "Hi — sorry. One second. I just have a question about the stop."));
            c.Lead.Add(new DialogueLine("COMMUTER", "You're not transit. You're, what, sixteen?"));
            c.Lead.Add(new DialogueLine("COMMUTER", "Fine. One question. The 9's late again anyway."));
            c.BranchA.Add(new DialogueLine("COMMUTER", "Short guy. Young — my age, maybe less. Got here this morning, same as me."));
            c.BranchA.Add(new DialogueLine("COMMUTER", "I stood right next to him. I notice people. Short. Young. I'm sure."));
            c.BranchB.Add(new DialogueLine("COMMUTER", "Weird question. Honestly? I'd swear it wasn't here yesterday. Which is stupid."));
            c.BranchB.Add(new DialogueLine("COMMUTER", "And the drivers skip it. They look right at us and keep going. Every morning."));
            c.Close.Add(new DialogueLine("COMMUTER", "That's my alarm. Ten more minutes, then I'm ordering a car."));
            c.Close.Add(new DialogueLine("COMMUTER", "Nobody waits here longer than that. Nobody should."));
            return c;
        }
    }

    /// SLICE 0 — the friend (human anchor). Two conversations bracket the
    /// first involuntary Sight: a mundane greeting, then their concern once
    /// they see the protagonist go grey. The friend never sees the anomaly.
    /// (Text finalized from the Slice 0 writer pass.)
    public static class FriendDialogue
    {
        public static Conversation Greeting()
        {
            var c = new Conversation();
            c.Lead.Add(new DialogueLine("FRIEND", "There you are. You didn't answer the group chat. Again."));
            c.Lead.Add(new DialogueLine("YOU", "Phone died. What'd I miss?"));
            c.Lead.Add(new DialogueLine("FRIEND", "Nothing. Everything. We have that thing third period, you did the reading, right?"));
            c.Lead.Add(new DialogueLine("YOU", "…Define 'did.'"));
            c.Lead.Add(new DialogueLine("FRIEND", "Unbelievable. Okay, I'll walk you through it. We've got, like, ten minutes."));
            c.Lead.Add(new DialogueLine("FRIEND", "Keep up. You always drift off right about — hey. You listening?"));
            return c;
        }

        public static Conversation Noticed()
        {
            var c = new Conversation();
            c.Lead.Add(new DialogueLine("FRIEND", "Whoa — hey. Hey. You just went grey. Like, actually grey."));
            c.Lead.Add(new DialogueLine("YOU", "I'm — I'm fine. Did you not just — "));
            c.Lead.Add(new DialogueLine("FRIEND", "Not just what? You stopped dead. You were staring at nothing."));
            c.Lead.Add(new DialogueLine("YOU", "The stop. Down the road. You didn't see — no. Forget it."));
            c.Lead.Add(new DialogueLine("FRIEND", "You're shaking. I'm not forgetting it. Sit down a second."));
            c.Lead.Add(new DialogueLine("FRIEND", "I'm not leaving you like this. School can wait. You come first."));
            return c;
        }
    }
}
