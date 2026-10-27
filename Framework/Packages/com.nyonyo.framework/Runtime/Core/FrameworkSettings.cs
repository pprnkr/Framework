using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nyonyo.Framework
{
    /// <summary>
    /// 부트스트랩 설정입니다. <c>Resources/NyonyoFrameworkSettings</c> 경로에 있어야 합니다.
    /// 메뉴 Tools > Nyonyo Framework > Settings > Framework Settings로 만들 수 있습니다.
    /// </summary>
    public sealed class FrameworkSettings : ScriptableObject
    {
        [Tooltip("위에서부터 순서대로 실행됩니다. 서비스 초기화 순서도 이 순서를 따릅니다.")]
        [SerializeField] private GameInstaller[] _installers = Array.Empty<GameInstaller>();

        public IReadOnlyList<GameInstaller> Installers => _installers;
    }
}
