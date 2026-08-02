// UN-HUMANITY — owns the live CaseFile for the scene and brokers between
// the street (clue nodes), the Sight state, and the case-log UI.

using UnityEngine;
using UnHumanity.Case;

public class CaseController : MonoBehaviour
{
    public SightState sightState;
    public CaseLogUI ui;
    public ReadingUI reading;
    public Texture2D[] evidenceImages;   // catalog order, wired by CaseSetup

    CaseFile file;
    public CaseFile File
    {
        get
        {
            if (file == null) InitFile();
            return file;
        }
    }

    void Awake() => InitFile();

    void InitFile()
    {
        if (file != null) return;
        file = CaseUH001.NewCaseFile();
        foreach (var node in FindObjectsByType<ClueNode>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (file.Catalog.TryGetValue(node.clueId, out var clue))
                node.respondsInNormalcy = clue.RespondsInNormalcy;
    }

    public bool SightActive => sightState != null && sightState.blend > 0.5f;

    public void TryCollect(ClueNode node)
    {
        if (node == null || File == null) return;
        int logBefore = File.Log.Count;
        bool changed = File.Collect(node.clueId, SightActive);
        if (ui != null)
        {
            if (File.Log.Count > logBefore) ui.Toast(File.Log[File.Log.Count - 1]);
            ui.Refresh();
        }

        // A successful collect — or a re-read of evidence already in the
        // file — opens the record itself. Refusals only toast.
        var got = File.Get(node.clueId);
        if (got != null && reading != null && (changed || got != null))
        {
            Texture2D img = null;
            int idx = (int)node.clueId;
            if (evidenceImages != null && idx >= 0 && idx < evidenceImages.Length)
                img = evidenceImages[idx];
            reading.Show(got.Clue.Title, got.Text, got.UnderSight, img);
        }
    }

    public void Classify(Verdict verdict)
    {
        if (File.Classify(verdict) && ui != null)
        {
            ui.Toast(File.Log[File.Log.Count - 1]);
            ui.Refresh();
        }
    }
}
