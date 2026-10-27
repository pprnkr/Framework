using Nyonyo.Framework.UI;
using UnityEditor;
using UnityEngine;

namespace Nyonyo.Framework.Editor.UI
{
    /// <summary>
    /// <see cref="UISettings"/>의 화면 방향을 Android, iOS, WebGL, PC의 Player Settings에 한 번에 반영합니다.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Android, iOS: 자동 회전을 방향에 맞는 쪽으로만 허용합니다. 세로형은 정방향 세로만, 가로형은 좌우 가로를 허용합니다.</item>
    /// <item>WebGL: 웹 페이지의 기본 캔버스 크기를 방향에 맞춥니다.</item>
    /// <item>PC: 창 모드 기본 크기를 방향에 맞추고, 세로형은 창 모드로, 가로형은 전체 화면으로 시작합니다.
    /// 가로 모니터에서 세로형 게임을 전체 화면으로 띄우면 양옆이 비기 때문입니다.</item>
    /// <item>Android 창 모드(태블릿, 크롬북)의 기본 창 크기도 맞춥니다.</item>
    /// </list>
    /// 창과 캔버스 크기는 기준 해상도의 절반입니다(1080x1920이면 540x960). 기준 해상도 그대로는 1080p 모니터에 세로로 다 들어가지 않습니다.
    /// </remarks>
    // [MenuItem]과 인스펙터 버튼 어디서든 부르는 에디터 유틸리티라 static입니다. 상태는 없습니다.
    public static class OrientationPlayerSettings
    {
        /// <summary>
        /// Player Settings가 이 설정의 방향과 해상도에 맞는지 확인합니다.
        /// </summary>
        public static bool IsApplied(UISettings settings)
        {
            var portrait = settings.Orientation == GameOrientation.Portrait;
            var window = WindowSize(settings);

            bool orientationMatches;
            switch (PlayerSettings.defaultInterfaceOrientation)
            {
                case UIOrientation.Portrait:
                case UIOrientation.PortraitUpsideDown:
                    orientationMatches = portrait;
                    break;
                case UIOrientation.LandscapeLeft:
                case UIOrientation.LandscapeRight:
                    orientationMatches = !portrait;
                    break;
                default:
                    var allowsPortrait = PlayerSettings.allowedAutorotateToPortrait || PlayerSettings.allowedAutorotateToPortraitUpsideDown;
                    var allowsLandscape = PlayerSettings.allowedAutorotateToLandscapeLeft || PlayerSettings.allowedAutorotateToLandscapeRight;
                    orientationMatches = portrait ? allowsPortrait && !allowsLandscape : allowsLandscape && !allowsPortrait;
                    break;
            }

            return orientationMatches
                && PlayerSettings.fullScreenMode == FullScreenModeFor(settings)
                && PlayerSettings.defaultScreenWidth == window.x
                && PlayerSettings.defaultScreenHeight == window.y
                && PlayerSettings.defaultWebScreenWidth == window.x
                && PlayerSettings.defaultWebScreenHeight == window.y
                && PlayerSettings.Android.defaultWindowWidth == window.x
                && PlayerSettings.Android.defaultWindowHeight == window.y;
        }

        /// <summary>
        /// 모바일 화면 회전, WebGL 캔버스 크기, PC와 Android 창 크기를 설정의 방향에 맞춥니다.
        /// </summary>
        public static void Apply(UISettings settings)
        {
            var portrait = settings.Orientation == GameOrientation.Portrait;
            var window = WindowSize(settings);

            // Android, iOS
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = portrait;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = !portrait;
            PlayerSettings.allowedAutorotateToLandscapeRight = !portrait;

            // PC
            PlayerSettings.fullScreenMode = FullScreenModeFor(settings);
            PlayerSettings.defaultScreenWidth = window.x;
            PlayerSettings.defaultScreenHeight = window.y;

            // WebGL
            PlayerSettings.defaultWebScreenWidth = window.x;
            PlayerSettings.defaultWebScreenHeight = window.y;

            // Android 창 모드(태블릿, 크롬북)
            PlayerSettings.Android.defaultWindowWidth = window.x;
            PlayerSettings.Android.defaultWindowHeight = window.y;

            AssetDatabase.SaveAssets();
            Debug.Log($"[Nyonyo Framework] Player Settings를 {settings.Orientation}에 맞췄습니다. 창과 웹 캔버스 크기는 {window.x}x{window.y}입니다.");
        }

        private static FullScreenMode FullScreenModeFor(UISettings settings) =>
            settings.Orientation == GameOrientation.Portrait ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;

        private static Vector2Int WindowSize(UISettings settings)
        {
            var resolution = settings.ReferenceResolution;
            return new Vector2Int(Mathf.RoundToInt(resolution.x / 2f), Mathf.RoundToInt(resolution.y / 2f));
        }
    }
}
