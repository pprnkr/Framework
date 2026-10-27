using UnityEngine;
#if NYONYO_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Nyonyo.Framework.UI
{
    /// <summary>
    /// 매 프레임 뒤로가기 키를 확인해서 <see cref="PopupManager.HandleBack"/>를 호출합니다.
    /// Input System에서는 안드로이드 뒤로가기 버튼도 ESC 키로 들어옵니다.
    /// </summary>
    sealed class PopupBackKeyHandler : MonoBehaviour
    {
        internal PopupManager Manager;

        private void Update()
        {
            if (Manager != null && WasBackPressed())
                Manager.HandleBack();
        }

        private bool WasBackPressed()
        {
#if NYONYO_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }
    }
}
