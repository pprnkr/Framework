namespace Nyonyo.Framework.Pooling
{
    /// <summary>
    /// 풀에서 꺼내거나 돌려놓을 때 알림을 받습니다. 풀링되는 프리팹의 컴포넌트(자식 포함)에 구현하세요.
    /// </summary>
    /// <remarks>
    /// 풀링된 오브젝트는 Destroy되지 않고 재사용되므로, 체력이나 속도 같은 상태는 <see cref="OnSpawned"/>에서 초기화하세요.
    /// </remarks>
    public interface IPoolable
    {
        /// <summary>풀에서 꺼내서 활성화한 직후 호출됩니다.</summary>
        void OnSpawned();

        /// <summary>풀에 돌려놓고 비활성화하기 직전에 호출됩니다.</summary>
        void OnDespawned();
    }
}
