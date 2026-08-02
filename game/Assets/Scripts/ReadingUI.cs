// UN-HUMANITY — the evidence record. Examining a clue opens an actual
// document: photograph (or WITHHELD card), title, and the full reading.
// F or click closes it; the case log keeps the summary.

using UnityEngine;
using UnityEngine.UI;

public class ReadingUI : MonoBehaviour
{
    [Header("wired by CaseSetup")]
    public GameObject panelRoot;
    public RawImage photo;
    public Text title;
    public Text stateTag;
    public Text body;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    public void Show(string clueTitle, string text, bool underSight, Texture2D image)
    {
        if (panelRoot == null) return;
        title.text = clueTitle.ToUpperInvariant();
        stateTag.text = underSight ? "RECORDED UNDER SIGHT" : "PLAIN OBSERVATION";
        stateTag.color = underSight ? new Color32(0xE4, 0x56, 0x8A, 0xFF) : new Color32(0x97, 0x9D, 0xA8, 0xFF);
        body.text = text;
        photo.texture = image;
        photo.enabled = image != null;
        panelRoot.SetActive(true);
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}
