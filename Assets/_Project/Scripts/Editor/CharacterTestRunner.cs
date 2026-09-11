using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

namespace CultivationGame.Editor
{
    [InitializeOnLoad]
    public static class CharacterTestRunner
    {
        private static TestRunnerApi api;
        static CharacterTestRunner()
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Results());
        }
        public static void Run(bool playMode)
        {
            api.Execute(new ExecutionSettings(new Filter {
                testMode = playMode ? TestMode.PlayMode : TestMode.EditMode,
                assemblyNames = new[] { playMode ? "Game.Movement.Tests" : "Game.Tests" }
            }));
        }
        private class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                Directory.CreateDirectory("ArtSource/Playtests");
                TestRunnerApi.SaveResultToFile(result, Path.GetFullPath("ArtSource/Playtests/Tests-" + result.Test.TestMode + ".xml"));
                Debug.Log($"Character validation tests: {result.PassCount} passed, {result.FailCount} failed.");
            }
        }
    }
}
