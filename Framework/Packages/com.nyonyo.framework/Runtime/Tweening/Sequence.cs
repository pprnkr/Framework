using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nyonyo.Framework.Tweening
{
    /// <summary>
    /// 여러 트윈과 콜백을 시간 순서대로 묶어 하나처럼 재생합니다. DOTween의 Sequence와 같습니다.
    /// </summary>
    /// <remarks>
    /// <para>트윈은 만든 직후(아직 시작하기 전)에 넣어야 합니다. 시퀀스가 시작된 뒤에는 더 넣을 수 없습니다.</para>
    /// <para>시퀀스에 넣은 트윈은 시퀀스가 움직이므로 직접 Kill하거나 일시정지할 수 없습니다. 무한 반복 트윈은 넣을 수 없습니다.</para>
    /// <para>시퀀스의 기본 곡선은 <see cref="Ease.Linear"/>입니다. 곡선은 시퀀스 전체 시간에 적용됩니다.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Sequence.Create()
    ///     .Append(panel.DOScale(1f, 0.3f).From(Vector3.zero))
    ///     .Join(group.DOFade(1f, 0.3f))
    ///     .AppendInterval(1f)
    ///     .AppendCallback(() =&gt; Debug.Log("끝"))
    ///     .SetUpdate(true);
    /// </code>
    /// </example>
    public sealed class Sequence : Tween
    {
        private readonly List<Entry> _entries = new();
        private float _lastPosition;
        private float _lastTime;
        private int _lastLoop;

        private Sequence() : base(null, 0f)
        {
            Ease = Ease.Linear;
        }

        // 생성자를 막고 이 함수로만 만들게 해서, 만들자마자 TweenManager에 등록되도록 합니다.
        public static Sequence Create() => new();

        /// <summary>지금까지 넣은 것들이 모두 끝난 뒤에 재생합니다.</summary>
        public Sequence Append(Tween tween) => Add(_duration, tween);

        /// <summary>바로 앞에 넣은 트윈(또는 간격, 콜백)과 동시에 재생합니다.</summary>
        public Sequence Join(Tween tween) => Add(_lastPosition, tween);

        /// <summary>시퀀스 시작부터 <paramref name="position"/>초 지점에 넣습니다.</summary>
        public Sequence Insert(float position, Tween tween) => Add(Mathf.Max(0f, position), tween);

        /// <summary>끝에 빈 시간을 넣습니다.</summary>
        public Sequence AppendInterval(float seconds)
        {
            RequireEditable();
            _lastPosition = _duration;
            _duration += Mathf.Max(0f, seconds);
            return this;
        }

        /// <summary>지금까지 넣은 것들이 모두 끝난 시점에 <paramref name="callback"/>을 호출합니다.</summary>
        public Sequence AppendCallback(Action callback) => InsertCallback(_duration, callback);

        /// <summary>시퀀스 시작부터 <paramref name="position"/>초 지점에 <paramref name="callback"/>을 호출합니다.</summary>
        public Sequence InsertCallback(float position, Action callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            RequireEditable();
            position = Mathf.Max(0f, position);
            _entries.Add(new Entry(position, null, callback));
            _lastPosition = position;
            _duration = Mathf.Max(_duration, position);
            return this;
        }

        private protected override void OnStartup()
        {
        }

        private protected override void Apply(int loop, float progress)
        {
            var time = progress >= 1f ? _duration : progress * _duration;

            bool backward;
            if (loop > _lastLoop && LoopType != LoopType.Yoyo)
            {
                // Restart 반복입니다. 처음으로 되감은 뒤 다시 재생합니다.
                Seek(0f, true);
                backward = false;
            }
            else
            {
                backward = loop < _lastLoop || time < _lastTime;
            }

            Seek(time, backward);
            _lastLoop = loop;
            _lastTime = time;
        }

        private protected override void OnKilled()
        {
            foreach (var entry in _entries)
                entry.Tween?.KillInternal(false);
        }

        // 되감을 때는 뒤에서부터 처리해야 같은 값을 바꾸는 트윈들 중 앞의 것의 시작 값이 남습니다.
        private void Seek(float time, bool backward)
        {
            if (backward)
            {
                for (var i = _entries.Count - 1; i >= 0; i--)
                    Process(_entries[i], time, true);
            }
            else
            {
                for (var i = 0; i < _entries.Count; i++)
                    Process(_entries[i], time, false);
            }
        }

        private void Process(Entry entry, float time, bool backward)
        {
            if (entry.Tween != null)
            {
                entry.Tween.Goto(time - entry.Position);
                return;
            }

            if (backward)
            {
                if (time < entry.Position || time <= 0f)
                    entry.Fired = false;
            }
            else if (!entry.Fired && time >= entry.Position)
            {
                entry.Fired = true;
                Invoke(entry.Callback);
            }
        }

        private Sequence Add(float position, Tween tween)
        {
            if (tween == null)
                throw new ArgumentNullException(nameof(tween));

            RequireEditable();
            if (tween == this)
                throw new ArgumentException("시퀀스를 자기 자신에 넣을 수 없습니다.", nameof(tween));
            if (tween.Parent != null)
                throw new InvalidOperationException("이미 다른 시퀀스에 들어 있는 트윈입니다. 트윈 하나는 시퀀스 하나에만 넣을 수 있습니다.");
            if (!tween.IsActive)
                throw new InvalidOperationException("Kill된 트윈은 시퀀스에 넣을 수 없습니다. 새로 만들어 넣으세요.");
            if (tween.HasStarted)
                throw new InvalidOperationException("이미 시작된 트윈은 시퀀스에 넣을 수 없습니다. 트윈을 만든 직후에 넣으세요.");
            if (tween.Loops < 0)
                throw new ArgumentException("무한 반복 트윈은 시퀀스에 넣을 수 없습니다. 시퀀스 자체에 SetLoops(-1)을 쓰세요.", nameof(tween));

            tween.Parent = this;
            _entries.Add(new Entry(position, tween, null));
            _lastPosition = position;
            _duration = Mathf.Max(_duration, position + tween.FullDuration);
            return this;
        }

        private void RequireEditable()
        {
            if (HasStarted || !IsActive)
                throw new InvalidOperationException("이미 시작되었거나 Kill된 시퀀스는 바꿀 수 없습니다. 시퀀스를 만든 직후에 트윈을 넣으세요.");
        }

        private sealed class Entry
        {
            public readonly float Position;
            public readonly Tween Tween;
            public readonly Action Callback;
            public bool Fired;

            public Entry(float position, Tween tween, Action callback)
            {
                Position = position;
                Tween = tween;
                Callback = callback;
            }
        }
    }
}
