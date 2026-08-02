// UN-HUMANITY — owns the live CaseFile for the scene and brokers between
// the street (clue nodes), the Sight state, and the case-log UI.

using UnityEngine;
using UnHumanity.Case;

public class CaseController : MonoBehaviour
{
    public SightState sightState;
    public CaseLogUI ui;

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
        File.Collect(node.clueId, SightActive);
        if (ui != null)
        {
            if (File.Log.Count > logBefore) ui.Toast(File.Log[File.Log.Count - 1]);
            ui.Refresh();
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
