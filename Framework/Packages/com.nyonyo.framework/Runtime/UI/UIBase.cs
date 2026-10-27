using Nyonyo.Framework.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Nyonyo.Framework.UI
{
    /// <summary>
    /// 팝업(<see cref="PopupBase"/>)과 자식 UI(<see cref="UIView"/>)의 공통 기본 클래스입니다. 버튼 이벤트 처리와 에디터 자동 매핑을 담당합니다.
    /// </summary>
    /// <remarks>
    /// 이 클래스를 직접 상속하지 말고, 팝업은 <see cref="PopupBase"/>, 팝업 안의 UI 묶음(탭, 목록 항목 등)은 <see cref="UIView"/>를 상속하세요.
    /// 에디터에서는 인스펙터의 Generate Script, Assign Object 버튼으로 IMG_, TXT_ 같은 접두사가 붙은 자식을 필드에 자동으로 연결할 수 있습니다.
    /// 자식 오브젝트에 다른 UIBase가 있으면, 그 아래는 자식 UI가 관리합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public abstract class UIBase : MonoBehaviour
    {
        /// <summary>
        /// 버튼 클릭 효과음 이름입니다. 효과음 경로 형식(기본 SFX/{0})으로 찾으므로, 기본 설정이면 SFX/Click을 재생합니다.
        /// </summary>
        public const string ButtonClickSound = "Click";

        /// <summary>
        /// 버튼 클릭 진입점입니다. Button의 OnClick에 이 함수를 연결하고, 인자로 그 Button을 넘기세요.
        /// Assign Object를 실행하면 BTN_ 버튼은 자동으로 연결됩니다.
        /// 클릭 효과음(<see cref="PlayButtonSound"/>)을 재생한 뒤 <see cref="OnButtonClick"/>을 호출합니다.
        /// 클릭 처리는 이 함수가 아니라 <see cref="OnButtonClick"/>을 재정의하세요.
        /// </summary>
        public void OnButtonEvent(Button button)
        {
            PlayButtonSound(button);
            OnButtonClick(button);
        }

        /// <summary>
        /// 버튼 클릭을 한곳에서 처리합니다. 버튼 이름이 아니라 필드와 비교하세요. 이름이 같은 버튼이 있어도 헷갈리지 않습니다.
        /// </summary>
        /// <example>
        /// <code>
        /// protected override void OnButtonClick(Button button)
        /// {
        ///     if (button == BTN_Ok)
        ///     {
        ///         Confirm();
        ///     }
        ///     else
        ///     {
        ///         base.OnButtonClick(button);
        ///     }
        /// }
        /// </code>
        /// </example>
        protected virtual void OnButtonClick(Button button)
        {
        }

        /// <summary>
        /// 버튼 클릭 효과음(<see cref="ButtonClickSound"/>)을 재생합니다. <see cref="SoundManager"/>가 등록되어 있지 않으면 재생하지 않습니다.
        /// 특정 UI나 버튼만 다른 소리를 내거나 소리를 끄려면 재정의하세요.
        /// </summary>
        protected virtual void PlayButtonSound(Button button)
        {
            if (Services.TryGet<SoundManager>(out var sound))
                sound.PlaySFX(ButtonClickSound);
        }
    }
}
