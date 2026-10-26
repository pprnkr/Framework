using System;

namespace Nyonyo.Framework.Audio
{
    /// <summary>
    /// <see cref="SoundManager"/>가 전역 저장 데이터로 보관하는 볼륨 값입니다.
    /// </summary>
    [Serializable]
    sealed class SoundVolumeData
    {
        public float master = 1f;
        public float bgm = 1f;
        public float sfx = 1f;
    }
}
