using System;

namespace Nyonyo.Framework
{
    /// <summary>
    /// 게임 어디서든 전역 서비스를 꺼내 쓰는 진입점입니다.
    /// </summary>
    /// <remarks>
    /// 게임이 종료되거나 Play 모드를 나갈 때 서비스가 먼저 정리됩니다.
    /// 그래서 <c>OnDestroy</c>나 <c>OnDisable</c>에서는 <see cref="Get{T}"/> 대신 <see cref="TryGet{T}"/>를 쓰세요.
    /// </remarks>
    // 확정된 결정(서비스 로케이터)에 따른 유일한 전역 진입점이라 static입니다. 다른 전역 상태는 서비스 인스턴스에 둡니다.
    public static class Services
    {
        private static ServiceRegistry s_registry;

        public static T Get<T>() where T : class => Registry.Get<T>();

        public static bool TryGet<T>(out T service) where T : class
        {
            if (s_registry != null)
                return s_registry.TryGet(out service);

            service = null;
            return false;
        }

        private static ServiceRegistry Registry =>
            s_registry ?? throw new InvalidOperationException(
                "서비스 레지스트리가 없습니다. 게임이 실행 중이고 Bootstrapper가 실행됐는지 확인하세요.");

        internal static void SetRegistry(ServiceRegistry registry) => s_registry = registry;
    }
}
