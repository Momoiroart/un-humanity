// UN-HUMANITY — SLICE 0's cross-scene transport AND its cinematic staging.
// A persistent rig (this + player + camera + UI, all DontDestroyOnLoad)
// carries through the location scenes: bedroom → walk → school → the exam.
// Each location loads Single (the rig survives); its RoomMood drives the
// look; a black fade bridges every hop; the rig self-destructs when the
// exam street loads so the built game takes over.
//
// Presentation is CINEMATIC, not modal: a thin letterbox + a subtitle band
// at the bottom keep the 3D world visible the whole time. The characters
// perform in-world — turning to face each other, looking toward the crack,
// stepping in — instead of the story living in full-screen dossier cards.
// The tested DialogueRunner still walks the conversations.

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnHumanity.Case;

public class PrologueFlow : MonoBehaviour
{
    [Header("wired by PrologueCoreSetup")]
    public Transform player;
    public Camera cam;
    public Image fade;                    // fullscreen black (scene transitions)
    public Image letterTop, letterBot;    // the letterbox bars
    public Text timeChip;                 // top-left time stamp
    public Text capSpeaker, capBody, capPrompt;   // the subtitle band

    // speaker tints — friend keyed to their green hoodie, you a cool blue
    static readonly Color kFriend = new Color32(0x9A, 0xCF, 0x93, 0xFF);
    static readonly Color kYou    = new Color32(0xA6, 0xC4, 0xE6, 0xFF);
    static readonly Color kNarr   = new Color32(0xB8, 0xBC, 0xC4, 0xFF);
    static readonly Color kPrompt = new Color32(0x86, 0x8C, 0x96, 0xFF);

    public static bool InputLocked { get; private set; }
    static bool started;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { InputLocked = false; started = false; }

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnLoaded;
        if (!started)
        {
            started = true;
            SceneManager.LoadScene(PrologueScenePaths.Bedroom, LoadSceneMode.Single);
        }
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnLoaded;

