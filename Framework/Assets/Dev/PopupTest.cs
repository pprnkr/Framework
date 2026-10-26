using Nyonyo.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Nyonyo.Dev
{
    /// <summary>
    /// 팝업 동작 확인용 샘플입니다. 같은 층에 여러 개를 띄우면 나중 것이 위에 겹쳐 보이도록 조금씩 비켜서 놓습니다.
    /// </summary>
    public class PopupTest : PopupBase
    {
        [SerializeField] RectTransform _panel;

        protected override void OnOpen()
        {
            var index = Manager.GetOpenPopups(Layer).Count - 1;
            _panel.anchoredPosition += new Vector2(40f, -40f) * index;
        }

        protected override void OnButtonEvent(Button button)
        {
            switch (button.name)
            {
                case "StackButton":
                    PopupManager.Instance.Show<PopupTest>();
                    break;
                case "NoticeButton":
                    PopupManager.Instance.Show<PopupNotice>().SetMessage("Notice is drawn above every popup.");
                    break;
                case "OkButton":
                    Debug.Log("[Dev] PopupTest OK");
                    Close();
                    break;
                default:
                    base.OnButtonEvent(button);
                    break;
            }
        }
    }
}
