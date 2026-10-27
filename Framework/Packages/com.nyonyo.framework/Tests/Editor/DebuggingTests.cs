using System.Text.RegularExpressions;
using Nyonyo.Framework.Debugging;
using Nyonyo.Framework.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nyonyo.Framework.Editor.Tests
{
    public enum TestDifficulty
    {
        Easy,
        Hard,
    }

    public static class TestDebugCommands
    {
        [DebugCommand("test.add", "두 수를 더합니다")]
        private static int Add(int a, int b = 10) => a + b;

        [DebugCommand("test.echo")]
        private static string Echo(string[] args) => string.Join("|", args);

        [DebugCommand("test.mode")]
        private static string Mode(TestDifficulty difficulty, bool cheats) => $"{difficulty}:{cheats}";

        [DebugCommand("test.fail")]
        private static void Fail() => throw new System.InvalidOperationException("boom");
    }

    public class DebuggingTests
    {
        [TearDown]
        public void TearDown() => Log.MinimumLevel = LogLevel.Debug;

        [Test]
        public void Log_WritesWithChannelPrefix()
        {
            LogAssert.Expect(LogType.Log, "[Battle] hit 10");
            LogAssert.Expect(LogType.Warning, "[Battle] low hp");

            var channel = Log.Channel("Battle");
            channel.Info("hit 10");
            channel.Warning("low hp");
        }

        [Test]
        public void Log_MinimumLevelFiltersLowerLevels()
        {
            Log.MinimumLevel = LogLevel.Warning;

            Log.Debug("hidden");
            Log.Info("hidden");
            LogAssert.Expect(LogType.Error, "shown");
            Log.Error("shown");

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Tokenize_SplitsBySpaceAndKeepsQuotedText()
        {
            CollectionAssert.AreEqual(new[] { "say", "hello world", "x" }, new DebugCommandRegistry().Tokenize("  say \"hello world\"   x "));
            CollectionAssert.AreEqual(new[] { "set", "" }, new DebugCommandRegistry().Tokenize("set \"\""));
            Assert.AreEqual(0, new DebugCommandRegistry().Tokenize("   ").Length);
        }

        [Test]
        public void Execute_RunsRegisteredCommandCaseInsensitive()
        {
            var registry = new DebugCommandRegistry();
            registry.Register("greet", "", args => $"hi {args[0]}");

            Assert.AreEqual("hi bob", registry.Execute("GREET bob"));
            StringAssert.Contains("알 수 없는 명령", registry.Execute("nope"));
            StringAssert.Contains("greet", registry.Execute("help"));
        }

        [Test]
        public void RegisterAttributes_BindsTypedParameters()
        {
            var registry = new DebugCommandRegistry();
            var count = registry.RegisterAttributes(new[] { typeof(TestDebugCommands).Assembly });

            Assert.AreEqual(4, count);
            Assert.AreEqual("7", registry.Execute("test.add 3 4"));
            Assert.AreEqual("13", registry.Execute("test.add 3"), "기본값이 있는 인자는 생략할 수 있습니다.");
            Assert.AreEqual("a|b c", registry.Execute("test.echo a \"b c\""));
            Assert.AreEqual("Hard:True", registry.Execute("test.mode hard 1"));
            StringAssert.Contains("test.add <a:int> [b:int]", registry.Execute("help"));
        }

        [Test]
        public void Execute_ReportsErrorsInsteadOfThrowing()
        {
            var registry = new DebugCommandRegistry();
            registry.RegisterAttributes(new[] { typeof(TestDebugCommands).Assembly });

            StringAssert.Contains("오류: boom", registry.Execute("test.fail"));
            StringAssert.Contains("사용법", registry.Execute("test.add abc"));
            StringAssert.Contains("오류", registry.Execute("test.add"));
        }

        [Test]
        public void Console_RecordsCommandsAndLogs()
        {
            var parent = new GameObject("Console Parent");
            var console = new DebugConsole(parent.transform);
            try
            {
                Assert.IsTrue(console.IsEnabled, "에디터는 디버그 빌드로 취급합니다.");

                console.Execute("help");
                Debug.Log("captured log");

                var texts = new System.Collections.Generic.List<string>();
                foreach (var line in console.Lines)
                    texts.Add(line.Text);

                CollectionAssert.Contains(texts, "> help");
                CollectionAssert.Contains(texts, "captured log");

                console.Execute("clear");
                Assert.AreEqual(0, console.Lines.Count);
            }
            finally
            {
                console.Dispose();
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void Console_TimescaleCommandChangesTimeScale()
        {
            var parent = new GameObject("Console Parent");
            var console = new DebugConsole(parent.transform);
            try
            {
                console.Execute("timescale 0.5");
                Assert.AreEqual(0.5f, Time.timeScale);
            }
            finally
            {
                Time.timeScale = 1f;
                console.Dispose();
                Object.DestroyImmediate(parent);
            }
        }
    }
}
