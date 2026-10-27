using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nyonyo.Framework.Tweening
{
    /// <summary>
    /// Punch, Shake, Jump처럼 시작 값과 끝 값 사이를 단순 보간하지 않는 트윈을 만듭니다.
    /// </summary>
    static class TweenShapes
    {
        /// <summary>시작 값에서 <paramref name="punch"/> 방향으로 튕겼다가 진동하며 시작 값으로 돌아옵니다.</summary>
        internal static Tweener<Vector3> Punch(object target, Func<Vector3> getter, Action<Vector3> setter,
            Vector3 punch, float duration, int vibrato, float elasticity)
        {
            vibrato = Mathf.Max(1, vibrato);
            elasticity = Mathf.Clamp01(elasticity);
            return new Tweener<Vector3>(target, getter, setter, Vector3.zero, duration,
                (start, _, t) => start + punch * PunchWave(t, vibrato, elasticity), null) { SupportsIncremental = false };
        }

        internal static Tweener<Vector2> Punch(object target, Func<Vector2> getter, Action<Vector2> setter,
            Vector2 punch, float duration, int vibrato, float elasticity)
        {
            vibrato = Mathf.Max(1, vibrato);
            elasticity = Mathf.Clamp01(elasticity);
            return new Tweener<Vector2>(target, getter, setter, Vector2.zero, duration,
                (start, _, t) => start + punch * PunchWave(t, vibrato, elasticity), null) { SupportsIncremental = false };
        }

        /// <summary>시작 값 주변을 무작위로 흔들다가 시작 값으로 돌아옵니다.</summary>
        internal static Tweener<Vector3> Shake(object target, Func<Vector3> getter, Action<Vector3> setter,
            Vector3 strength, float duration, int vibrato, float randomness, bool fadeOut)
        {
            var points = CreateShakePoints(strength, duration, vibrato, randomness, fadeOut);
            return new Tweener<Vector3>(target, getter, setter, Vector3.zero, duration,
                (start, _, t) => start + Sample(points, t), null) { SupportsIncremental = false };
        }

        internal static Tweener<Vector2> Shake(object target, Func<Vector2> getter, Action<Vector2> setter,
            Vector2 strength, float duration, int vibrato, float randomness, bool fadeOut)
        {
            var points = CreateShakePoints(strength, duration, vibrato, randomness, fadeOut);
            return new Tweener<Vector2>(target, getter, setter, Vector2.zero, duration,
                (start, _, t) => start + (Vector2)Sample(points, t), null) { SupportsIncremental = false };
        }

        /// <summary>끝 값으로 이동하면서 Y축으로 <paramref name="numJumps"/>번 뜁니다. 기본 곡선은 Linear입니다.</summary>
        internal static Tweener<Vector3> Jump(object target, Func<Vector3> getter, Action<Vector3> setter,
            Vector3 endValue, float jumpPower, int numJumps, float duration)
        {
            numJumps = Mathf.Max(1, numJumps);
            return new Tweener<Vector3>(target, getter, setter, endValue, duration,
                (start, end, t) => Vector3.LerpUnclamped(start, end, t) + Vector3.up * JumpHeight(t, jumpPower, numJumps),
                (a, b) => a + b).SetEase(Ease.Linear);
        }

        internal static Tweener<Vector2> Jump(object target, Func<Vector2> getter, Action<Vector2> setter,
            Vector2 endValue, float jumpPower, int numJumps, float duration)
        {
            numJumps = Mathf.Max(1, numJumps);
            return new Tweener<Vector2>(target, getter, setter, endValue, duration,
                (start, end, t) => Vector2.LerpUnclamped(start, end, t) + Vector2.up * JumpHeight(t, jumpPower, numJumps),
                (a, b) => a + b).SetEase(Ease.Linear);
        }

        // 양의 반파는 그대로, 음의 반파는 elasticity만큼 줄이고, 전체 크기는 끝으로 갈수록 0이 됩니다.
        private static float PunchWave(float t, int vibrato, float elasticity)
        {
            t = Mathf.Clamp01(t);
            var wave = Mathf.Sin(t * Mathf.PI * vibrato);
            if (wave < 0f)
                wave *= elasticity;
            return wave * (1f - t);
        }

        private static float JumpHeight(float t, float jumpPower, int numJumps)
        {
            var phase = t * numJumps;
            var local = phase - Mathf.Floor(phase);
            return 4f * jumpPower * local * (1f - local);
        }

        // 처음과 끝은 0이고, 그 사이 점들은 직전 방향의 반대쪽에서 randomness(도)만큼 틀어진 방향입니다.
        // XY는 평면 방향으로, Z는 부호를 번갈아 흔듭니다. 그래서 2D 회전(Z만)도 자연스럽게 흔들립니다.
        private static Vector3[] CreateShakePoints(Vector3 strength, float duration, int vibrato, float randomness, bool fadeOut)
        {
            var count = Mathf.Max(2, Mathf.RoundToInt(Mathf.Max(1, vibrato) * duration));
            randomness = Mathf.Clamp(randomness, 0f, 180f);
            var points = new Vector3[count + 1];
            var angle = Random.Range(0f, 360f);
            for (var i = 1; i < count; i++)
            {
                if (i > 1)
                    angle += 180f + Random.Range(-randomness, randomness);

                var decay = fadeOut ? 1f - (float)(i - 1) / (count - 1) : 1f;
                var radians = angle * Mathf.Deg2Rad;
                var z = (i % 2 == 0 ? -1f : 1f) * Random.Range(0.5f, 1f);
                points[i] = new Vector3(Mathf.Cos(radians) * strength.x, Mathf.Sin(radians) * strength.y, z * strength.z) * decay;
            }
            return points;
        }

        private static Vector3 Sample(Vector3[] points, float t)
        {
            var position = Mathf.Clamp01(t) * (points.Length - 1);
            var index = Mathf.Min((int)position, points.Length - 2);
            return Vector3.LerpUnclamped(points[index], points[index + 1], position - index);
        }
    }
}
