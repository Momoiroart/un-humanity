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
        // the walk to school — mundane, but every line is a soft watch on
        // reread ("stick close to me today," "humor me")
        public static Conversation Greeting()
        {
            var c = new Conversation();
            c.Lead.Add(new DialogueLine("FRIEND", "There you are. You look like death. Did you actually sleep, or...?"));
            c.Lead.Add(new DialogueLine("YOU", "Define sleep."));
            c.Lead.Add(new DialogueLine("FRIEND", "That bad. Weird dreams again? You always get them right before something's coming."));
            c.Lead.Add(new DialogueLine("YOU", "There was a light. It's gone now. Bio's third period, right? I'm so dead."));
            c.Lead.Add(new DialogueLine("FRIEND", "You're not dead, I'll cover you. Just — stick close to me today, okay? Humor me."));
            c.Lead.Add(new DialogueLine("YOU", "You're being weird."));
            c.Lead.Add(new DialogueLine("FRIEND", "I'm being nice. Come on — left, not right. Want to check something before the bell."));
            return c;
        }

        // the crack, being handled — the friend is calm, procedural, a pro;
        // you provide the Sight, they work the craft
        public static Conversation Handled()
        {
            var c = new Conversation();
            c.Lead.Add(new DialogueLine("FRIEND", "Okay. Hey — look at me. Not at it, at me. You're alright. You're alright."));
            c.Lead.Add(new DialogueLine("YOU", "You can see it too. You can actually SEE it—"));
            c.Lead.Add(new DialogueLine("FRIEND", "Yeah. I can. Deep breath. Stay behind me and do exactly what I say."));
            c.Lead.Add(new DialogueLine("FRIEND", "The kid on the stairs — you still see him? Point for me. Don't touch the doorframe."));
            c.Lead.Add(new DialogueLine("YOU", "Third step. He keeps climbing, he won't stop, he can't—"));
            c.Lead.Add(new DialogueLine("FRIEND", "Good. That's all I needed from you. Eyes on me now. This part's mine."));
            c.Lead.Add(new DialogueLine("YOU", "How many times have you—"));
            c.Lead.Add(new DialogueLine("FRIEND", "Later. Hold still. And whatever it says to you next — don't answer it."));
            return c;
        }

        // the reveal — the friend is an agent, a Painter, placed near you;
        // UH-001 will be your test
        public static Conversation After()
        {
            var c = new Conversation();
            c.Lead.Add(new DialogueLine("FRIEND", "Sit. Breathe. You did good back there — better than I did, my first time."));
            c.Lead.Add(new DialogueLine("YOU", "Your first time. So this is— this is a thing you do. Regularly."));
            c.Lead.Add(new DialogueLine("FRIEND", "Since I was nine. You're born able to See, or you're not. There's almost none of us."));
            c.Lead.Add(new DialogueLine("FRIEND", "Two of us, same class, same street? That's not supposed to happen. That's the part I hid."));
            c.Lead.Add(new DialogueLine("YOU", "Then why did you even come near me—"));
            c.Lead.Add(new DialogueLine("FRIEND", "Because I hoped I was wrong about you. You've flinched at empty corners for weeks. I knew."));
            c.Lead.Add(new DialogueLine("YOU", "The walking-me-home. The texts. All of it was—"));
            c.Lead.Add(new DialogueLine("FRIEND", "Somebody has to catch you before it does. That's what I am now. That's the job."));
            c.Lead.Add(new DialogueLine("FRIEND", "There are quiet people I answer to. They already know your name."));
            c.Lead.Add(new DialogueLine("FRIEND", "They want one thing first — to watch you work a single case, alone. A test."));
            c.Lead.Add(new DialogueLine("FRIEND", "If you're what I think you are, you'll walk right through it. I did."));
            return c;
        }
    }
}
