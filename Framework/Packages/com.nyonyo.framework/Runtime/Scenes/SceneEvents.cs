using Nyonyo.Framework.Events;

namespace Nyonyo.Framework.Scenes
{
    /// <summary>
    /// 씬 전환을 시작할 때, 화면을 가리기 전에 발행됩니다.
    /// </summary>
    public readonly struct SceneLoadStarted : IEvent
    {
        public readonly string SceneName;

        public SceneLoadStarted(string sceneName) => SceneName = sceneName;
    }

    /// <summary>
    /// 새 씬을 다 불러온 뒤, 화면을 다시 보여 주기 전에 발행됩니다.
    /// 이 시점에는 새 씬 오브젝트의 <c>Awake</c>와 <c>OnEnable</c>이 이미 호출된 상태입니다.
    /// </summary>
    public readonly struct SceneLoaded : IEvent
    {
        public readonly string SceneName;

        public SceneLoaded(string sceneName) => SceneName = sceneName;
    }
}
