using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nyonyo.Framework.Editor.Tests
{
    public class ServiceRegistryTests
    {
        private interface IServiceA { }
        private interface IServiceB { }
        private interface IServiceC { }

        private class TestService : IServiceA, IServiceB, IServiceC, IInitializable, IDisposable
        {
            private readonly string _name;
            private readonly List<string> _log;

            public bool ThrowOnInitialize;
            public bool ThrowOnDispose;

            public TestService(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public void Initialize()
            {
                _log.Add($"init:{_name}");
                if (ThrowOnInitialize)
                    throw new InvalidOperationException("initialize failed");
            }

            public void Dispose()
            {
                _log.Add($"dispose:{_name}");
                if (ThrowOnDispose)
                    throw new InvalidOperationException("dispose failed");
            }
        }

        private class AsyncTestService : IServiceA, IServiceB, IServiceC, IAsyncInitializable, IDisposable
        {
            private readonly string _name;
            private readonly List<string> _log;

            public AsyncTestService(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public Awaitable InitializeAsync(CancellationToken cancellationToken)
            {
                _log.Add($"init:{_name}");
                var source = new AwaitableCompletionSource();
                source.SetResult();
                return source.Awaitable;
            }

            public void Dispose() => _log.Add($"dispose:{_name}");
        }

        private List<string> _log;
        private ServiceRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _registry = new ServiceRegistry();
        }

        [TearDown]
        public void TearDown() => _registry.Dispose();

        // 테스트 서비스는 모두 즉시 완료되므로 InitializeAsync도 동기적으로 끝납니다.
        private void InitializeNow()
        {
            var awaiter = _registry.InitializeAsync().GetAwaiter();
            Assert.IsTrue(awaiter.IsCompleted, "InitializeAsync가 동기적으로 완료되지 않았습니다.");
            awaiter.GetResult();
        }

        [Test]
        public void Get_ReturnsRegisteredInstance()
        {
            var service = new TestService("A", _log);
            _registry.Register<IServiceA>(service);

            Assert.AreSame(service, _registry.Get<IServiceA>());
        }

        [Test]
        public void Get_Unregistered_ThrowsWithTypeName()
        {
            var e = Assert.Throws<InvalidOperationException>(() => _registry.Get<IServiceA>());

            StringAssert.Contains(nameof(IServiceA), e.Message);
        }

        [Test]
        public void TryGet_Unregistered_ReturnsFalse()
        {
            Assert.IsFalse(_registry.TryGet(out IServiceA service));
            Assert.IsNull(service);
        }

        [Test]
        public void Register_SameTypeTwice_Throws()
        {
            _registry.Register<IServiceA>(new TestService("A", _log));

            Assert.Throws<InvalidOperationException>(() => _registry.Register<IServiceA>(new TestService("B", _log)));
        }

        [Test]
        public void Register_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _registry.Register<IServiceA>(null));
        }

        [Test]
        public void Register_AfterInitialization_Throws()
        {
            InitializeNow();

            Assert.Throws<InvalidOperationException>(() => _registry.Register<IServiceA>(new TestService("A", _log)));
        }

        [Test]
        public void Initialize_RunsInRegistrationOrder()
        {
            _registry.Register<IServiceA>(new TestService("A", _log));
            _registry.Register<IServiceB>(new AsyncTestService("B", _log));
            _registry.Register<IServiceC>(new TestService("C", _log));

            InitializeNow();

            CollectionAssert.AreEqual(new[] { "init:A", "init:B", "init:C" }, _log);
        }

        [Test]
        public void Initialize_SameInstanceUnderTwoTypes_RunsOnce()
        {
            var service = new TestService("A", _log);
            _registry.Register<IServiceA>(service);
            _registry.Register<IServiceB>(service);

            InitializeNow();
            _registry.Dispose();

            CollectionAssert.AreEqual(new[] { "init:A", "dispose:A" }, _log);
        }

        [Test]
        public void Initialize_Twice_Throws()
        {
            InitializeNow();

            Assert.Throws<InvalidOperationException>(InitializeNow);
        }

        [Test]
        public void Initialize_WhenServiceThrows_StopsAndDisposesOnlyInitialized()
        {
            _registry.Register<IServiceA>(new TestService("A", _log));
            _registry.Register<IServiceB>(new TestService("B", _log) { ThrowOnInitialize = true });
            _registry.Register<IServiceC>(new TestService("C", _log));

            Assert.Throws<InvalidOperationException>(InitializeNow);
            _registry.Dispose();

            CollectionAssert.AreEqual(new[] { "init:A", "init:B", "dispose:A" }, _log);
        }

        [Test]
        public void Dispose_RunsInReverseOrder()
        {
            _registry.Register<IServiceA>(new TestService("A", _log));
            _registry.Register<IServiceB>(new AsyncTestService("B", _log));
            _registry.Register<IServiceC>(new TestService("C", _log));
            InitializeNow();
            _log.Clear();

            _registry.Dispose();

            CollectionAssert.AreEqual(new[] { "dispose:C", "dispose:B", "dispose:A" }, _log);
        }

        [Test]
        public void Dispose_WhenServiceThrows_ContinuesAndLogsException()
        {
            _registry.Register<IServiceA>(new TestService("A", _log));
            _registry.Register<IServiceB>(new TestService("B", _log) { ThrowOnDispose = true });
            _registry.Register<IServiceC>(new TestService("C", _log));
            InitializeNow();
            _log.Clear();

            LogAssert.Expect(LogType.Exception, new Regex("dispose failed"));
            _registry.Dispose();

            CollectionAssert.AreEqual(new[] { "dispose:C", "dispose:B", "dispose:A" }, _log);
        }

        [Test]
        public void Dispose_ClearsRegistrations()
        {
            _registry.Register<IServiceA>(new TestService("A", _log));
            InitializeNow();

            _registry.Dispose();

            Assert.IsFalse(_registry.Contains<IServiceA>());
        }
    }
}
