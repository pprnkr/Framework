namespace Nyonyo.Framework.StateMachines
{
    /// <summary>
    /// 클래스로 만드는 상태입니다. 상태마다 필드나 로직이 많을 때 씁니다. 간단한 상태는 <see cref="StateMachine{TState}.Add(TState, System.Action, System.Action{float}, System.Action)"/>에 함수를 넘기세요.
    /// </summary>
    public interface IState
    {
        void Enter();

        void Update(float deltaTime);

        void Exit();
    }
}
