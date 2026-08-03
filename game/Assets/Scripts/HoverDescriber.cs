// UN-HUMANITY — before a player presses anything, the strip tells them
// what it does. Works for mouse hover and pad/keyboard selection alike;
// routes through QueueCombatUI's single strip arbiter.

using UnityEngine;
using UnityEngine.EventSystems;

public class HoverDescriber : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [TextArea] public string description;
    public QueueCombatUI ui;

    public void OnPointerEnter(PointerEventData e) { if (ui != null) ui.SetHover(description); }
    public void OnPointerExit(PointerEventData e) { if (ui != null) ui.ClearHover(); }
    public void OnSelect(BaseEventData e) { if (ui != null) ui.SetHover(description); }
    public void OnDeselect(BaseEventData e) { if (ui != null) ui.ClearHover(); }
}
