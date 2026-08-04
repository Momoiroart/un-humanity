// UN-HUMANITY — creates and registers Slice 0's separate scenes. Each
// prologue location is its own .unity file (mood law writes GLOBAL
// RenderSettings, so a warm bedroom and the wine-black street cannot share
// a scene). A persistent core scene carries the player/camera/UI across
// the hops; location scenes hold only geometry + mood + spawn + beats.
//   unity command eval "return PrologueScenes.EnsureScenes();"

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrologueScenes
{
    // build-settings order: Core boots first, then the three locations, then
    // the exam street. Paths come from the runtime PrologueScenePaths.
    static readonly string[] kAll = {
        PrologueScenePaths.Core, PrologueScenePaths.Bedroom, PrologueScenePaths.Walk,
        PrologueScenePaths.School, PrologueScenePaths.Handoff, PrologueScenePaths.Street };

    public static string EnsureScenes()
    {
        var made = new List<string>();
        foreach (var path in kAll)
        {
            if (System.IO.File.Exists(path)) continue;
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(s, path);
            made.Add(System.IO.Path.GetFileNameWithoutExtension(path));
        }

        // register all in EditorBuildSettings (idempotent, order preserved)
        var scenes = kAll.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        EditorBuildSettings.scenes = scenes;

        return $"scenes ensured: created [{string.Join(",", made)}]; "
             + $"{kAll.Length} registered in build settings (Core boots first)";
    }
}
