// Flipbook driver for the player's lit billboard quad. The sheet is
// 4 columns x 2 rows (walk 0-3 on the top row, idle 0-1 on the bottom);
// frames are selected by offsetting _BaseMap_ST — no Animator asset needed
// for a gray-box placeholder.

using UnityEngine;

public class SpriteAnimator : MonoBehaviour
{
    public PlayerController controller;
    public Renderer quad;
    public float walkFps = 7f;
    public float idleFps = 2f;
    public int walkFrames = 4;
    public int idleFrames = 2;

    static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
    MaterialPropertyBlock mpb;
    float clock;

    void LateUpdate()
    {
        if (quad == null) return;
        mpb ??= new MaterialPropertyBlock();

        bool walking = controller != null && controller.CurrentSpeed > 0.1f;
        float fps = walking ? walkFps : idleFps;
        int count = walking ? walkFrames : idleFrames;
        clock += Time.deltaTime * fps;
        int frame = Mathf.FloorToInt(clock) % count;

        // u: column; v: walk row sits at the top (offset 0.5), idle at 0.
        var st = new Vector4(0.25f, 0.5f, frame * 0.25f, walking ? 0.5f : 0f);
        quad.GetPropertyBlock(mpb);
        mpb.SetVector(BaseMapST, st);
        quad.SetPropertyBlock(mpb);
    }
}
