using Nyonyo.Framework.Assets;
using Nyonyo.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Nyonyo.Dev
{
    /// <summary>
    /// 팝업 동작 확인용 샘플입니다. 같은 층에 여러 개를 띄우면 나중 것이 위에 겹쳐 보이도록 조금씩 비켜서 놓습니다.
    /// </summary>
    [Popup(Path = "Assets/Dev/Prefabs/PopupTest.prefab", LoadType = LoadType.Addressables, Preload = true)]
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

        protected override void PostOpen()
        {
            var index = Manager.GetOpenPopups(Layer).Count - 1;
            RT_Panel.anchoredPosition += new Vector2(40f, -40f) * index;
        }

        protected override void OnButtonClick(Button button)
        {
            if (button == BTN_Stack)
            {
                PopupManager.Instance.Open<PopupTest>().OnClosed(() => Debug.Log("[Dev] Stacked PopupTest closed"));
            }
            else if (button == BTN_Notice)
            {
                PopupManager.Instance.Open<PopupNotice>().SetMessage("Notice is drawn above every popup.");
            }
            else if (button == BTN_Ok)
            {
                Debug.Log("[Dev] PopupTest OK");
                Close();
            }
            else if (button == BTN_Close)
            {
                Close();
            }
            else
            {
                base.OnButtonClick(button);
            }
        }
    }
}
