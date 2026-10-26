using System;
using System.Threading;

namespace Nyonyo.Framework.Events
{
    public static class EventBusExtensions
    {
        /// <summary>
        /// 이벤트를 구독하고, <paramref name="unsubscribeToken"/>이 취소되면 자동으로 구독을 해제합니다.
        /// MonoBehaviour에서는 <c>destroyCancellationToken</c>을 넘기면 오브젝트가 파괴될 때 해제됩니다.
        /// </summary>
        /// <example>
        /// <code>
        /// void Start() => Services.Get&lt;IEventBus&gt;().Subscribe&lt;PlayerDied&gt;(OnPlayerDied, destroyCancellationToken);
        /// </code>
        /// </example>
        public static IDisposable Subscribe<TEvent>(
            this IEventBus bus, Action<TEvent> handler, CancellationToken unsubscribeToken)
            where TEvent : struct, IEvent
        {
            var subscription = bus.Subscribe(handler);
            unsubscribeToken.Register(subscription.Dispose);
            return subscription;
        }
    }
}
