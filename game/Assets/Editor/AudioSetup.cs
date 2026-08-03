// UN-HUMANITY — builds the sound board. Curated cues live in
// Assets/Audio as SFX_<cue>.wav (hand-picked from the asset shelf at
// D:\AssetLibrary\UN-HUMANITY — never import the full libraries).
// Re-runnable: fixes import settings, rebuilds the AUDIO root, wires
// every cue it finds. Missing files are reported, not fatal.
//   unity command eval "return AudioSetup.Build();"

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AudioSetup
{
    const string kScene = "Assets/Scenes/SC_02_StreetBlock.unity";
    const string kDir = "Assets/Audio";

    // every cue the runtime asks for — SFX_<name>.wav in Assets/Audio
    static readonly string[] kCues =
    {
        "ui_click", "ui_denied", "toast", "case_open", "record_open",
        "record_close", "sight_enter", "dread_sting", "combat_start",
        "flare", "radio", "photograph", "escort", "hold", "withdraw",
        "turn_theft", "counter_delete", "waiting_lock", "win", "lose",
    };
    static readonly string[] kLoops = { "sight_loop", "morning_loop" };

    public static string Build()
    {
        if (!Directory.Exists(kDir))
            return "FAILED: Assets/Audio missing — copy the curated wavs first";

        // import settings: short cues decompress, beds stream
        foreach (var path in Directory.GetFiles(kDir, "SFX_*.wav"))
        {
            var p = path.Replace('\\', '/');
            var imp = (AudioImporter)AssetImporter.GetAtPath(p);
            if (imp == null) continue;
            var s = imp.defaultSampleSettings;
            bool isLoop = kLoops.Any(l => p.EndsWith($"SFX_{l}.wav"));
            s.loadType = isLoop ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            imp.defaultSampleSettings = s;
            imp.forceToMono = !isLoop;
            imp.SaveAndReimport();
        }

        var scene = EditorSceneManager.OpenScene(kScene, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "AUDIO"))
            Object.DestroyImmediate(g);

        var root = new GameObject("AUDIO");
        var boss = root.AddComponent<SfxBoss>();

        AudioSource Src(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var a = go.AddComponent<AudioSource>();
            a.playOnAwake = false;
            a.loop = loop;
            a.spatialBlend = 0f;   // 2D — the HUD of sound
            return a;
        }
        boss.oneShot = Src("OneShot", false);
        boss.sightLoop = Src("SightBed", true);
        boss.morningLoop = Src("MorningBed", true);

        var wired = new List<SfxBoss.Cue>();
        var missing = new List<string>();
        foreach (var cue in kCues)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{kDir}/SFX_{cue}.wav");
            if (clip == null) { missing.Add(cue); continue; }
            wired.Add(new SfxBoss.Cue { name = cue, clip = clip, volume = 0.7f });
        }
        boss.cues = wired.ToArray();

        boss.sightLoop.clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{kDir}/SFX_sight_loop.wav");
        boss.morningLoop.clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{kDir}/SFX_morning_loop.wav");
        if (boss.sightLoop.clip == null) missing.Add("sight_loop");
        if (boss.morningLoop.clip == null) missing.Add("morning_loop(optional)");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"sound board built: {wired.Count} cues wired"
             + (missing.Count > 0 ? $", missing: {string.Join(",", missing)}" : "");
    }
}
