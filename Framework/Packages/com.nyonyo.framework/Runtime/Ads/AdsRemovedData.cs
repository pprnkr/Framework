using System;

namespace Nyonyo.Framework.Ads
{
    /// <summary>
    /// 광고 제거 구매 여부를 저장하는 형식입니다. JsonUtility는 bool 하나를 바로 저장할 수 없어서 감쌉니다.
    /// </summary>
    [Serializable]
    internal sealed class AdsRemovedData
    {
        public bool removed;
    }
}
