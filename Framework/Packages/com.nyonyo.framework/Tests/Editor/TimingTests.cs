using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Nyonyo.Framework.Events;
using Nyonyo.Framework.Timing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nyonyo.Framework.Editor.Tests
{
    public class TimingTests
    {
        TimerService _timers;
        List<string> _log;

        [SetUp]
        public void SetUp()
        {
            _timers = new TimerService();
            _log = new List<string>();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        [Test]
        public void After_FiresOnceWhenTimeElapses()
        {
            var timer = _timers.After(1f, () => _log.Add("fired"));

            _timers.Tick(0.6f, 0.6f);
            Assert.AreEqual(0, _log.Count);
            Assert.AreEqual(0.4f, timer.Remaining, 1e-5f);

            _timers.Tick(0.6f, 0.6f);
            _timers.Tick(5f, 5f);

            CollectionAssert.AreEqual(new[] { "fired" }, _log);
            Assert.IsFalse(timer.IsActive);
            Assert.AreEqual(0, _timers.ActiveCount);
        }

        [Test]
        public void Every_RepeatsGivenCount()
        {
            _timers.Every(1f, () => _log.Add("tick"), repeatCount: 2);

            for (var i = 0; i < 5; i++)
                _timers.Tick(1f, 1f);

            Assert.AreEqual(2, _log.Count);
        }

        [Test]
        public void UnscaledTimer_UsesUnscaledDelta()
        {
            _timers.After(1f, () => _log.Add("scaled"));
            _timers.After(1f, () => _log.Add("unscaled"), unscaledTime: true);

            _timers.Tick(0f, 1f);

            CollectionAssert.AreEqual(new[] { "unscaled" }, _log);
        }

        [Test]
        public void Cancel_StopsTimer()
        {
            var timer = _timers.After(1f, () => _log.Add("fired"));

            timer.Cancel();
            _timers.Tick(2f, 2f);

            Assert.AreEqual(0, _log.Count);
        }

        [Test]
        public void DestroyedOwner_CancelsTimer()
        {
            var owner = new GameObject("Owner");
            _timers.After(1f, () => _log.Add("fired"), owner: owner);

            UnityEngine.Object.DestroyImmediate(owner);
            _timers.Tick(2f, 2f);

            Assert.AreEqual(0, _log.Count);
        }

        [Test]
        public void TimerAddedInCallback_StartsNextTick()
        {
            _timers.After(0f, () =>
            {
                _log.Add("first");
                _timers.After(0f, () => _log.Add("second"));
            });

            _timers.Tick(0f, 0f);
            CollectionAssert.AreEqual(new[] { "first" }, _log);

            _timers.Tick(0f, 0f);
            CollectionAssert.AreEqual(new[] { "first", "second" }, _log);
        }

        [Test]
        public void CallbackException_IsLoggedAndOthersRun()
        {
            _timers.After(0f, () => throw new InvalidOperationException("timer failed"));
            _timers.After(0f, () => _log.Add("ok"));

            LogAssert.Expect(LogType.Exception, new Regex("timer failed"));
            _timers.Tick(0f, 0f);

            CollectionAssert.AreEqual(new[] { "ok" }, _log);
        }

        [Test]
        public void Cooldown_TryUseStartsWaitAndReportsProgress()
        {
            var now = 10d;
            var cooldown = new Cooldown(2f, () => now);

            Assert.IsTrue(cooldown.TryUse());
            Assert.IsFalse(cooldown.TryUse());
            Assert.AreEqual(2f, cooldown.Remaining, 1e-5f);

            now = 11d;
            Assert.AreEqual(0.5f, cooldown.Progress, 1e-5f);

            now = 12d;
            Assert.IsTrue(cooldown.IsReady);
            Assert.AreEqual(1f, cooldown.Progress);

            cooldown.Start();
            cooldown.Reset();
            Assert.IsTrue(cooldown.IsReady);
        }

        [Test]
        public void Pause_RequiresAllRequestersToResume()
        {
            var pause = new PauseService();
            var popup = new object();
            var tutorial = new object();

            pause.Pause(popup);
            pause.Pause(tutorial);
            pause.Resume(popup);

            Assert.IsTrue(pause.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);

            pause.Resume(tutorial);

            Assert.IsFalse(pause.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
            pause.Dispose();
        }

        [Test]
        public void TimeScale_AppliesOnlyWhenNotPaused()
        {
            var pause = new PauseService();
            var requester = new object();

            pause.TimeScale = 0.5f;
            Assert.AreEqual(0.5f, Time.timeScale);

            pause.Pause(requester);
            Assert.AreEqual(0f, Time.timeScale);

            pause.Resume(requester);
            Assert.AreEqual(0.5f, Time.timeScale);
            pause.Dispose();
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void Pause_PublishesPauseChangedOnlyOnChange()
        {
            var events = new EventBus();
            var changes = new List<bool>();
            events.Subscribe<PauseChanged>(e => changes.Add(e.IsPaused));
            var pause = new PauseService(events);
            var a = new object();
            var b = new object();

            pause.Pause(a);
            pause.Pause(b);
            pause.Resume(a);
            pause.ResumeAll();

            CollectionAssert.AreEqual(new[] { true, false }, changes);
            pause.Dispose();
        }
    }
}
