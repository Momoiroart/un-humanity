// Runs the EditMode combat tests inside the live editor and writes a
// plain-text verdict to Logs/combat-tests.txt so the CLI can poll it.
//   unity command eval "return CombatTestRunner.Run();"   (kick off)
//   ... poll Logs/combat-tests.txt

using System.IO;
using System.Text;
using UnityEditor.TestTools.TestRunner.Api;

public static class CombatTestRunner
{
    class Callbacks : ICallbacks
    {
        readonly StringBuilder sb = new();
        int passed, failed;

        public void RunStarted(ITestAdaptor testsToRun) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            sb.Insert(0, $"RESULT {(failed == 0 ? "PASS" : "FAIL")} — {passed} passed, {failed} failed\n");
            File.WriteAllText(Path.GetFullPath("Logs/combat-tests.txt"), sb.ToString());
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.HasChildren) return;
            bool ok = result.TestStatus == TestStatus.Passed;
            if (ok) passed++; else failed++;
            sb.AppendLine($"{(ok ? "  ok " : "FAIL ")} {result.FullName}");
            if (!ok) sb.AppendLine($"      {result.Message?.Trim()}");
        }
    }

    public static string Run()
    {
        var path = Path.GetFullPath("Logs/combat-tests.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        if (File.Exists(path)) File.Delete(path);

        var api = UnityEngine.ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Callbacks());
        api.Execute(new ExecutionSettings(new Filter
        {
            testMode = TestMode.EditMode,
            assemblyNames = new[] { "UnHumanity.Combat.Tests" }, // holds both combat and case suites
        }));
        return "combat tests started — poll Logs/combat-tests.txt";
    }
}
