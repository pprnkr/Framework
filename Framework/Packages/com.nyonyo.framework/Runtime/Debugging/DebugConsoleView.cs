using UnityEngine;

namespace Nyonyo.Framework.Debugging
{
    /// <summary>
    /// <see cref="DebugConsole"/>를 IMGUI로 그립니다. DebugConsole이 직접 붙입니다.
    /// </summary>
    sealed class DebugConsoleView : MonoBehaviour
    {
        const string InputControlName = "nyonyo-debug-console-input";

        internal DebugConsole Console;

        string _input = "";
        Vector2 _scroll;
        int _shownVersion = -1;
        bool _focusInput;
        GUIStyle _lineStyle;

        void OnGUI()
        {
            if (Console == null)
                return;

            var current = Event.current;
            if (current.type == EventType.KeyDown && current.keyCode == Console.ToggleKey)
            {
                Console.Toggle();
                _focusInput = Console.IsOpen;
                current.Use();
                return;
            }

            if (!Console.IsOpen)
                return;

            _lineStyle ??= new GUIStyle(GUI.skin.label) { wordWrap = true, richText = false };

            // 해상도와 상관없이 읽을 수 있도록 720p 기준으로 키웁니다.
            var scale = Mathf.Max(1f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var width = Screen.width / scale;
            var height = Screen.height / scale * 0.5f;

            GUI.depth = -1000;
            GUI.Box(new Rect(0f, 0f, width, height), GUIContent.none);
            GUILayout.BeginArea(new Rect(6f, 6f, width - 12f, height - 12f));

            if (_shownVersion != Console.Version)
            {
                _shownVersion = Console.Version;
                _scroll.y = float.MaxValue;
            }

            _scroll = GUILayout.BeginScrollView(_scroll);
            var defaultColor = GUI.contentColor;
            foreach (var line in Console.Lines)
            {
                GUI.contentColor = ColorOf(line.Type);
                GUILayout.Label(line.Text, _lineStyle);
            }

            GUI.contentColor = defaultColor;
            GUILayout.EndScrollView();

            var submit = current.type == EventType.KeyDown &&
                         (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter) &&
                         GUI.GetNameOfFocusedControl() == InputControlName;

            GUILayout.BeginHorizontal();
            GUI.SetNextControlName(InputControlName);
            _input = GUILayout.TextField(_input).Replace("`", "");
            if (GUILayout.Button("Run", GUILayout.Width(60f)))
                submit = true;
            if (GUILayout.Button("Close", GUILayout.Width(60f)))
                Console.Close();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            if (submit)
            {
                Console.Execute(_input);
                _input = "";
                _focusInput = true;
                if (current.type == EventType.KeyDown)
                    current.Use();
            }

            if (_focusInput)
            {
                GUI.FocusControl(InputControlName);
                _focusInput = false;
            }
        }

        static Color ColorOf(LogType type)
        {
            switch (type)
            {
                case LogType.Warning:
                    return new Color(1f, 0.85f, 0.4f);
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return new Color(1f, 0.5f, 0.5f);
                default:
                    return Color.white;
            }
        }
    }
}
