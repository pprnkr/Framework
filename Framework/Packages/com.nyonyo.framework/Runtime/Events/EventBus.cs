using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nyonyo.Framework.Events
{
    /// <summary>
    /// 기본 <see cref="IEventBus"/> 구현입니다. 이벤트를 발행할 때 메모리를 할당하지 않습니다.
    /// </summary>
    /// <remarks>
    /// 이벤트를 전달하는 도중에 구독이 바뀌는 경우는 이렇게 처리합니다.
    /// 새로 구독한 핸들러는 다음 발행부터 호출됩니다.
    /// 아직 호출되지 않은 핸들러의 구독을 해제하면, 이번 발행에서도 호출되지 않습니다.
    /// </remarks>
    public sealed class EventBus : IEventBus, IDisposable
    {
        interface IChannel
        {
            void Clear();
        }

        sealed class Channel<TEvent> : IChannel
        {
            static readonly Predicate<Action<TEvent>> s_isRemoved = handler => handler == null;

            readonly List<Action<TEvent>> _handlers = new();
            int _publishDepth;
            bool _hasRemovals;

            public void Add(Action<TEvent> handler) => _handlers.Add(handler);

            public void Remove(Action<TEvent> handler)
            {
                for (var i = _handlers.Count - 1; i >= 0; i--)
                {
                    if (_handlers[i] != handler)
                        continue;

                    RemoveAt(i);
                    return;
                }
            }

            public void Clear()
            {
                for (var i = _handlers.Count - 1; i >= 0; i--)
                    RemoveAt(i);
            }

            public void Publish(TEvent evt)
            {
                _publishDepth++;
                try
                {
                    // 발행 도중 추가된 핸들러는 이번 발행에서 호출하지 않습니다.
                    var count = _handlers.Count;
                    for (var i = 0; i < count; i++)
                    {
                        var handler = _handlers[i];
                        if (handler == null)
                            continue;

                        try
                        {
                            handler(evt);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
                finally
                {
                    _publishDepth--;
                    if (_publishDepth == 0 && _hasRemovals)
                    {
                        _handlers.RemoveAll(s_isRemoved);
                        _hasRemovals = false;
                    }
                }
            }

            // 발행 도중에는 리스트 순서가 바뀌지 않도록 자리만 비워 두고, 발행이 끝난 뒤에 정리합니다.
            void RemoveAt(int index)
            {
                if (_publishDepth > 0)
                {
                    _handlers[index] = null;
                    _hasRemovals = true;
                }
                else
                {
                    _handlers.RemoveAt(index);
                }
            }
        }

        sealed class Subscription<TEvent> : IDisposable where TEvent : struct, IEvent
        {
            IEventBus _bus;
            readonly Action<TEvent> _handler;

            public Subscription(IEventBus bus, Action<TEvent> handler)
            {
                _bus = bus;
                _handler = handler;
            }

            public void Dispose()
            {
                _bus?.Unsubscribe(_handler);
                _bus = null;
            }
        }

        readonly Dictionary<Type, IChannel> _channels = new();

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (!_channels.TryGetValue(typeof(TEvent), out var channel))
            {
                channel = new Channel<TEvent>();
                _channels.Add(typeof(TEvent), channel);
            }

            ((Channel<TEvent>)channel).Add(handler);
            return new Subscription<TEvent>(this, handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IEvent
        {
            if (handler != null && _channels.TryGetValue(typeof(TEvent), out var channel))
                ((Channel<TEvent>)channel).Remove(handler);
        }

        public void Publish<TEvent>(TEvent evt) where TEvent : struct, IEvent
        {
            if (_channels.TryGetValue(typeof(TEvent), out var channel))
                ((Channel<TEvent>)channel).Publish(evt);
        }

        /// <summary>
        /// 모든 구독을 해제합니다.
        /// </summary>
        public void Dispose()
        {
            foreach (var channel in _channels.Values)
                channel.Clear();
        }
    }
}