    void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        var beats = FindFirstObjectByType<PrologueBeats>();
        if (beats == null) return;   // not a prologue location (e.g. the exam)
        var spawn = GameObject.Find("Spawn");
        if (player != null && spawn != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.position = spawn.transform.position;
            if (cc != null) cc.enabled = true;
        }
        ApplyCameraFraming();
        StartCoroutine(RunScene(beats));
    }

    // Interiors ship an authored SceneCam (fixed framed shot); the open
    // street/yard have none and keep the follow rig. Switching per scene
    // stops the diorama cam from burying itself in a small room.
    void ApplyCameraFraming()
    {
        if (cam == null) return;
        var follow = cam.GetComponent<CameraRigFollow>();
        var authored = FindInScene("SceneCam");
        if (authored != null)
        {
            if (follow != null) follow.enabled = false;
            cam.transform.SetPositionAndRotation(authored.transform.position, authored.transform.rotation);
            var sc = authored.GetComponent<SceneCam>();
            if (sc != null) cam.fieldOfView = sc.fov;
        }
        else if (follow != null)
        {
            follow.enabled = true;
            cam.fieldOfView = 40f;
        }
    }

    IEnumerator RunScene(PrologueBeats beats)
    {
        ClearCaption();
        yield return FadeTo(0f, 0.7f);   // fade IN from black
        switch (beats.sceneId)
        {
            case "bedroom": yield return Bedroom(); break;
            case "walk": yield return Walk(); break;
            case "school": yield return School(); break;
        }
        ClearCaption();                  // caption sits above the fade now
        yield return FadeTo(1f, 0.7f);   // fade OUT to black
        GoNext(beats.nextScene);
    }

    void GoNext(string next)
    {
        if (string.IsNullOrEmpty(next) || next == PrologueScenePaths.Street)
        {
            SceneManager.LoadScene(PrologueScenePaths.Street, LoadSceneMode.Single);
            Destroy(gameObject);
            return;
        }
        SceneManager.LoadScene(next, LoadSceneMode.Single);
    }

    // ─────────────────────────── the beats ───────────────────────────
    IEnumerator Bedroom()
    {
        InputLocked = true;
        SetTime("MONDAY  ·  06:30");
        yield return Beat(1.1f);                       // wake — hold on the room
        LookPlayer(1f);                                // glance toward the window
        yield return Narrate("Four minutes of alarm. The ceiling. That water-stain shaped like a country that doesn't exist.");
        yield return Narrate("You slept badly. A dream you can't hold — and a dull ache behind your eyes, like you stared at something too bright.");
        LookPlayer(1f);
        yield return Narrate("Bio test third period. It's Monday. It's only ever Monday.");
        yield return Prompt("You get up.", "the door");   // MOVE hint → walk to the door
        yield return WalkTo("DoorExit", 1.1f);

        SetTime("MONDAY  ·  07:12");
        yield return Narrate("Toast in your teeth, the door on the latch. The cold takes the last of the sleep.");
        yield return Narrate("At the corner the Route 9 stop sits empty — and for half a second your eyes snag on it. Nothing there.");
        yield return Narrate("Then you're moving again. Late. Not thinking about it at all.");
    }

    IEnumerator Walk()
    {
        InputLocked = true;
        SetTime("MONDAY  ·  07:24");
        var friend = FindInScene("NPC_Friend");
        yield return Beat(0.7f);
        FaceToward(friend, player);                    // the friend turns to you
        if (friend != null) yield return Nudge(friend.transform, new Vector3(0.4f, 0, -1.6f), 0.7f);  // falls into step
        yield return Say(FriendDialogue.Greeting());
        yield return Prompt("You walk.", "the school gate");
        // the friend keeps pace a little, then you go ahead
        yield return WalkTo("GateReach", 1.4f, friend);
    }

    IEnumerator School()
    {
        InputLocked = true;
        SetTime("MONDAY  ·  07:40");
        var friend = FindInScene("NPC_Friend");
        yield return Beat(0.6f);
        yield return Narrate("Gates, noise. Four hundred people pretending it isn't this early.");
        FaceToward(friend, player);
        yield return Narrate("A hand lands on your shoulder before you've clocked whose. Of course. They always find you first.");

        // ── the crack ──
        SfxBoss.Play("sight_enter");
        var anomaly = FindInScene("SchoolAnomaly");
        LookPlayer(1f);                                // you turn toward the door
        yield return CrackLerp(0f, 1f, 1.8f, anomaly);
        SfxBoss.Play("dread_sting");
        yield return Beat(0.9f);
        SetTime("07:4—");
        yield return Narrate("The sound drains out of the world. The colour follows — not dark. Drained. The grey of an old photograph.");
        yield return Narrate("A fire door hangs open on a stairwell that isn't there, climbing up into wine-black.");
        yield return Narrate("On the third step a first-year climbs and never rises. Mouth open. No sound. It is turned toward you.");
        FaceToward(friend, anomaly != null ? anomaly.transform : null);
        yield return Narrate("Beside you, your friend has gone very still.");

        // the friend handles it — a small, certain act
        // the friend walks DOWN the hall toward the crack at the corridor end
        // to paint it — a real approach, framed in the same depth as the rift
        if (friend != null) yield return Nudge(friend.transform, new Vector3(0.3f, 0, 4.0f), 1.4f);
        yield return Say(FriendDialogue.Handled());
        SfxBoss.Play("win");
        yield return CrackLerp(1f, 0f, 1.5f, anomaly);
        yield return Beat(0.4f);

        SetTime("MONDAY  ·  07:46");
        yield return Narrate("It's over in under a minute. A word, a turn of the hand — like resetting a clock — and the stairwell folds shut.");
        yield return Narrate("The first-year steps onto the landing, blinking, late, completely fine.");
        FaceToward(friend, player);
        yield return Narrate("Your friend turns to you. Not shaken. Not surprised. Watching you like someone they've waited on a long time.");
        yield return Say(FriendDialogue.After());

        // one week later — the summons
        yield return FadeTo(1f, 0.9f);
        ClearCaption();
        yield return Beat(0.6f);
        SetTime("ONE WEEK LATER  ·  05:47");
        yield return Beat(0.4f);
        yield return Narrate("Seven days to the day. Your phone wakes with an address and a time already bleeding out.");
        yield return Narrate("One line, from the number your friend made you save: YOUR FIRST FILE. THEY'RE WATCHING HOW YOU WORK IT.");
        yield return Narrate("No partner in the field — that's the whole point of a test. Route 9, northbound. Nothing has waited at that stop since 1974.");
        // GoNext hands to the exam street (rig teardown)
    }

    // ──────────────────────── presentation ────────────────────────
    // A subtitle line. Narration = italic, no speaker; the world stays lit
    // behind the thin band. Waits for F.
    IEnumerator Line(string speaker, string text, Color tint, bool italic, string sfx)
    {
        InputLocked = true;
        capSpeaker.text = string.IsNullOrEmpty(speaker) ? "" : speaker;
        capSpeaker.color = tint;
        capBody.text = text;
        capBody.fontStyle = italic ? FontStyle.Italic : FontStyle.Normal;
        capBody.color = italic ? kNarr : new Color32(0xEC, 0xEE, 0xF1, 0xFF);
        capPrompt.text = "F  ▸";
        capPrompt.color = kPrompt;
        if (!string.IsNullOrEmpty(sfx)) SfxBoss.Play(sfx);
        yield return null;                     // swallow the press that got us here
        while (!Pressed()) yield return null;
        SfxBoss.Play("dialogue_advance");
    }

    IEnumerator Narrate(string text) => Line("", text, kNarr, true, "case_open");

    IEnumerator Say(Conversation convo)
    {
        var r = new DialogueRunner(convo);
        while (!r.IsDone)
        {
            var l = r.Current;
            bool friend = l.Speaker == "FRIEND";
            yield return Line(l.Speaker, l.Text, friend ? kFriend : kYou, false,
                              friend ? "bark_commuter" : "bark_player");
            r.Advance();
        }
    }

    // a held beat — no text, world visible, input frozen (unless a WalkTo)
    IEnumerator Beat(float seconds)
    {
        InputLocked = true;
        ClearCaption();
        yield return new WaitForSeconds(seconds);
    }

    // a movement prompt: shows a hint, then hands control to WalkTo
    IEnumerator Prompt(string line, string toward)
    {
        capSpeaker.text = "";
        capBody.text = line;
        capBody.fontStyle = FontStyle.Italic;
        capBody.color = kNarr;
        capPrompt.text = "WASD  ▸  " + toward;
        capPrompt.color = kYou;
        yield return null;
    }

    void ClearCaption()
    {
        if (capSpeaker != null) capSpeaker.text = "";
        if (capBody != null) capBody.text = "";
        if (capPrompt != null) capPrompt.text = "";
    }

    void SetTime(string t) { if (timeChip != null) timeChip.text = t; }

    // ──────────────────────── performance ────────────────────────
    // flip a billboard's Sprite child to face a direction (+1 right, -1 left)
    static void FaceSprite(Transform root, float dir)
    {
        if (root == null) return;
        var q = root.Find("Sprite");
        if (q == null) return;
        var s = q.localScale;
        s.x = Mathf.Abs(s.x) * Mathf.Sign(dir == 0 ? 1 : dir);
        q.localScale = s;
    }

    void LookPlayer(float dir) => FaceSprite(player, dir);

    // turn `who` to face `target` along X
    void FaceToward(GameObject who, Transform target)
    {
        if (who == null || target == null) return;
        FaceSprite(who.transform, target.position.x - who.transform.position.x);
    }

    IEnumerator Nudge(Transform t, Vector3 delta, float dur)
    {
        if (t == null) yield break;
        Vector3 a = t.position, b = a + delta;
        FaceSprite(t, delta.x);
        float k = 0f;
        while (k < dur)
        {
            k += Time.deltaTime;
            t.position = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, k / dur));
            yield return null;
        }
        t.position = b;
    }

    // walk to a named target; optional companion trails a step behind
    IEnumerator WalkTo(string targetName, float radius, GameObject companion = null)
    {
        var target = GameObject.Find(targetName);
        if (target == null || player == null) yield break;
        InputLocked = false;                 // hand control to the player
        Vector3 offset = companion != null ? companion.transform.position - player.position : Vector3.zero;
        while (Vector3.Distance(player.position, target.transform.position) > radius)
        {
            if (companion != null)           // the friend keeps pace, a beat behind
            {
                var want = player.position + offset;
                companion.transform.position = Vector3.Lerp(companion.transform.position, want, 2.5f * Time.deltaTime);
            }
            yield return null;
        }
        InputLocked = true;
    }

    // ──────────────────────── the crack ────────────────────────
    IEnumerator CrackLerp(float from, float to, float dur, GameObject anomaly)
    {
        if (anomaly != null && to > from) anomaly.SetActive(true);
        var key = FindInScene("RigKey");
        var keyLight = key != null ? key.GetComponent<Light>() : null;
        float keyNormal = 0.9f, keyDrained = 0.12f;   // kill the light so the glow rules
        var normalAmb = new Color(0.52f, 0.50f, 0.52f);
        var darkAmb = new Color(0.05f, 0.02f, 0.035f);
        var normalBg = new Color(0.30f, 0.28f, 0.30f);
        var darkBg = new Color(0.045f, 0.02f, 0.035f);
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
            RenderSettings.ambientLight = Color.Lerp(normalAmb, darkAmb, k);
            if (cam != null) cam.backgroundColor = Color.Lerp(normalBg, darkBg, k);
            if (keyLight != null) keyLight.intensity = Mathf.Lerp(keyNormal, keyDrained, k);
            yield return null;
        }
        if (anomaly != null && to < 0.5f) anomaly.SetActive(false);
    }

    IEnumerator FadeTo(float a, float dur)
    {
        if (fade == null) yield break;
        var c = fade.color; float from = c.a; float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, a, t / dur);
            fade.color = c;
            yield return null;
        }
        c.a = a; fade.color = c;
    }

    static bool Pressed()
    {
        var kb = Keyboard.current; var pad = Gamepad.current;
        return (kb != null && kb.fKey.wasPressedThisFrame)
            || (pad != null && pad.buttonSouth.wasPressedThisFrame);
    }

    // GameObject.Find skips inactive objects; the school anomaly is built
    // inactive, so search the active scene's whole hierarchy instead.
    static GameObject FindInScene(string name)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var hit = FindInChildren(root.transform, name);
            if (hit != null) return hit.gameObject;
        }
        return null;
    }

    static Transform FindInChildren(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var hit = FindInChildren(child, name);
            if (hit != null) return hit;
        }
        return null;
    }
}
