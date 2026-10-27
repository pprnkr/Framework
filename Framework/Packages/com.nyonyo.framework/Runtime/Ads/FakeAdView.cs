using UnityEngine;

namespace Nyonyo.Framework.Ads
{
    /// <summary>
    /// <see cref="FakeAdService"/>의 테스트 배너와 전체 화면 창을 IMGUI로 그립니다.
    /// </summary>
    internal sealed class FakeAdView : MonoBehaviour
    {
        internal FakeAdService Service;

        private GUIStyle _bannerStyle;
        private GUIStyle _titleStyle;

        private void OnGUI()
        {
            if (Service == null)
                return;

            _bannerStyle ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _titleStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold };
            GUI.depth = -2000;

            if (Service.IsBannerVisible)
                DrawBanner();
            if (Service.IsPresenting)
                DrawFullScreen();
        }

        private void DrawBanner()
        {
            var height = Service.BannerHeight;
            var y = Service.BannerPosition == BannerPosition.Top ? 0f : Screen.height - height;
            GUI.matrix = Matrix4x4.identity;
            GUI.Box(new Rect(0f, y, Screen.width, height), "TEST BANNER", _bannerStyle);
        }

        private void DrawFullScreen()
        {
            // 해상도와 상관없이 누르기 쉽도록 짧은 변 540 기준으로 키웁니다.
            var scale = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / 540f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var width = Screen.width / scale;
            var height = Screen.height / scale;

            var panel = new Rect(width * 0.1f, height * 0.3f, width * 0.8f, height * 0.4f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x, panel.y + 20f, panel.width, 40f), $"TEST {Service.PresentingFormat}", _titleStyle);

            var buttonWidth = panel.width - 40f;
            var buttonY = panel.yMax - 120f;
            if (Service.PresentingFormat == AdFormat.Rewarded)
            {
                if (GUI.Button(new Rect(panel.x + 20f, buttonY, buttonWidth, 44f), "보상 받고 닫기"))
                    Service.Respond(true);
                if (GUI.Button(new Rect(panel.x + 20f, buttonY + 54f, buttonWidth, 44f), "건너뛰기 (보상 없음)"))
                    Service.Respond(false);
            }
            else if (GUI.Button(new Rect(panel.x + 20f, buttonY + 54f, buttonWidth, 44f), "닫기"))
            {
                Service.Respond(false);
            }
        }
    }
}
