using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Nyonyo.Framework.Events;
using Nyonyo.Framework.Scenes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nyonyo.Framework.Editor.Tests
{
    public class SceneServiceTests
    {
        class FakeTransition : ISceneTransition
        {
            readonly List<string> _log;

            public float LastProgress = -1f;

            public FakeTransition(List<string> log) => _log = log;

            public Awaitable ShowAsync(CancellationToken cancellationToken)
            {
                _log.Add("show");
                return Completed();
            }

            public void SetProgress(float progress) => LastProgress = progress;

            public Awaitable HideAsync(CancellationToken cancellationToken)
            {
                _log.Add("hide");
                return Completed();
            }
        }

        class FakeLoader : ISceneLoader
        {
            readonly List<string> _log;

            public AwaitableCompletionSource Pending;
            public bool Throw;

            public FakeLoader(List<string> log) => _log = log;

            public Awaitable LoadAsync(string sceneName, Action<float> onProgress, CancellationToken cancellationToken)
            {
                _log.Add($"load:{sceneName}");
                if (Throw)
                    throw new InvalidOperationException("load failed");

                onProgress(0.5f);
                return Pending != null ? Pending.Awaitable : Completed();
            }
        }

        List<string> _log;
        FakeTransition _transition;
        FakeLoader _loader;
        EventBus _eventBus;
        SceneService _service;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _transition = new FakeTransition(_log);
            _loader = new FakeLoader(_log);
            _eventBus = new EventBus();
            _eventBus.Subscribe<SceneLoadStarted>(e => _log.Add($"started:{e.SceneName}"));
            _eventBus.Subscribe<SceneLoaded>(e => _log.Add($"loaded:{e.SceneName}"));
            _service = new SceneService(_transition, _eventBus, _loader);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
            _eventBus.Dispose();
        }

        static Awaitable Completed()
        {
            var source = new AwaitableCompletionSource();
            source.SetResult();
            return source.Awaitable;
        }

        // 가짜 연출과 로더는 즉시 완료되므로 LoadAsync도 동기적으로 끝납니다.
        static bool LoadNow(SceneService service, string sceneName)
        {
            var awaiter = service.LoadAsync(sceneName).GetAwaiter();
            Assert.IsTrue(awaiter.IsCompleted, "LoadAsync가 동기적으로 완료되지 않았습니다.");
            return awaiter.GetResult();
        }

        [Test]
        public void Load_RunsStepsInOrder()
        {
            var result = LoadNow(_service, "Stage1");

            Assert.IsTrue(result);
            CollectionAssert.AreEqual(
                new[] { "started:Stage1", "show", "load:Stage1", "loaded:Stage1", "hide" }, _log);
        }

        [Test]
        public void Load_ReportsProgressToTransition_AndEndsAtOne()
        {
            LoadNow(_service, "Stage1");

            Assert.AreEqual(1f, _service.Progress);
            Assert.AreEqual(1f, _transition.LastProgress);
        }

        [Test]
        public void Load_ResetsIsLoadingWhenFinished()
        {
            LoadNow(_service, "Stage1");

            Assert.IsFalse(_service.IsLoading);
        }

        [Test]
        public void Load_WhileLoading_IsIgnoredWithWarning()
        {
            _loader.Pending = new AwaitableCompletionSource();
            _ = _service.LoadAsync("Stage1");
            Assert.IsTrue(_service.IsLoading);

            LogAssert.Expect(LogType.Warning, new Regex("Stage2"));
            var result = LoadNow(_service, "Stage2");

            Assert.IsFalse(result);
            CollectionAssert.DoesNotContain(_log, "load:Stage2");

            _loader.Pending.SetResult();
        }

        [Test]
        public void Load_WhenLoaderThrows_ResetsIsLoadingAndCanLoadAgain()
        {
            _loader.Throw = true;
            Assert.Throws<InvalidOperationException>(() => LoadNow(_service, "Stage1"));
            Assert.IsFalse(_service.IsLoading);

            _loader.Throw = false;
            Assert.IsTrue(LoadNow(_service, "Stage1"));
        }

        [Test]
        public void Load_EmptyName_Throws()
        {
            Assert.Throws<ArgumentException>(() => LoadNow(_service, ""));
        }

        [Test]
        public void Load_WithoutTransitionAndEventBus_Works()
        {
            var service = new SceneService(loader: _loader);

            Assert.IsTrue(LoadNow(service, "Stage1"));
            CollectionAssert.AreEqual(new[] { "load:Stage1" }, _log);
        }

        [Test]
        public void Load_AfterDispose_Throws()
        {
            _service.Dispose();

            Assert.Throws<ObjectDisposedException>(() => LoadNow(_service, "Stage1"));
        }
    }
}
