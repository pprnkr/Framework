using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nyonyo.Framework.Logging
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        None,
    }

    /// <summary>
    /// 레벨과 카테고리가 있는 로그입니다. Debug와 Info는 릴리스 빌드에서 호출 자체가 사라져서 성능 비용이 없습니다.
    /// </summary>
    /// <remarks>
    /// Debug, Info는 에디터, Development Build, 또는 <c>NYONYO_LOG</c> 심볼이 있을 때만 컴파일됩니다(문자열 만들기 비용도 사라짐).
    /// Warning, Error는 항상 남습니다. <see cref="MinimumLevel"/>로 실행 중에 걸러낼 수도 있습니다.
    /// </remarks>
    /// <example>
    /// <code>
    /// Log.Info($"스테이지 {stage} 시작");
    ///
    /// static readonly LogChannel s_log = Log.Channel("Battle");
    /// s_log.Debug($"데미지 {damage}");   // [Battle] 데미지 10
    /// </code>
    /// </example>
    public static class Log
    {
        private const string EditorSymbol = "UNITY_EDITOR";
        private const string DevelopmentSymbol = "DEVELOPMENT_BUILD";
        private const string ForceSymbol = "NYONYO_LOG";

        /// <summary>이 레벨보다 낮은 로그는 남기지 않습니다. 기본값은 <see cref="LogLevel.Debug"/>(모두 남김)입니다.</summary>
        public static LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => MinimumLevel = LogLevel.Debug;

        [Conditional(EditorSymbol), Conditional(DevelopmentSymbol), Conditional(ForceSymbol)]
        public static void Debug(string message, Object context = null) => Write(LogLevel.Debug, null, message, context);

        [Conditional(EditorSymbol), Conditional(DevelopmentSymbol), Conditional(ForceSymbol)]
        public static void Info(string message, Object context = null) => Write(LogLevel.Info, null, message, context);

        public static void Warning(string message, Object context = null) => Write(LogLevel.Warning, null, message, context);

        public static void Error(string message, Object context = null) => Write(LogLevel.Error, null, message, context);

        public static void Exception(System.Exception exception, Object context = null) =>
            UnityEngine.Debug.LogException(exception, context);

        /// <summary>
        /// 메시지 앞에 <c>[카테고리]</c>를 붙이는 로그 채널을 만듭니다. static 필드에 만들어 두고 쓰세요.
        /// </summary>
        public static LogChannel Channel(string category) => new(category);

        internal static void Write(LogLevel level, string category, string message, Object context)
        {
            if (level < MinimumLevel)
                return;

            var text = category == null ? message : $"[{category}] {message}";
            switch (level)
            {
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(text, context);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(text, context);
                    break;
                default:
                    UnityEngine.Debug.Log(text, context);
                    break;
            }
        }
    }

    /// <summary>
    /// 카테고리가 붙는 로그입니다. <see cref="Log.Channel"/>로 만듭니다.
    /// </summary>
    public readonly struct LogChannel
    {
        private readonly string _category;

        public LogChannel(string category) => _category = category;

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("NYONYO_LOG")]
        public void Debug(string message, Object context = null) => Log.Write(LogLevel.Debug, _category, message, context);

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("NYONYO_LOG")]
        public void Info(string message, Object context = null) => Log.Write(LogLevel.Info, _category, message, context);

        public void Warning(string message, Object context = null) => Log.Write(LogLevel.Warning, _category, message, context);

        public void Error(string message, Object context = null) => Log.Write(LogLevel.Error, _category, message, context);
    }
}
