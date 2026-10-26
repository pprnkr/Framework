namespace Nyonyo.Framework
{
    /// <summary>
    /// 등록된 서비스가 초기화 단계에서 동기 작업을 해야 할 때 구현합니다.
    /// </summary>
    public interface IInitializable
    {
        void Initialize();
    }
}
