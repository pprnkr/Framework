using Nyonyo.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Nyonyo.Framework.Tests
{
    public class TestMappedUI : UIView
    {
        #region Auto Generated UI
        #pragma warning disable CS0169, CS0649
        [SerializeField] private Image IMG_Icon;
        [SerializeField] private Button BTN_Ok;
        [SerializeField] private RectTransform RT_Panel;
        [SerializeField] private GameObject GO_Root;
        [SerializeField] private TestChildUI UI_Child;
        #pragma warning restore CS0169, CS0649
        #endregion

        public Image Icon => IMG_Icon;

        public Button Ok => BTN_Ok;

        public RectTransform Panel => RT_Panel;

        public GameObject Root => GO_Root;

        public TestChildUI Child => UI_Child;
    }
}
