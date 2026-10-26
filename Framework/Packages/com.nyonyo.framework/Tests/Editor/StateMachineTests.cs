using System;
using System.Collections.Generic;
using Nyonyo.Framework.StateMachines;
using NUnit.Framework;

namespace Nyonyo.Framework.Editor.Tests
{
    public class StateMachineTests
    {
        enum State
        {
            Idle,
            Run,
            Jump,
        }

        class RecordingState : IState
        {
            readonly List<string> _log;
            readonly string _name;

            public RecordingState(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public void Enter() => _log.Add($"enter:{_name}");

            public void Update(float deltaTime) => _log.Add($"update:{_name}");

            public void Exit() => _log.Add($"exit:{_name}");
        }

        List<string> _log;
        StateMachine<State> _fsm;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _fsm = new StateMachine<State>()
                .Add(State.Idle, () => _log.Add("enter:Idle"), _ => _log.Add("update:Idle"), () => _log.Add("exit:Idle"))
                .Add(State.Run, new RecordingState("Run", _log))
                .Add(State.Jump, () => _log.Add("enter:Jump"));
        }

        [Test]
        public void ChangeState_CallsExitThenEnter()
        {
            _fsm.ChangeState(State.Idle);
            _fsm.ChangeState(State.Run);

            CollectionAssert.AreEqual(new[] { "enter:Idle", "exit:Idle", "enter:Run" }, _log);
            Assert.AreEqual(State.Run, _fsm.CurrentState);
            Assert.AreEqual(State.Idle, _fsm.PreviousState);
        }

        [Test]
        public void Update_CallsCurrentStateAndTracksTime()
        {
            _fsm.ChangeState(State.Run);
            _fsm.Update(0.5f);
            _fsm.Update(0.25f);

            CollectionAssert.AreEqual(new[] { "enter:Run", "update:Run", "update:Run" }, _log);
            Assert.AreEqual(0.75f, _fsm.TimeInState, 1e-5f);
        }

        [Test]
        public void Update_WithoutState_DoesNothing()
        {
            Assert.DoesNotThrow(() => _fsm.Update(1f));
            Assert.IsFalse(_fsm.HasState);
        }

        [Test]
        public void ChangeState_ToSameState_IsIgnoredUnlessReenter()
        {
            _fsm.ChangeState(State.Idle);
            _fsm.ChangeState(State.Idle);
            _fsm.ChangeState(State.Idle, reenter: true);

            CollectionAssert.AreEqual(new[] { "enter:Idle", "exit:Idle", "enter:Idle" }, _log);
        }

        [Test]
        public void ChangeState_InsideEnter_RunsAfterCurrentTransition()
        {
            var fsm = new StateMachine<State>();
            fsm.Add(State.Idle, () =>
                {
                    _log.Add("enter:Idle");
                    fsm.ChangeState(State.Jump);
                    fsm.ChangeState(State.Jump);
                }, exit: () => _log.Add("exit:Idle"))
                .Add(State.Jump, () => _log.Add("enter:Jump"));

            fsm.ChangeState(State.Idle);

            CollectionAssert.AreEqual(new[] { "enter:Idle", "exit:Idle", "enter:Jump" }, _log);
            Assert.AreEqual(State.Jump, fsm.CurrentState);
        }

        [Test]
        public void StateChanged_ReportsPreviousAndNext()
        {
            var changes = new List<string>();
            _fsm.StateChanged += (from, to) => changes.Add($"{from}->{to}");

            _fsm.ChangeState(State.Run);
            _fsm.ChangeState(State.Jump);

            CollectionAssert.AreEqual(new[] { "Idle->Run", "Run->Jump" }, changes);
        }

        [Test]
        public void UnknownOrDuplicateState_Throws()
        {
            var fsm = new StateMachine<State>().Add(State.Idle);

            Assert.Throws<ArgumentException>(() => fsm.ChangeState(State.Run));
            Assert.Throws<ArgumentException>(() => fsm.Add(State.Idle));
        }
    }
}
