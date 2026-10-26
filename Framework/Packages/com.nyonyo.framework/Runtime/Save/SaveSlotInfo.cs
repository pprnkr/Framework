using System;

namespace Nyonyo.Framework.Save
{
    /// <summary>
    /// 세이브 슬롯 요약입니다. 슬롯 선택 화면에 표시할 때 씁니다.
    /// </summary>
    public readonly struct SaveSlotInfo
    {
        public SaveSlotInfo(int slot, DateTime lastSavedUtc, int keyCount)
        {
            Slot = slot;
            LastSavedUtc = lastSavedUtc;
            KeyCount = keyCount;
        }

        public int Slot { get; }

        /// <summary>마지막으로 저장한 시각(UTC)입니다. 화면에는 <c>ToLocalTime()</c>으로 바꿔서 표시하세요.</summary>
        public DateTime LastSavedUtc { get; }

        /// <summary>슬롯에 저장된 데이터 키 수입니다.</summary>
        public int KeyCount { get; }
    }
}
