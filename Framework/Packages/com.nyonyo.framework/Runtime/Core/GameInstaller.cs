using UnityEngine;

namespace Nyonyo.Framework
{
    /// <summary>
    /// 게임별로 사용할 서비스를 등록합니다. 상속해서 에셋으로 만든 뒤 <see cref="FrameworkSettings"/>에 추가하세요.
    /// </summary>
    /// <remarks>
    /// MonoBehaviour 서비스가 필요하면 <see cref="Bootstrapper.Root"/>에 컴포넌트로 붙이세요.
    /// 이 오브젝트는 씬이 바뀌어도 유지됩니다.
    /// </remarks>
    public abstract class GameInstaller : ScriptableObject
    {
        public abstract void Install(ServiceRegistry services);
    }
}
