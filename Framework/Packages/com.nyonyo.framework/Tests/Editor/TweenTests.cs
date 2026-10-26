using System;
using Nyonyo.Framework.Tweening;
using NUnit.Framework;
using UnityEngine;

namespace Nyonyo.Framework.Editor.Tests
{
    public class TweenTests
    {
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
        }

        [Test]
        public void To_WithZeroDuration_SetsFinalValueImmediately()
        {
            var value = 0f;

            var awaiter = Tween.To(0f, 10f, 0f, v => value = v).GetAwaiter();

            Assert.IsTrue(awaiter.IsCompleted);
            awaiter.GetResult();
            Assert.AreEqual(10f, value);
        }

        [Test]
        public void TransformTween_WithZeroDuration_AppliesTarget()
        {
            var go = new GameObject("Tween Target");
            try
            {
                var awaiter = go.transform.ScaleAsync(Vector3.one * 2f, 0f).GetAwaiter();

                Assert.IsTrue(awaiter.IsCompleted);
                Assert.AreEqual(Vector3.one * 2f, go.transform.localScale);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
