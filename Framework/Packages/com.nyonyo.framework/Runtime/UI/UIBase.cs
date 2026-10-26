using UnityEngine;
using UnityEngine.UI;

namespace Nyonyo.Framework.UI
{
    /// <summary>
    /// 모든 UI 컴포넌트의 기본 클래스입니다. 표시/숨김과 버튼 이벤트 처리를 담당합니다.
    /// </summary>
    /// <remarks>
    /// 팝업은 <see cref="PopupBase"/>를 상속하세요. 팝업 안에서 따로 동작하는 UI 묶음(탭, 목록 항목 등)은 이 클래스를 직접 상속합니다.
    /// 에디터에서는 인스펙터의 Generate Script, Assign Object 버튼으로 IMG_, TXT_ 같은 접두사가 붙은 자식을 필드에 자동으로 연결할 수 있습니다.
    /// 자식 오브젝트에 다른 UIBase가 있으면, 그 아래는 자식 UI가 관리합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public abstract class UIBase : MonoBehaviour
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

        /// <summary>
        /// 버튼 클릭을 한곳에서 처리합니다. Button의 OnClick에 이 함수를 연결하고, 인자로 그 Button을 넘기세요.
        /// Assign Object를 실행하면 BTN_ 버튼은 자동으로 연결됩니다.
        /// </summary>
        /// <example>
        /// <code>
        /// public override void OnButtonEvent(Button button)
        /// {
        ///     switch (button.name)
        ///     {
        ///         case "BTN_Ok": Confirm(); break;
        ///         default: base.OnButtonEvent(button); break;
        ///     }
        /// }
        /// </code>
        /// </example>
        public virtual void OnButtonEvent(Button button)
        {
        }
    }
}
