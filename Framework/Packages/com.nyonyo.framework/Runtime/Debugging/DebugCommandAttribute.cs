using System;

namespace Nyonyo.Framework.Debugging
{
    /// <summary>
    /// static 메서드를 디버그 콘솔 명령으로 등록합니다. <see cref="DebugConsole"/>가 시작할 때 자동으로 찾습니다.
    /// </summary>
    /// <remarks>
    /// 매개변수는 string, int, long, float, double, bool, enum을 쓸 수 있고, 기본값이 있으면 생략할 수 있습니다.
    /// <c>string[]</c> 매개변수 하나만 있으면 입력한 인자를 그대로 받습니다. 반환값이 있으면 콘솔에 표시합니다.
    /// </remarks>
    /// <example>
    /// <code>
    /// [DebugCommand("gold", "골드를 더합니다")]
    /// static string AddGold(int amount = 1000)
    /// {
    ///     Wallet.Gold += amount;
    ///     return $"골드: {Wallet.Gold}";
    /// }
    /// // 콘솔에서: gold 500
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class DebugCommandAttribute : Attribute
    {
        public DebugCommandAttribute(string name, string description = "")
        {
            Name = name;
            Description = description;
        }

        public string Name { get; }

        public string Description { get; }
    }
}
