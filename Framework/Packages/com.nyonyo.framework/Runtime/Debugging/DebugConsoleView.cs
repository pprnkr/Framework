using UnityEngine;

namespace Nyonyo.Framework.Debugging
{
    /// <summary>
    /// <see cref="DebugConsole"/>를 IMGUI로 그립니다. DebugConsole이 직접 붙입니다.
    /// </summary>
    sealed class DebugConsoleView : MonoBehaviour
    {
        private const string InputControlName = "nyonyo-debug-console-input";

        internal DebugConsole Console;

        private string _input = "";
        private Vector2 _scroll;
        private int _shownVersion = -1;
        private bool _focusInput;
        private GUIStyle _lineStyle;

        // 키보드가 없는 모바일(Android, iOS, 모바일 웹)에서는 손가락 여러 개로 동시에 탭해서 콘솔을 여닫습니다.
        private void Update()
        {
            if (Console == null || Console.ToggleTouchCount <= 0)
                return;

            if (WasMultiTouchTapped(Console.ToggleTouchCount))
            {
                Console.Toggle();
                _focusInput = Console.IsOpen;
            }
        }

        // 마지막 손가락이 닿은 프레임에, 화면에 닿은 손가락 수가 정확히 count개이면 true입니다.
        private bool WasMultiTouchTapped(int count)
        {
#if NYONYO_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var touchscreen = UnityEngine.InputSystem.Touchscreen.current;
            if (touchscreen == null)
                return false;

            var active = 0;
            var began = false;
            foreach (var touch in touchscreen.touches)
            {
                if (!touch.isInProgress)
                    continue;

                active++;
                began |= touch.press.wasPressedThisFrame;
            }

            return began && active == count;
#else
            if (Input.touchCount != count)
                return false;

            for (var i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                    return true;
            }

            return false;
#endif
        }

        private void OnGUI()
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

            // 해상도와 상관없이 읽을 수 있도록 짧은 변 720 기준으로 키웁니다. 세로 화면에서도 폭이 너무 좁아지지 않습니다.
            var scale = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / 720f);
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

        private Color ColorOf(LogType type)
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
