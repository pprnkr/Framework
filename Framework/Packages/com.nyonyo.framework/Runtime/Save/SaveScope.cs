namespace Nyonyo.Framework.Save
{
    /// <summary>
    /// 저장 데이터가 속하는 범위입니다.
    /// </summary>
    public enum SaveScope
    {
        /// <summary>현재 세이브 슬롯(<see cref="SaveService.CurrentSlot"/>)에 저장합니다. 진행 데이터용입니다.</summary>
        Slot,

        /// <summary>슬롯과 상관없이 하나만 저장합니다. 볼륨, 언어 같은 설정용입니다.</summary>
        Global,
    }
}
