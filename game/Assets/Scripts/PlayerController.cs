// UN-HUMANITY — gray-box player: 8-direction walk on the street block,
// hold-to-Sight. CharacterController + direct Input System polling (the
// real input map lands with the systems pass).
//   WASD / arrows / left stick — move
//   E held / gamepad west button — Sight (drain cost comes later)

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 3.0f;
    public float gravity = 20f;
    public Transform spriteQuad;      // X-flips to face travel direction
    public SightState sightState;
    public float sightBlendSpeed = 3f;

    public float CurrentSpeed { get; private set; }

    CharacterController cc;
    float fall;
    float facing = 1f;

    void Awake() => cc = GetComponent<CharacterController>();

    void Update()
    {
        var kb = Keyboard.current;
        var pad = Gamepad.current;

        Vector2 move = Vector2.zero;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
        }
        if (pad != null && move == Vector2.zero) move = pad.leftStick.ReadValue();
        move = Vector2.ClampMagnitude(move, 1f);

        // world axes: X across the street, Z up the travel axis
        CurrentSpeed = move.magnitude * walkSpeed;
        var wish = new Vector3(move.x, 0f, move.y) * walkSpeed;
        fall = cc.isGrounded ? -1f : fall - gravity * Time.deltaTime;
        wish.y = fall;
        cc.Move(wish * Time.deltaTime);

        if (Mathf.Abs(move.x) > 0.05f) facing = Mathf.Sign(move.x);
        if (spriteQuad != null)
        {
            var s = spriteQuad.localScale;
            s.x = Mathf.Abs(s.x) * facing;
            spriteQuad.localScale = s;
        }

        if (sightState != null)
        {
            bool hold = (kb != null && kb.eKey.isPressed)
                     || (pad != null && pad.buttonWest.isPressed);
            float target = hold ? 1f : 0f;
            float b = Mathf.MoveTowards(sightState.blend, target, sightBlendSpeed * Time.deltaTime);
            if (!Mathf.Approximately(b, sightState.blend)) sightState.SetSight(b);
        }
    }
}
