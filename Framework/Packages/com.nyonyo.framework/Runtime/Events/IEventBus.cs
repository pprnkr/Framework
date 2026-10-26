using System;

namespace Nyonyo.Framework.Events
{
    /// <summary>
    /// 이벤트 타입별로 구독자에게 이벤트를 전달합니다. 메인 스레드에서만 사용하세요.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 이벤트를 구독합니다. 반환된 구독 객체를 Dispose하면 구독이 해제됩니다.
        /// 같은 핸들러를 두 번 구독하면 두 번 호출됩니다.
        /// </summary>
        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IEvent;

        /// <summary>
        /// 구독을 하나 해제합니다. 구독하지 않은 핸들러라면 아무 일도 하지 않습니다.
        /// </summary>
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IEvent;

        /// <summary>
        /// 구독한 순서대로 핸들러를 호출합니다. 핸들러 하나가 예외를 던져도 나머지 핸들러는 계속 호출합니다.
        /// </summary>
        void Publish<TEvent>(TEvent evt) where TEvent : struct, IEvent;
    }
}
