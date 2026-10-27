using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Nyonyo.Framework.Events;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nyonyo.Framework.Editor.Tests
{
    public class EventBusTests
    {
        private struct TestEvent : IEvent
        {
            public int Value;
        }

        private struct OtherEvent : IEvent
        {
        }

        private EventBus _bus;
        private List<string> _log;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _log = new List<string>();
        }

        [TearDown]
        public void TearDown() => _bus.Dispose();

        private Action<TestEvent> Record(string name) => e => _log.Add($"{name}:{e.Value}");

        [Test]
        public void Publish_PassesEventToHandler()
        {
            _bus.Subscribe(Record("A"));

            _bus.Publish(new TestEvent { Value = 7 });

            CollectionAssert.AreEqual(new[] { "A:7" }, _log);
        }

        [Test]
        public void Publish_WithoutSubscribers_DoesNothing()
        {
            Assert.DoesNotThrow(() => _bus.Publish(new TestEvent()));
        }

        [Test]
        public void Publish_OnlyCallsHandlersOfSameEventType()
        {
            _bus.Subscribe(Record("A"));
            _bus.Subscribe<OtherEvent>(_ => _log.Add("other"));

            _bus.Publish(new OtherEvent());

            CollectionAssert.AreEqual(new[] { "other" }, _log);
        }

        [Test]
        public void Publish_CallsHandlersInSubscriptionOrder()
        {
            _bus.Subscribe(Record("A"));
            _bus.Subscribe(Record("B"));
            _bus.Subscribe(Record("C"));

            _bus.Publish(new TestEvent { Value = 1 });

            CollectionAssert.AreEqual(new[] { "A:1", "B:1", "C:1" }, _log);
        }

        [Test]
        public void Unsubscribe_StopsReceiving()
        {
            var handler = Record("A");
            _bus.Subscribe(handler);

            _bus.Unsubscribe(handler);
            _bus.Publish(new TestEvent());

            CollectionAssert.IsEmpty(_log);
        }

        [Test]
        public void Unsubscribe_NotSubscribed_DoesNothing()
        {
            Assert.DoesNotThrow(() => _bus.Unsubscribe(Record("A")));
        }

        [Test]
        public void SubscriptionDispose_StopsReceiving_AndCanBeCalledTwice()
        {
            var subscription = _bus.Subscribe(Record("A"));

            subscription.Dispose();
            subscription.Dispose();
            _bus.Publish(new TestEvent());

            CollectionAssert.IsEmpty(_log);
        }

        [Test]
        public void SameHandlerTwice_IsCalledTwice_AndUnsubscribeRemovesOne()
        {
            var handler = Record("A");
            _bus.Subscribe(handler);
            _bus.Subscribe(handler);

            _bus.Publish(new TestEvent { Value = 1 });
            _bus.Unsubscribe(handler);
            _bus.Publish(new TestEvent { Value = 2 });

            CollectionAssert.AreEqual(new[] { "A:1", "A:1", "A:2" }, _log);
        }

        [Test]
        public void HandlerUnsubscribingItselfDuringPublish_OthersStillCalled()
        {
            IDisposable subscription = null;
            subscription = _bus.Subscribe<TestEvent>(e =>
            {
                _log.Add($"A:{e.Value}");
                subscription.Dispose();
            });
            _bus.Subscribe(Record("B"));

            _bus.Publish(new TestEvent { Value = 1 });
            _bus.Publish(new TestEvent { Value = 2 });

            CollectionAssert.AreEqual(new[] { "A:1", "B:1", "B:2" }, _log);
        }

        [Test]
        public void UnsubscribingLaterHandlerDuringPublish_SkipsItImmediately()
        {
            var handlerB = Record("B");
            _bus.Subscribe<TestEvent>(e =>
            {
                _log.Add($"A:{e.Value}");
                _bus.Unsubscribe(handlerB);
            });
            _bus.Subscribe(handlerB);

            _bus.Publish(new TestEvent { Value = 1 });

            CollectionAssert.AreEqual(new[] { "A:1" }, _log);
        }

        [Test]
        public void SubscribingDuringPublish_IsCalledFromNextPublish()
        {
            var subscribed = false;
            _bus.Subscribe<TestEvent>(e =>
            {
                _log.Add($"A:{e.Value}");
                if (subscribed)
                    return;

                subscribed = true;
                _bus.Subscribe(Record("B"));
            });

            _bus.Publish(new TestEvent { Value = 1 });
            _bus.Publish(new TestEvent { Value = 2 });

            CollectionAssert.AreEqual(new[] { "A:1", "A:2", "B:2" }, _log);
        }

        [Test]
        public void NestedPublishOfSameType_Works()
        {
            _bus.Subscribe<TestEvent>(e =>
            {
                _log.Add($"A:{e.Value}");
                if (e.Value == 1)
                    _bus.Publish(new TestEvent { Value = 2 });
            });
            _bus.Subscribe(Record("B"));

            _bus.Publish(new TestEvent { Value = 1 });

            CollectionAssert.AreEqual(new[] { "A:1", "A:2", "B:2", "B:1" }, _log);
        }

        [Test]
        public void HandlerThrows_OthersStillCalled_AndExceptionLogged()
        {
            _bus.Subscribe(Record("A"));
            _bus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("handler failed"));
            _bus.Subscribe(Record("C"));

            LogAssert.Expect(LogType.Exception, new Regex("handler failed"));
            _bus.Publish(new TestEvent { Value = 1 });

            CollectionAssert.AreEqual(new[] { "A:1", "C:1" }, _log);
        }

        [Test]
        public void Dispose_RemovesAllSubscriptions()
        {
            _bus.Subscribe(Record("A"));
            _bus.Subscribe<OtherEvent>(_ => _log.Add("other"));

            _bus.Dispose();
            _bus.Publish(new TestEvent());
            _bus.Publish(new OtherEvent());

            CollectionAssert.IsEmpty(_log);
        }

        [Test]
        public void SubscribeWithToken_UnsubscribesWhenCancelled()
        {
            using var cts = new CancellationTokenSource();
            _bus.Subscribe(Record("A"), cts.Token);

            _bus.Publish(new TestEvent { Value = 1 });
            cts.Cancel();
            _bus.Publish(new TestEvent { Value = 2 });

            CollectionAssert.AreEqual(new[] { "A:1" }, _log);
        }

        [Test]
        public void Subscribe_NullHandler_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _bus.Subscribe<TestEvent>(null));
        }
    }
}
