using System;
using Nyonyo.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Nyonyo.Dev
{
    /// <summary>
    /// 화면 위쪽에 잠깐 떴다가 사라지는 띠알람 샘플입니다. Notice 층에 떠서 팝업보다 항상 위에 보입니다.
    /// </summary>
    public class PopupNotice : PopupBase
    {
        [SerializeField] Text _message;
        [SerializeField] float _duration = 2f;

        public void SetMessage(string message) => _message.text = message;

        protected override async void OnOpen()
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(_duration, destroyCancellationToken);
                Close();
            }
            catch (OperationCanceledException)
            {
                // 시간이 되기 전에 다른 이유로 파괴된 경우입니다.
            }
        }
    }
}
