namespace Nyonyo.Framework.Events
{
    /// <summary>
    /// <see cref="IEventBus"/>로 주고받는 이벤트임을 표시합니다. 이벤트는 struct로 정의하세요.
    /// </summary>
    /// <example>
    /// <code>
    /// public readonly struct PlayerDied : IEvent
    /// {
    ///     public readonly int PlayerId;
    ///     public PlayerDied(int playerId) => PlayerId = playerId;
    /// }
    /// </code>
    /// </example>
    public interface IEvent
    {
    }
}
