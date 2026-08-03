// UN-HUMANITY — SLICE 0, "The First Sighting." A short on-rails prequel
// that runs BEFORE UH-001's dispatch, in the same scene, reusing the
// street, Sight, and the dialogue system. Beats: ordinary morning →
// friend greeting → the crack (involuntary Sight) → the friend notices
// YOU → the friend's care → a week later → hand into the existing
// dispatch. Friend = human anchor (GDD v0.9): no Organization thread.
//
// The whole prologue freezes the player (Active) — it is watched, not
// walked — and suppresses the case HUD, QueueTrigger, and PlayerController's
// own Sight tick so the scripted crack cannot be fought or interrupted.

using System.Collections;
using UnityEngine;
using UnHumanity.Case;

public class PrologueSequence : MonoBehaviour
{
    [Header("wired by PrologueSetup")]
    public StoryUI story;
    public DialogueUI dialogue;
    public SightState sightState;
    public GameObject friend;      // the NPC, only present during the prologue

    public static bool Active { get; private set; }

    // domain-reload-off safety: never let a stale Active freeze the next Play
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Active = false;

    // ── card text (Slice 0 writer pass) ──
    [TextArea] public string openingTitle = "MONDAY — 07:44";
    [TextArea] public string openingBody =
        "Monday. The 07:44 sun, the same cracked pavement, the same walk you've made a " +
        "thousand times. Your bag's too heavy. There's a bio test third period you didn't " +
        "study for. Your friend fell into step beside you a block ago and hasn't stopped " +
        "talking since. Nothing is wrong. Nothing has ever been wrong. It's just a morning, " +
        "and you're just late.";
    public string openingPrompt = "F — KEEP WALKING";

    [TextArea] public string crackTitle = "07:4—";
    [TextArea] public string crackBody =
        "Mid-sentence, the color drains out of everything. Not dark — drained. Your friend's " +
        "mouth is still moving with no sound reaching you. The morning peels back like wet " +
        "paper, and underneath, the street is wine-black and wrong, and down at the bus stop " +
        "something is glowing that should not glow, and it is turned toward you, and you can't—";
    public string crackPrompt = "F — BREATHE";

    [TextArea] public string contactTitle = "MONDAY — 07:52";
    [TextArea] public string contactBody =
        "Your friend doesn't peel off at their class. They walk you to your door, wait for " +
        "the bell, text you twice at lunch, then walk you the whole way home. They don't " +
        "understand. They just won't leave you alone with it.\n\n" +
        "That's the thing about ordinary kindness aimed at someone who just Saw: it makes a " +
        "shape. Somewhere, something that watches for that shape starts watching you.";
    public string contactPrompt = "F — GO HOME";

    [TextArea] public string handoffTitle = "ONE WEEK LATER";
    [TextArea] public string handoffBody =
        "05:47. A week to the day. Your phone wakes with no name attached — just an address " +
        "and a time already running out. You were never going to keep this to yourself. " +
        "Someone always finds the ones who can See.\n\nThey found you.";
    public string handoffPrompt = "F — BEGIN";

    bool waiting;

    void Awake()
    {
        // claim the opening beat before StoryUI.Start auto-dispatches
        if (story != null) story.autoDispatch = false;
    }

    void Start()
    {
        if (Application.isPlaying) StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        Active = true;   // held for the WHOLE prologue — no inter-beat freeze gap
        if (sightState != null) sightState.SetSight(0f);

        yield return Card(openingTitle, openingBody, openingPrompt);
        yield return Talk(FriendDialogue.Greeting());
        // the crack takes you first — the world drains before any words for it
        yield return Lerp(0f, 1f, 1.6f);
        SfxBoss.Play("dread_sting");
        yield return new WaitForSeconds(1.6f);
        yield return Card(crackTitle, crackBody, crackPrompt);   // narrates, over the wine-black street
        yield return Lerp(1f, 0f, 1.3f);                          // morning returns
        yield return new WaitForSeconds(0.3f);
        yield return Talk(FriendDialogue.Noticed());
        yield return Card(contactTitle, contactBody, contactPrompt);
        yield return Card(handoffTitle, handoffBody, handoffPrompt);

        // Slice 0 is over — clear the prequel and let UH-001 begin, seamless
        if (friend != null) friend.SetActive(false);
        Active = false;
        if (story != null) story.ShowDispatch();
    }

    IEnumerator Card(string t, string b, string p)
    {
        waiting = true;
        story.ShowCard(t, b, p, () => waiting = false);
        while (waiting) yield return null;
    }

    IEnumerator Talk(Conversation convo)
    {
        dialogue.Begin(convo, null);   // null node → no evidence collect on finish
        yield return null;             // let DialogueActive latch on
        while (DialogueUI.DialogueActive) yield return null;
    }

    // the first Sight, taken from you. The prologue owns the blend outright
    // (Active freezes PlayerController before it can tick Sight), so nothing
    // fights these writes.
    IEnumerator Lerp(float from, float to, float dur)
    {
        if (from < 0.5f && to >= 0.5f) SfxBoss.Play("sight_enter");
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            sightState.SetSight(Mathf.Lerp(from, to, t / dur));
            yield return null;
        }
        sightState.SetSight(to);
    }
}
