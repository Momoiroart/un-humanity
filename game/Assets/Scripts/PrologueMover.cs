// UN-HUMANITY — the player's mover during Slice 0's prologue. Lightweight,
// decoupled from the street's PlayerController (no Sight, no combat). Walks
// WASD/stick; freezes whenever the flow has a card or conversation up.

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PrologueMover : MonoBehaviour
{
    public float walkSpeed = 2.6f;
    public Transform spriteQuad;   // X-flips to face travel

    CharacterController cc;
    float fall, facing = 1f;

    void Awake() => cc = GetComponent<CharacterController>();

    void Update()
    {
        if (PrologueFlow.InputLocked) return;
        var kb = Keyboard.current;
        var pad = Gamepad.current;
        Vector2 m = Vector2.zero;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) m.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) m.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) m.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) m.y += 1f;
        }
        if (pad != null && m == Vector2.zero) m = pad.leftStick.ReadValue();
        m = Vector2.ClampMagnitude(m, 1f);

        var wish = new Vector3(m.x, 0f, m.y) * walkSpeed;
        fall = cc.isGrounded ? -1f : fall - 20f * Time.deltaTime;
        wish.y = fall;
        cc.Move(wish * Time.deltaTime);

        if (Mathf.Abs(m.x) > 0.05f) facing = Mathf.Sign(m.x);
        if (spriteQuad != null)
        {
            var s = spriteQuad.localScale;
            s.x = Mathf.Abs(s.x) * facing;
            spriteQuad.localScale = s;
        }
    }
}
