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
        #region Auto Generated UI
        #pragma warning disable CS0169, CS0649
        [SerializeField] private RectTransform RT_Panel;
        [SerializeField] private Button BTN_Stack;
        [SerializeField] private Button BTN_Notice;
        [SerializeField] private Button BTN_Ok;
        [SerializeField] private Button BTN_Close;
        #pragma warning restore CS0169, CS0649
        #endregion

        protected override void OnOpen()
        {
            var index = Manager.GetOpenPopups(Layer).Count - 1;
            RT_Panel.anchoredPosition += new Vector2(40f, -40f) * index;
        }

        public override void OnButtonEvent(Button button)
        {
            switch (button.name)
            {
                case "BTN_Stack":
                    PopupManager.Instance.Show<PopupTest>();
                    break;
                case "BTN_Notice":
                    PopupManager.Instance.Show<PopupNotice>().SetMessage("Notice is drawn above every popup.");
                    break;
                case "BTN_Ok":
                    Debug.Log("[Dev] PopupTest OK");
                    Close();
                    break;
                case "BTN_Close":
                    Close();
                    break;
                default:
                    base.OnButtonEvent(button);
                    break;
            }
        }
    }
}
