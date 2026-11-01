using UnityEngine;

namespace Nyonyo.Framework.Auth
{
    /// <summary>
    /// <see cref="FakeAuthService"/>의 가짜 로그인 창을 IMGUI로 그립니다.
    /// </summary>
    internal sealed class FakeAuthView : MonoBehaviour
    {
        internal FakeAuthService Service;

        private GUIStyle _titleStyle;

        private void OnGUI()
        {
            if (Service == null || !Service.IsPresenting)
                return;

            _titleStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold };
            GUI.depth = -2000;

            // 해상도와 상관없이 누르기 쉽도록 짧은 변 540 기준으로 키웁니다.
            var scale = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / 540f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var width = Screen.width / scale;
            var height = Screen.height / scale;

            var panel = new Rect(width * 0.1f, height * 0.25f, width * 0.8f, height * 0.5f);
            GUI.Box(panel, GUIContent.none);
            var action = Service.IsPresentingLink ? "계정 연결" : "로그인";
            GUI.Label(new Rect(panel.x, panel.y + 20f, panel.width, 40f), $"TEST {Service.PresentingProvider} {action}", _titleStyle);

            var buttonWidth = panel.width - 40f;
            var y = panel.y + 80f;
            if (DrawButton(panel.x + 20f, ref y, buttonWidth, "성공"))
                Service.Respond(AuthResult.Success);
            if (Service.IsPresentingLink && DrawButton(panel.x + 20f, ref y, buttonWidth, "이미 다른 계정에 연결됨"))
                Service.Respond(AuthResult.AlreadyInUse);
            if (DrawButton(panel.x + 20f, ref y, buttonWidth, "네트워크 오류"))
                Service.Respond(AuthResult.NetworkError);
            if (DrawButton(panel.x + 20f, ref y, buttonWidth, "취소"))
                Service.Respond(AuthResult.Cancelled);
        }

        private bool DrawButton(float x, ref float y, float width, string text)
        {
            var clicked = GUI.Button(new Rect(x, y, width, 44f), text);
            y += 54f;
            return clicked;
        }
    }
}
