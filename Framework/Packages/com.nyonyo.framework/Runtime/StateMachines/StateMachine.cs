using System;
using System.Collections.Generic;

namespace Nyonyo.Framework.StateMachines
{
    /// <summary>
    /// 상태 머신입니다. 캐릭터, AI, 게임 흐름(타이틀 → 플레이 → 결과) 등에 씁니다. 상태 키는 보통 enum을 씁니다.
    /// </summary>
    /// <remarks>
    /// Unity 오브젝트가 아니므로 직접 <see cref="Update"/>를 불러 줘야 합니다(보통 MonoBehaviour의 Update에서).
    /// Enter나 Exit 안에서 <see cref="ChangeState"/>를 불러도 안전합니다. 진행 중인 전환이 끝난 뒤 이어서 바뀝니다.
    /// </remarks>
    /// <example>
    /// <code>
    /// enum PlayerState { Idle, Run, Jump }
    ///
    /// _fsm = new StateMachine&lt;PlayerState&gt;()
    ///     .Add(PlayerState.Idle, update: dt =&gt; { if (moving) _fsm.ChangeState(PlayerState.Run); })
    ///     .Add(PlayerState.Run, enter: () =&gt; _animator.Play("Run"))
    ///     .Add(PlayerState.Jump, new JumpState(this));
    /// _fsm.ChangeState(PlayerState.Idle);
    ///
    /// void Update() =&gt; _fsm.Update(Time.deltaTime);
    /// </code>
    /// </example>
    public sealed class StateMachine<TState>
    {
        sealed class StateCallbacks
        {
            public Action Enter;
            public Action<float> Update;
            public Action Exit;
        }

        readonly Dictionary<TState, StateCallbacks> _states = new();
        readonly Queue<TState> _pending = new();
        readonly EqualityComparer<TState> _comparer = EqualityComparer<TState>.Default;
        StateCallbacks _current;
        bool _changing;
        TState _lastRequested;

        public TState CurrentState { get; private set; }

        public TState PreviousState { get; private set; }

        /// <summary>한 번이라도 상태에 들어갔으면 true입니다.</summary>
        public bool HasState => _current != null;

        /// <summary>현재 상태에 들어온 뒤 <see cref="Update"/>로 흐른 시간입니다.</summary>
        public float TimeInState { get; private set; }

        /// <summary>상태가 바뀐 뒤(이전 상태, 새 상태) 호출됩니다.</summary>
        public event Action<TState, TState> StateChanged;

        /// <summary>
        /// 상태를 함수로 등록합니다. 필요한 함수만 넘기면 됩니다.
        /// </summary>
        public StateMachine<TState> Add(TState state, Action enter = null, Action<float> update = null, Action exit = null)
        {
            if (_states.ContainsKey(state))
                throw new ArgumentException($"{state} 상태가 이미 등록되어 있습니다.", nameof(state));

            _states.Add(state, new StateCallbacks { Enter = enter, Update = update, Exit = exit });
            return this;
        }

        /// <summary>
        /// 상태를 <see cref="IState"/> 클래스로 등록합니다.
        /// </summary>
        public StateMachine<TState> Add(TState state, IState handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return Add(state, handler.Enter, handler.Update, handler.Exit);
        }

        public bool Contains(TState state) => _states.ContainsKey(state);

        public bool IsIn(TState state) => HasState && _comparer.Equals(CurrentState, state);

        /// <summary>
        /// 상태를 바꿉니다. 이미 그 상태면 <paramref name="reenter"/>가 true일 때만 Exit, Enter를 다시 호출합니다.
        /// </summary>
        public void ChangeState(TState state, bool reenter = false)
        {
            if (!_states.ContainsKey(state))
                throw new ArgumentException($"{state} 상태가 등록되지 않았습니다.", nameof(state));

            // 전환 도중이면 마지막으로 요청된 상태와 비교해서, Enter 안에서 같은 상태를 다시 요청해도 반복되지 않게 합니다.
            var latest = _changing ? _lastRequested : CurrentState;
            if (!reenter && (HasState || _changing) && _comparer.Equals(latest, state))
                return;

            _lastRequested = state;
            _pending.Enqueue(state);
            if (_changing)
                return;

            _changing = true;
            try
            {
                while (_pending.Count > 0)
                    Transition(_pending.Dequeue());
            }
            finally
            {
                _changing = false;
                _pending.Clear();
            }
        }

        /// <summary>
        /// 현재 상태의 Update를 호출합니다.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (_current == null)
                return;

            TimeInState += deltaTime;
            _current.Update?.Invoke(deltaTime);
        }

        void Transition(TState next)
        {
            var hadState = HasState;
            var previous = CurrentState;

            _current?.Exit?.Invoke();

            PreviousState = previous;
            CurrentState = next;
            TimeInState = 0f;
            _current = _states[next];
            _current.Enter?.Invoke();

            StateChanged?.Invoke(hadState ? previous : default, next);
        }
    }
}
