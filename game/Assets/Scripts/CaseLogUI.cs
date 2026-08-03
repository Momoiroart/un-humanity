// UN-HUMANITY — case-log UI (dossier identity). TAB opens the file; rows
// fill in as evidence lands; the classification fork arms at four pieces.
// Also owns the always-on HUD: sight meter cells, interact prompt, toast.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnHumanity.Case;

public class CaseLogUI : MonoBehaviour
{
    [Header("wired by CaseSetup")]
    public CaseController controller;
    public SightState sightState;
    public PlayerInteractor interactor;

    public GameObject panelRoot;
    public Text counter;
    public Text[] rowTitles;      // 6, catalog order
    public Text[] rowTags;        // "—" / "PLAIN" / "SIGHT"
    public GameObject classifySection;
    public GameObject classifyButtons;
    public Text classifyNote;
    public Text verdictStamp;
    public Text[] analysisLines;  // the running deduction, newest last
    [TextArea] public string doctrine = "";   // HOW to judge — shown when the file is ready
    public Image[] sightCells;    // 10 HUD cells
    public GameObject promptRoot; // keycap chip — readable on any backdrop
    public Text promptText;
    public GameObject toastRoot;  // top-center chip, hidden when quiet
    public Text toastText;
    public GameObject sightMeterRoot; // case chrome — hidden while THE QUEUE runs
    public GameObject hintRoot;

    static readonly Color Paper = new Color32(0xE7, 0xE9, 0xEC, 0xFF);
    static readonly Color Fog = new Color32(0x97, 0x9D, 0xA8, 0xFF);
    static readonly Color Steel = new Color32(0x3A, 0x3F, 0x48, 0xFF);
    static readonly Color Anomaly = new Color32(0xE4, 0x56, 0x8A, 0xFF);
    static readonly Color Wine = new Color32(0x5E, 0x20, 0x36, 0xFF);

    float toastUntil;

    void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        Refresh();
    }

    void Update()
    {
        // combat owns the stage — and so does an open record or dossier:
        // the meter and hint chips are street chrome, not panel chrome
        bool combat = QueueCombatUI.CombatActive;
        bool readingOpen = controller != null && controller.reading != null && controller.reading.IsOpen;
        bool chromeHidden = combat || readingOpen || (panelRoot != null && panelRoot.activeSelf);
        if (sightMeterRoot != null && sightMeterRoot.activeSelf == chromeHidden) sightMeterRoot.SetActive(!chromeHidden);
        if (hintRoot != null && hintRoot.activeSelf == chromeHidden) hintRoot.SetActive(!chromeHidden);
        if (combat && panelRoot != null && panelRoot.activeSelf) panelRoot.SetActive(false);

        var kb = Keyboard.current;
        if (kb != null && kb.tabKey.wasPressedThisFrame && panelRoot != null && !combat && !readingOpen)
        {
            panelRoot.SetActive(!panelRoot.activeSelf);
            SfxBoss.Play(panelRoot.activeSelf ? "case_open" : "record_close");
            if (panelRoot.activeSelf) Refresh();
        }

        // sight meter
        if (!combat && sightState != null && sightCells != null)
        {
            float frac = sightState.Meter / Mathf.Max(1f, sightState.meterMax);
            int lit = Mathf.RoundToInt(frac * sightCells.Length);
            for (int i = 0; i < sightCells.Length; i++)
            {
                bool on = i < lit;
                sightCells[i].color = !on ? new Color(0.06f, 0.05f, 0.07f, 1f)
                    : (sightState.LockedOut ? Wine : (sightState.blend > 0.5f ? Anomaly : Fog));
            }
        }

        // interact prompt — chip appears only when something responds
        if (promptText != null && promptRoot != null)
        {
            var node = interactor != null ? interactor.Current : null;
            bool show = node != null && controller != null
                && controller.File.Catalog.TryGetValue(node.clueId, out var clue);
            if (show)
            {
                controller.File.Catalog.TryGetValue(node.clueId, out var c2);
                promptText.text = $"EXAMINE: {c2.Title.ToUpperInvariant()}";
            }
            if (promptRoot.activeSelf != show) promptRoot.SetActive(show);
        }

        if (toastText != null && Time.realtimeSinceStartup > toastUntil)
        {
            toastText.text = "";
            if (toastRoot != null && toastRoot.activeSelf) toastRoot.SetActive(false);
        }
    }

    public void Toast(string message)
    {
        if (toastText == null || QueueCombatUI.CombatActive) return;
        toastText.text = message;
        if (toastRoot != null) toastRoot.SetActive(true);
        toastUntil = Time.realtimeSinceStartup + 4.5f;
        SfxBoss.Play("toast");
    }

    public void Refresh()
    {
        if (controller == null || controller.File == null) return;
        var file = controller.File;

        if (counter != null)
            counter.text = $"EVIDENCE {file.FoundCount}/{file.Catalog.Count}";

        int i = 0;
        foreach (var clue in file.Catalog.Values)
        {
            if (i >= rowTitles.Length) break;
            var got = file.Get(clue.Id);
            rowTitles[i].text = got != null ? clue.Title : "· · · · · ·";
            rowTitles[i].color = got != null ? Paper : Steel;
            rowTags[i].text = got == null ? "" : (got.UnderSight ? "SIGHT" : "PLAIN");
            rowTags[i].color = got != null && got.UnderSight ? Anomaly : Fog;
            i++;
        }

        // ANALYSIS — the deduction assembles as evidence lands, so the
        // player can watch the clues CONNECT (newest lines shown)
        if (analysisLines != null && analysisLines.Length > 0)
        {
            var chain = new List<string>();
            foreach (var clue in file.Catalog.Values)
                if (file.Has(clue.Id) && !string.IsNullOrEmpty(clue.AnalysisLine))
                    chain.Add(clue.AnalysisLine);
            for (int k = 0; k < analysisLines.Length; k++)
            {
                int idx = chain.Count - analysisLines.Length + k;
                analysisLines[k].text = idx >= 0 ? $"› {chain[idx]}" : "";
                analysisLines[k].color = (k == analysisLines.Length - 1 && chain.Count > 0) ? Paper : Fog;
            }
            if (chain.Count == 0 && analysisLines.Length > 0)
                analysisLines[0].text = "› no evidence yet. The street knows more than you do.";
        }

        if (classifySection != null)
        {
            bool armed = file.CanClassify;
            bool done = file.Verdict != Verdict.Unclassified;
            classifyButtons.SetActive(armed && !done);
            classifyNote.text = done ? "" : (armed
                ? (string.IsNullOrEmpty(doctrine)
                    ? "the file will accept a classification. It will accept exactly one."
                    : doctrine)
                : $"insufficient — {CaseFile.CluesToClassify} pieces required");
            // the Type: taxonomy tag surfaces once the file is classified
            verdictStamp.text = file.Verdict switch
            {
                Verdict.UnHumanity => $"CLASSIFIED: UN-HUMANITY — {CaseUH001.CaseType}",
                Verdict.Remnant => $"CLASSIFIED: REMNANT — {CaseUH001.CaseType}",
                _ => "",
            };
        }
    }

    // button hooks
    public void OnClassifyUnHumanity() { SfxBoss.Play("ui_click"); controller.Classify(Verdict.UnHumanity); }
    public void OnClassifyRemnant() { SfxBoss.Play("ui_click"); controller.Classify(Verdict.Remnant); }
}
