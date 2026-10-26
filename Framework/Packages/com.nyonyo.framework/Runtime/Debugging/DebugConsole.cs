using System;
using System.Collections.Generic;
using System.Globalization;
using Nyonyo.Framework.Scenes;
using Nyonyo.Framework.Timing;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nyonyo.Framework.Debugging
{
    /// <summary>
    /// 게임 화면 위에 띄우는 로그 보기 + 명령 입력 창입니다. 기본적으로 에디터와 Development Build에서만 동작합니다.
    /// </summary>
    /// <remarks>
    /// <c>`</c>(백쿼트) 키로 열고 닫습니다. 모바일에서는 숨은 버튼 등에서 <see cref="Toggle"/>을 부르세요.
    /// <see cref="DebugCommandAttribute"/>가 붙은 static 메서드는 자동으로 명령이 됩니다.
    /// 기본 명령: help, clear, timescale &lt;값&gt;, scene &lt;이름&gt;.
    /// 화면은 IMGUI로 그려서 별도 에셋이 필요 없고 모든 UI 층보다 위에 보입니다.
    /// </remarks>
    public sealed class DebugConsole : IDisposable
    {
        internal readonly struct Line
        {
            public Line(string text, LogType type)
            {
                Text = text;
                Type = type;
            }

            public string Text { get; }

            public LogType Type { get; }
        }

        readonly List<Line> _lines = new();
        readonly int _maxLines;
        readonly GameObject _viewObject;

        /// <summary>
        /// 서비스로 등록된 DebugConsole입니다. <c>Services.Get&lt;DebugConsole&gt;()</c>와 같습니다.
        /// </summary>
        public static DebugConsole Instance => Services.Get<DebugConsole>();

        /// <param name="parent">콘솔 화면 오브젝트를 붙일 부모입니다. 보통 <see cref="Bootstrapper.Root"/>를 넘깁니다.</param>
        /// <param name="enableInReleaseBuild">true면 릴리스 빌드에서도 동작합니다. 출시 빌드에서는 켜지 마세요.</param>
        /// <param name="maxLines">보관할 로그 줄 수입니다.</param>
        public DebugConsole(Transform parent, bool enableInReleaseBuild = false, int maxLines = 300)
        {
            _maxLines = Mathf.Max(10, maxLines);
            Commands = new DebugCommandRegistry();
            RegisterBuiltInCommands();

            IsEnabled = Debug.isDebugBuild || enableInReleaseBuild;
            if (!IsEnabled)
                return;

            Commands.RegisterAttributes(GameAssemblies.Get());
            Application.logMessageReceived += OnLogMessage;

            _viewObject = new GameObject("[DebugConsole]");
            if (parent != null)
                _viewObject.transform.SetParent(parent, false);
            else if (Application.isPlaying)
                Object.DontDestroyOnLoad(_viewObject);
            _viewObject.AddComponent<DebugConsoleView>().Console = this;
        }

        public DebugCommandRegistry Commands { get; }

        /// <summary>이 빌드에서 콘솔이 동작하면 true입니다.</summary>
        public bool IsEnabled { get; }

        public bool IsOpen { get; private set; }

        /// <summary>콘솔을 여닫는 키입니다. 기본값은 백쿼트(`)입니다.</summary>
        public KeyCode ToggleKey { get; set; } = KeyCode.BackQuote;

        internal IReadOnlyList<Line> Lines => _lines;

        internal int Version { get; private set; }

        public void Toggle()
        {
            if (IsEnabled)
                IsOpen = !IsOpen;
        }

        public void Open()
        {
            if (IsEnabled)
                IsOpen = true;
        }

        public void Close() => IsOpen = false;

        /// <summary>
        /// 명령을 실행하고, 입력과 결과를 콘솔에 남깁니다.
        /// </summary>
        public string Execute(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            AddLine($"> {line}", LogType.Log);
            var result = Commands.Execute(line);
            if (!string.IsNullOrEmpty(result))
                AddLine(result, LogType.Log);
            return result;
        }

        public void Clear()
        {
            _lines.Clear();
            Version++;
        }

        public void Dispose()
        {
            Application.logMessageReceived -= OnLogMessage;
            IsOpen = false;
        }

        void OnLogMessage(string message, string stackTrace, LogType type)
        {
            var text = type == LogType.Exception || type == LogType.Error || type == LogType.Assert
                ? $"{message}\n{FirstLines(stackTrace, 3)}"
                : message;
            AddLine(text, type);
        }

        void AddLine(string text, LogType type)
        {
            _lines.Add(new Line(text, type));
            if (_lines.Count > _maxLines)
                _lines.RemoveRange(0, _lines.Count - _maxLines);
            Version++;
        }

        void RegisterBuiltInCommands()
        {
            Commands.Register("clear", "콘솔을 비웁니다.", _ => Clear());
            Commands.Register("timescale", "게임 속도를 바꿉니다. 예: timescale 0.5", args =>
            {
                if (args.Length == 0)
                    return $"timescale: {Time.timeScale}";

                var value = float.Parse(args[0], CultureInfo.InvariantCulture);
                if (Services.TryGet(out PauseService pause))
                    pause.TimeScale = value;
                else
                    Time.timeScale = value;
                return $"timescale: {value}";
            }, "timescale [value:float]");
            Commands.Register("scene", "씬을 불러옵니다. 예: scene Stage1", args =>
            {
                if (args.Length == 0)
                    return "사용법: scene <name>";
                if (!Services.TryGet(out ISceneService scenes))
                    return "ISceneService가 등록되지 않았습니다.";

                _ = scenes.LoadAsync(args[0]);
                return $"{args[0]} 씬을 불러옵니다.";
            }, "scene <name:string>");
        }

        static string FirstLines(string text, int count)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var lines = text.Split('\n');
            return string.Join("\n", lines, 0, Math.Min(count, lines.Length)).TrimEnd();
        }
    }
}
