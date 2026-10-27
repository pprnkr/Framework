using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nyonyo.Framework.Tweening
{
    /// <summary>
    /// 동작 중인 루트 트윈 목록입니다. Play 모드에서 처음 트윈을 만들 때 <see cref="TweenRunner"/>를 만들어 매 프레임 돌립니다.
    /// </summary>
    // transform.DOMove()처럼 어디서든 바로 트윈을 만들 수 있으려면, 만든 트윈을 받아 돌릴 관리자가 하나 있어야 해서 static입니다.
    // 이 목록과 러너가 의도된 전역 상태이고, Domain Reload를 꺼도 안전하도록 SubsystemRegistration에서 초기화합니다.
    static class TweenManager
    {
        private static readonly List<Tween> s_tweens = new();
        private static readonly List<Tween> s_added = new();
        private static readonly Predicate<Tween> s_isFinished = tween => !tween.IsActive || tween.Parent != null;
        private static TweenRunner s_runner;
        private static bool s_quitting;

        internal static int ActiveCount
        {
            get
            {
                var count = 0;
                foreach (var tween in s_tweens)
                    if (IsRoot(tween))
                        count++;
                foreach (var tween in s_added)
                    if (IsRoot(tween))
                        count++;
                return count;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Clear();
            s_runner = null;
            s_quitting = false;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        internal static void Add(Tween tween)
        {
            s_added.Add(tween);
            EnsureRunner();
        }

        /// <summary>
        /// 시간을 흘려 트윈을 움직입니다. 매 프레임 자동으로 호출됩니다. 테스트에서는 직접 호출합니다.
        /// </summary>
        internal static void Update(float deltaTime, float unscaledDeltaTime)
        {
            // 콜백 안에서 새로 만든 트윈은 다음 프레임부터 움직입니다.
            s_tweens.AddRange(s_added);
            s_added.Clear();

            for (var i = 0; i < s_tweens.Count; i++)
            {
                var tween = s_tweens[i];
                if (!IsRoot(tween) || !tween.IsPlaying || tween.IsComplete)
                    continue;

                if (tween.CancellationToken.IsCancellationRequested)
                {
                    tween.KillInternal(true);
                    continue;
                }

                try
                {
                    tween.Goto(tween.Elapsed + (tween.UnscaledTime ? unscaledDeltaTime : deltaTime));
                }
                catch (Exception e)
                {
                    // 값을 바꾸다 예외가 나면(대상이 사라진 Tween.To 등) 그 트윈만 없앱니다.
                    Debug.LogException(e);
                    tween.KillInternal(false);
                    continue;
                }

                if (tween.IsComplete)
                    tween.OnRootComplete();
            }

            s_tweens.RemoveAll(s_isFinished);
        }

        internal static int Kill(object targetOrId, bool complete)
        {
            // Kill 중에 콜백이 새 트윈을 만들 수 있으므로 복사본을 돕니다.
            var count = 0;
            foreach (var tween in Snapshot())
            {
                if (!IsRoot(tween) || (targetOrId != null && !Matches(tween, targetOrId)))
                    continue;

                tween.Kill(complete);
                count++;
            }
            return count;
        }

        internal static bool IsTweening(object targetOrId)
        {
            foreach (var tween in s_tweens)
                if (IsRoot(tween) && !tween.IsComplete && Matches(tween, targetOrId))
                    return true;
            foreach (var tween in s_added)
                if (IsRoot(tween) && Matches(tween, targetOrId))
                    return true;
            return false;
        }

        /// <summary>모든 트윈을 콜백 없이 버립니다. 테스트와 Play 모드 시작 때 씁니다.</summary>
        internal static void Clear()
        {
            s_tweens.Clear();
            s_added.Clear();
        }

        private static Tween[] Snapshot()
        {
            var tweens = new Tween[s_tweens.Count + s_added.Count];
            s_tweens.CopyTo(tweens, 0);
            s_added.CopyTo(tweens, s_tweens.Count);
            return tweens;
        }

        private static bool IsRoot(Tween tween) => tween.IsActive && tween.Parent == null;

        private static bool Matches(Tween tween, object targetOrId) =>
            targetOrId != null && (Equals(tween.Target, targetOrId) || Equals(tween.Id, targetOrId));

        private static void EnsureRunner()
        {
            if (s_runner != null || s_quitting || !Application.isPlaying)
                return;

            var go = new GameObject("[Tween]");
            if (Bootstrapper.Root != null)
                go.transform.SetParent(Bootstrapper.Root, false);
            else
                Object.DontDestroyOnLoad(go);
            s_runner = go.AddComponent<TweenRunner>();
        }

        private static void OnQuitting() => s_quitting = true;
    }
}
