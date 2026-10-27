using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Nyonyo.Framework.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Nyonyo.Framework.Editor.Tests
{
    public class TweenTests
    {
        private readonly List<GameObject> _objects = new();
        private List<string> _log;

        [SetUp]
        public void SetUp()
        {
            TweenManager.Clear();
            _log = new List<string>();
        }

        [TearDown]
        public void TearDown()
        {
            TweenManager.Clear();
            foreach (var go in _objects)
                if (go != null)
                    Object.DestroyImmediate(go);
            _objects.Clear();
        }

        #region Easing

        [Test]
        public void Easing_StartsAtZeroAndEndsAtOne()
        {
            foreach (Ease ease in Enum.GetValues(typeof(Ease)))
            {
                Assert.AreEqual(0f, Easing.Evaluate(ease, 0f), 1e-4f, ease.ToString());
                Assert.AreEqual(1f, Easing.Evaluate(ease, 1f), 1e-4f, ease.ToString());
            }
        }

        [Test]
        public void Easing_ClampsInput()
        {
            Assert.AreEqual(0f, Easing.Evaluate(Ease.Linear, -1f));
            Assert.AreEqual(1f, Easing.Evaluate(Ease.Linear, 2f));
        }

        [Test]
        public void Easing_CurvesHaveExpectedShape()
        {
            Assert.AreEqual(0.25f, Easing.Evaluate(Ease.InQuad, 0.5f), 1e-5f);
            Assert.AreEqual(0.75f, Easing.Evaluate(Ease.OutQuad, 0.5f), 1e-5f);
            Assert.AreEqual(0.5f, Easing.Evaluate(Ease.InOutQuad, 0.5f), 1e-5f);
            Assert.Greater(Easing.Evaluate(Ease.OutBack, 0.7f), 1f, "OutBack은 끝나기 전에 1을 넘었다 돌아옵니다.");
            Assert.Less(Easing.Evaluate(Ease.InBack, 0.2f), 0f, "InBack은 시작하자마자 0 아래로 내려갔다 갑니다.");
            Assert.Greater(Easing.Evaluate(Ease.OutElastic, 0.1f), 1f, "OutElastic은 1을 넘으며 진동합니다.");
        }

        #endregion

        #region 기본 동작

        [Test]
        public void DOMoveX_DoesNotMoveUntilUpdated_ThenReachesEnd()
        {
            var transform = CreateTransform();

            var tween = transform.DOMoveX(10f, 1f).SetEase(Ease.Linear);
            Assert.AreEqual(0f, transform.position.x);

            Step(0.5f);
            Assert.AreEqual(5f, transform.position.x, 1e-4f);

            Step(0.6f);
            Assert.AreEqual(10f, transform.position.x);
            Assert.IsFalse(tween.IsActive);
            Assert.AreEqual(0, Tween.ActiveCount);
        }

        [Test]
        public void AxisTween_KeepsOtherAxes()
        {
            var transform = CreateTransform();
            transform.position = new Vector3(1f, 2f, 3f);

            transform.DOMoveY(5f, 1f);
            Step(1f);

            Assert.AreEqual(new Vector3(1f, 5f, 3f), transform.position);
        }

        [Test]
        public void SetDelay_WaitsBeforeStarting()
        {
            var transform = CreateTransform();
            transform.DOMoveX(10f, 1f).SetEase(Ease.Linear).SetDelay(1f).OnStart(() => _log.Add("start"));

            Step(0.5f);
            Assert.AreEqual(0f, transform.position.x);
            Assert.AreEqual(0, _log.Count);

            Step(1f);
            Assert.AreEqual(5f, transform.position.x, 1e-4f);
            CollectionAssert.AreEqual(new[] { "start" }, _log);
        }

        [Test]
        public void SetRelative_AddsToStartValue()
        {
            var transform = CreateTransform();
            transform.position = new Vector3(3f, 0f, 0f);

            transform.DOMoveX(2f, 1f).SetRelative();
            Step(1f);

            Assert.AreEqual(5f, transform.position.x, 1e-5f);
        }

        [Test]
        public void From_JumpsToEndValueImmediately_ThenReturns()
        {
            var transform = CreateTransform();

            transform.DOMoveX(5f, 1f).From();
            Assert.AreEqual(5f, transform.position.x);

            Step(1f);
            Assert.AreEqual(0f, transform.position.x, 1e-5f);
        }

        [Test]
        public void FromValue_StartsFromGivenValue()
        {
            var transform = CreateTransform();

            transform.DOScale(1f, 1f).SetEase(Ease.Linear).From(Vector3.zero);
            Assert.AreEqual(Vector3.zero, transform.localScale);

            Step(0.5f);
            Assert.AreEqual(0.5f, transform.localScale.x, 1e-4f);
        }

        [Test]
        public void ZeroDuration_CompletesOnFirstUpdate()
        {
            var value = 0f;
            var tween = Tween.To(0f, 10f, 0f, v => value = v);

            Step(0f);

            Assert.AreEqual(10f, value);
            Assert.IsFalse(tween.IsActive);
        }

        [Test]
        public void ToInt_RoundsValues()
        {
            var score = 0;
            Tween.To(() => score, v => score = v, 100, 1f).SetEase(Ease.Linear);

            Step(0.5f);

            Assert.AreEqual(50, score);
        }

        [Test]
        public void SetUpdate_UsesUnscaledTime()
        {
            var scaled = CreateTransform();
            var unscaled = CreateTransform();
            scaled.DOMoveX(1f, 1f);
            unscaled.DOMoveX(1f, 1f).SetUpdate(true);

            TweenManager.Update(0f, 1f);

            Assert.AreEqual(0f, scaled.position.x);
            Assert.AreEqual(1f, unscaled.position.x);
        }

        [Test]
        public void DelayedCall_CallsOnceAfterDelay()
        {
            Tween.DelayedCall(1f, () => _log.Add("called"));

            Step(0.5f);
            Assert.AreEqual(0, _log.Count);

            Step(0.6f);
            Step(1f);
            CollectionAssert.AreEqual(new[] { "called" }, _log);
        }

        #endregion

        #region 반복

        [Test]
        public void Yoyo_GoesBackAndForth()
        {
            var transform = CreateTransform();
            var tween = transform.DOMoveX(10f, 1f).SetEase(Ease.Linear).SetLoops(2, LoopType.Yoyo);

            Step(1.25f);
            Assert.AreEqual(7.5f, transform.position.x, 1e-4f);

            Step(1f);
            Assert.AreEqual(0f, transform.position.x, 1e-5f);
            Assert.IsFalse(tween.IsActive);
        }

        [Test]
        public void Incremental_ContinuesFromLastEnd()
        {
            var transform = CreateTransform();
            transform.DOMoveX(10f, 1f).SetEase(Ease.Linear).SetLoops(3, LoopType.Incremental);

            Step(1.5f);
            Assert.AreEqual(15f, transform.position.x, 1e-4f);

            Step(2f);
            Assert.AreEqual(30f, transform.position.x, 1e-4f);
        }

        [Test]
        public void InfiniteLoops_RunUntilKilled()
        {
            var transform = CreateTransform();
            var tween = transform.DOMoveX(10f, 1f).SetEase(Ease.Linear).SetLoops(-1);

            Step(10.5f);
            Assert.IsTrue(tween.IsActive);
            Assert.AreEqual(5f, transform.position.x, 1e-3f);

            tween.Kill();
            Assert.IsFalse(tween.IsActive);
            Assert.AreEqual(0, Tween.ActiveCount);
        }

        [Test]
        public void Complete_OnInfiniteLoop_GoesToEndOfCurrentLoop()
        {
            var transform = CreateTransform();
            var tween = transform.DOMoveX(10f, 1f).SetLoops(-1).OnComplete(() => _log.Add("complete"));
            Step(0.5f);

            tween.Complete();

            Assert.AreEqual(10f, transform.position.x);
            Assert.IsFalse(tween.IsActive);
            CollectionAssert.AreEqual(new[] { "complete" }, _log);
        }

        [Test]
        public void Callbacks_FireInOrder()
        {
            var transform = CreateTransform();
            transform.DOMoveX(1f, 1f).SetLoops(2)
                .OnStart(() => _log.Add("start"))
                .OnStepComplete(() => _log.Add("step"))
                .OnComplete(() => _log.Add("complete"))
                .OnKill(() => _log.Add("kill"));

            Step(0.5f);
            Step(1f);
            Step(1f);

            CollectionAssert.AreEqual(new[] { "start", "step", "step", "complete", "kill" }, _log);
        }

        [Test]
        public void CallbackException_IsLoggedAndOtherCallbacksStillRun()
        {
            var transform = CreateTransform();
            transform.DOMoveX(1f, 1f)
                .OnComplete(() => throw new InvalidOperationException("boom"))
                .OnKill(() => _log.Add("kill"));

            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            Step(1f);

            Assert.AreEqual(1f, transform.position.x);
            CollectionAssert.AreEqual(new[] { "kill" }, _log);
        }

        #endregion

        #region 제어

        [Test]
        public void PauseAndPlay()
        {
            var transform = CreateTransform();
            var tween = transform.DOMoveX(10f, 1f).Pause();

            Step(1f);
            Assert.AreEqual(0f, transform.position.x);

            tween.Play();
            Step(1f);
            Assert.AreEqual(10f, transform.position.x);
        }

        [Test]
        public void KillWithComplete_JumpsToEndAndCallsOnComplete()
        {
            var transform = CreateTransform();
            var tween = transform.DOMoveX(10f, 1f).OnComplete(() => _log.Add("complete"));

            tween.Kill(true);

            Assert.AreEqual(10f, transform.position.x);
            Assert.IsFalse(tween.IsActive);
            CollectionAssert.AreEqual(new[] { "complete" }, _log);
        }

        [Test]
        public void DOKill_KillsTweensOfTarget()
        {
            var transform = CreateTransform();
            var other = CreateTransform();
            transform.DOMoveX(10f, 1f);
            transform.DOScale(2f, 1f);
            other.DOMoveX(10f, 1f);

            Assert.AreEqual(2, transform.DOKill());
            Assert.IsFalse(Tween.IsTweening(transform));
            Assert.IsTrue(Tween.IsTweening(other));
        }

        [Test]
        public void KillTarget_MatchesId()
        {
            var transform = CreateTransform();
            transform.DOMoveX(10f, 1f).SetId("intro");
            transform.DOMoveY(10f, 1f);

            Assert.AreEqual(1, Tween.KillTarget("intro"));
            Assert.AreEqual(1, Tween.ActiveCount);
        }

        [Test]
        public void TargetDestroyed_StopsSilently()
        {
            var transform = CreateTransform();
            var tween = transform.DOMoveX(10f, 1f);

            Object.DestroyImmediate(transform.gameObject);
            Step(0.5f);

            Assert.IsFalse(tween.IsActive);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SetAutoKillFalse_AllowsRestart()
        {
            var transform = CreateTransform();
            var tween = transform.DOMoveX(10f, 1f).SetEase(Ease.Linear).SetAutoKill(false);

            Step(1f);
            Assert.IsTrue(tween.IsActive);
            Assert.IsTrue(tween.IsComplete);

            tween.Restart();
            Assert.AreEqual(0f, transform.position.x);

            Step(0.5f);
            Assert.AreEqual(5f, transform.position.x, 1e-4f);
        }

        #endregion

        #region await

        [Test]
        public void Await_CompletesWhenTweenEnds()
        {
            var transform = CreateTransform();
            var awaiter = transform.DOMoveX(1f, 1f).GetAwaiter();
            Assert.IsFalse(awaiter.IsCompleted);

            Step(1f);

            Assert.IsTrue(awaiter.IsCompleted);
            awaiter.GetResult();
        }

        [Test]
        public void Await_CompletesWhenKilled()
        {
            var transform = CreateTransform();
            var tween = transform.DOMoveX(1f, 1f);
            var awaiter = tween.GetAwaiter();

            tween.Kill();

            Assert.IsTrue(awaiter.IsCompleted);
            awaiter.GetResult();
        }

        [Test]
        public void SetCancellation_KillsAndThrowsOnAwait()
        {
            using var cts = new CancellationTokenSource();
            var transform = CreateTransform();
            var tween = transform.DOMoveX(1f, 1f).SetCancellation(cts.Token);
            var awaiter = tween.GetAwaiter();

            cts.Cancel();
            Step(0.1f);

            Assert.IsFalse(tween.IsActive);
            Assert.AreEqual(0f, transform.position.x);
            Assert.IsTrue(awaiter.IsCompleted);
            Assert.Catch<OperationCanceledException>(() => awaiter.GetResult());
        }

        #endregion

        #region 시퀀스

        [Test]
        public void Sequence_AppendJoinAndCallback()
        {
            var transform = CreateTransform();
            var sequence = Sequence.Create()
                .Append(transform.DOMoveX(10f, 1f).SetEase(Ease.Linear))
                .Join(transform.DOScaleX(3f, 1f).SetEase(Ease.Linear))
                .Append(transform.DOMoveY(4f, 1f).SetEase(Ease.Linear))
                .AppendCallback(() => _log.Add("done"));
            Assert.AreEqual(2f, sequence.Duration);
            Assert.AreEqual(1, Tween.ActiveCount, "시퀀스 안의 트윈은 따로 세지 않습니다.");

            Step(0.5f);
            Assert.AreEqual(5f, transform.position.x, 1e-4f);
            Assert.AreEqual(2f, transform.localScale.x, 1e-4f);
            Assert.AreEqual(0f, transform.position.y);

            Step(1f);
            Assert.AreEqual(10f, transform.position.x);
            Assert.AreEqual(2f, transform.position.y, 1e-4f);
            Assert.AreEqual(0, _log.Count);

            Step(1f);
            Assert.AreEqual(4f, transform.position.y);
            CollectionAssert.AreEqual(new[] { "done" }, _log);
            Assert.IsFalse(sequence.IsActive);
        }

        [Test]
        public void Sequence_RestartLoop_ResetsValuesAndRefiresCallbacks()
        {
            var transform = CreateTransform();
            var calls = 0;
            Sequence.Create()
                .AppendCallback(() => calls++)
                .Append(transform.DOMoveX(10f, 1f).SetEase(Ease.Linear))
                .Append(transform.DOMoveX(20f, 1f).SetEase(Ease.Linear))
                .SetLoops(2);

            Step(0.5f);
            Assert.AreEqual(5f, transform.position.x, 1e-4f);
            Assert.AreEqual(1, calls);

            Step(1f);
            Assert.AreEqual(15f, transform.position.x, 1e-4f);

            Step(1f);
            Assert.AreEqual(5f, transform.position.x, 1e-4f, "두 번째 반복은 처음 위치에서 다시 시작합니다.");
            Assert.AreEqual(2, calls);

            Step(2f);
            Assert.AreEqual(20f, transform.position.x);
        }

        [Test]
        public void Sequence_Insert_PlacesAtPosition()
        {
            var transform = CreateTransform();
            var sequence = Sequence.Create()
                .Insert(1f, transform.DOMoveX(10f, 1f).SetEase(Ease.Linear))
                .InsertCallback(0.5f, () => _log.Add("half"));
            Assert.AreEqual(2f, sequence.Duration);

            Step(0.6f);
            CollectionAssert.AreEqual(new[] { "half" }, _log);
            Assert.AreEqual(0f, transform.position.x);

            Step(0.9f);
            Assert.AreEqual(5f, transform.position.x, 1e-4f);
        }

        [Test]
        public void Sequence_RejectsStartedOrInfiniteTween()
        {
            var transform = CreateTransform();
            var started = transform.DOMoveX(1f, 1f);
            Step(0.1f);

            Assert.Throws<InvalidOperationException>(() => Sequence.Create().Append(started));
            Assert.Throws<ArgumentException>(() => Sequence.Create().Append(transform.DOMoveY(1f, 1f).SetLoops(-1)));
        }

        [Test]
        public void NestedTween_CannotBeControlledDirectly()
        {
            var transform = CreateTransform();
            var child = transform.DOMoveX(1f, 1f);
            Sequence.Create().Append(child);

            LogAssert.Expect(LogType.Warning, new Regex("시퀀스"));
            child.Kill();

            Assert.IsTrue(child.IsActive);
        }

        #endregion

        #region 특수 트윈

        [Test]
        public void DOPunchPosition_ReturnsToStart()
        {
            var transform = CreateTransform();
            transform.localPosition = new Vector3(1f, 1f, 0f);
            transform.DOPunchPosition(Vector3.right, 1f).SetEase(Ease.Linear);

            Step(0.05f);
            Assert.Greater(transform.localPosition.x, 1.5f);

            Step(1f);
            Assert.AreEqual(new Vector3(1f, 1f, 0f), transform.localPosition);
        }

        [Test]
        public void DOShakePosition_StaysOnPlaneAndReturnsToStart()
        {
            var transform = CreateTransform();
            transform.localPosition = new Vector3(1f, 1f, 5f);
            transform.DOShakePosition(1f, 0.5f);

            Step(0.3f);
            Assert.AreNotEqual(new Vector3(1f, 1f, 5f), transform.localPosition);
            Assert.AreEqual(5f, transform.localPosition.z, "float 세기는 Z축을 흔들지 않습니다.");

            Step(1f);
            Assert.AreEqual(new Vector3(1f, 1f, 5f), transform.localPosition);
        }

        [Test]
        public void DOJump_ArcsAndLandsOnEnd()
        {
            var transform = CreateTransform();
            transform.DOJump(new Vector3(4f, 0f, 0f), 2f, 1, 1f);

            Step(0.5f);
            Assert.AreEqual(2f, transform.position.x, 1e-4f);
            Assert.AreEqual(2f, transform.position.y, 1e-4f);

            Step(0.5f);
            Assert.AreEqual(new Vector3(4f, 0f, 0f), transform.position);
        }

        [Test]
        public void DORotate_FastTakesShortestPath_BeyondSpinsFully()
        {
            var fast = CreateTransform();
            var beyond = CreateTransform();
            fast.DORotate(new Vector3(0f, 0f, 270f), 1f).SetEase(Ease.Linear);
            beyond.DORotate(new Vector3(0f, 0f, 360f), 1f, RotateMode.FastBeyond360).SetEase(Ease.Linear);

            Step(0.5f);

            Assert.AreEqual(-45f, Mathf.DeltaAngle(0f, fast.eulerAngles.z), 1e-2f);
            Assert.AreEqual(180f, beyond.eulerAngles.z, 1e-2f);
        }

        #endregion

        #region 컴포넌트

        [Test]
        public void UIAndRendererShortcuts_ChangeValues()
        {
            var go = CreateTransform().gameObject;
            var group = go.AddComponent<CanvasGroup>();
            var image = go.AddComponent<Image>();
            var spriteObject = CreateTransform().gameObject;
            var sprite = spriteObject.AddComponent<SpriteRenderer>();

            group.DOFade(0f, 1f);
            image.DOFade(0.5f, 1f);
            image.DOFillAmount(0.25f, 1f);
            sprite.DOColor(Color.red, 1f);
            Step(1f);

            Assert.AreEqual(0f, group.alpha);
            Assert.AreEqual(0.5f, image.color.a, 1e-5f);
            Assert.AreEqual(0.25f, image.fillAmount, 1e-5f);
            Assert.AreEqual(Color.red, sprite.color);
        }

        [Test]
        public void DOAnchorPos_MovesRectTransform()
        {
            var go = new GameObject("Rect", typeof(RectTransform));
            _objects.Add(go);
            var rect = (RectTransform)go.transform;

            rect.DOAnchorPosY(100f, 1f);
            Step(1f);

            Assert.AreEqual(new Vector2(0f, 100f), rect.anchoredPosition);
        }

        #endregion

        private Transform CreateTransform()
        {
            var go = new GameObject("Tween Target");
            _objects.Add(go);
            return go.transform;
        }

        private void Step(float seconds) => TweenManager.Update(seconds, seconds);
    }
}
