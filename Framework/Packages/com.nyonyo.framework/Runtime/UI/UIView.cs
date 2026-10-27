namespace Nyonyo.Framework.UI
{
    /// <summary>
    /// 팝업 안에서 따로 동작하는 UI 묶음(탭, 목록 항목 등)의 기본 클래스입니다. 켜고 끄기(<see cref="Show"/>, <see cref="Hide"/>)를 담당합니다.
    /// </summary>
    /// <remarks>
    /// <see cref="Hide"/>는 오브젝트를 비활성화할 뿐 파괴하지 않으므로, <see cref="Show"/>로 다시 켤 수 있습니다.
    /// 팝업 자식으로 둔 UIView는 팝업이 닫힐 때 팝업과 함께 파괴됩니다. 이때 <see cref="OnHide"/>는 호출되지 않습니다.
    /// </remarks>
    public abstract class UIView : UIBase
    {
        public bool IsVisible => gameObject.activeSelf;

        /// <summary>
        /// 오브젝트를 활성화하고 <see cref="OnShow"/>를 호출합니다. 이미 보이는 상태면 아무 일도 하지 않습니다.
        /// </summary>
        public void Show()
        {
            if (IsVisible)
                return;

            gameObject.SetActive(true);
            OnShow();
        }

        /// <summary>
        /// 오브젝트를 비활성화하고 <see cref="OnHide"/>를 호출합니다. 이미 숨겨진 상태면 아무 일도 하지 않습니다.
        /// </summary>
        public void Hide()
        {
            if (!IsVisible)
                return;

            gameObject.SetActive(false);
            OnHide();
        }

        protected virtual void OnShow()
        {
        }

        protected virtual void OnHide()
        {
        }
    }
}
