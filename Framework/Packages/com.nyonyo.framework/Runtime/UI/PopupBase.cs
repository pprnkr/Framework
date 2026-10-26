using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Nyonyo.Framework.UI
{
    /// <summary>
    /// 모든 팝업의 기본 클래스입니다. 상속한 컴포넌트를 프리팹 루트에 붙이고, <see cref="UISettings"/>에 등록하세요.
    /// </summary>
    /// <remarks>
    /// 팝업 안의 모든 <see cref="Button"/> 클릭은 <see cref="OnButtonEvent"/> 하나로 모입니다.
    /// <see cref="Close"/>는 팝업 오브젝트를 Destroy하므로, 닫힌 팝업은 메모리에 남지 않습니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public abstract class PopupBase : MonoBehaviour
    {
        [Tooltip("팝업이 그려질 층입니다. Show에서 층을 직접 지정하면 그 값이 우선합니다.")]
        [SerializeField] UILayer _layer = UILayer.Popup;

        [Tooltip("뒤로가기 키를 눌렀을 때의 동작입니다.")]
        [SerializeField] BackKeyBehavior _backKey = BackKeyBehavior.Close;

        [Tooltip("씬이 바뀌면 자동으로 닫을지 여부입니다.")]
        [SerializeField] bool _closeOnSceneChange = true;

        [Tooltip("누르면 팝업을 닫는 버튼입니다. 비워 둬도 됩니다.")]
        [SerializeField] Button _closeButton;

        public UILayer Layer => _layer;

        public BackKeyBehavior BackKey => _backKey;

        public bool CloseOnSceneChange => _closeOnSceneChange;

        public bool IsClosed { get; private set; }

        /// <summary>
        /// 팝업이 닫힐 때, Destroy되기 직전에 호출됩니다.
        /// </summary>
        public event Action<PopupBase> Closed;

        /// <summary>
        /// 이 팝업을 띄운 <see cref="PopupManager"/>입니다.
        /// </summary>
        protected PopupManager Manager { get; private set; }

        internal void Open(PopupManager manager, UILayer layer)
        {
            Manager = manager;
            _layer = layer;

            foreach (var button in GetComponentsInChildren<Button>(true))
                button.onClick.AddListener(() => OnButtonEvent(button));

            OnOpen();
        }

        /// <summary>
        /// 팝업이 생성되고 층에 배치된 직후 호출됩니다. 초기화 코드를 여기에 두세요.
        /// </summary>
        protected virtual void OnOpen()
        {
        }

        /// <summary>
        /// 팝업 안의 버튼이 눌리면 호출됩니다. 기본 동작은 닫기 버튼이면 팝업을 닫는 것입니다.
        /// </summary>
        /// <remarks>
        /// 재정의할 때 처리하지 않은 버튼은 <c>base.OnButtonEvent(button)</c>으로 넘기세요.
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override void OnButtonEvent(Button button)
        /// {
        ///     switch (button.name)
        ///     {
        ///         case "OkButton": Confirm(); break;
        ///         default: base.OnButtonEvent(button); break;
        ///     }
        /// }
        /// </code>
        /// </example>
        protected virtual void OnButtonEvent(Button button)
        {
            if (button == _closeButton)
                Close();
        }

        /// <summary>
        /// 팝업을 닫고 오브젝트를 Destroy합니다. 여러 번 호출해도 한 번만 닫힙니다.
        /// </summary>
        /// <remarks>
        /// 재정의할 때는 마지막에 <c>base.Close()</c>를 호출해야 실제로 닫히고 메모리에서 해제됩니다.
        /// </remarks>
        public virtual void Close()
        {
            if (IsClosed)
                return;

            IsClosed = true;
            Manager?.Remove(this);

            try
            {
                Closed?.Invoke(this);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
        }

        /// <summary>
        /// 팝업이 닫힐 때까지 기다립니다. 확인창의 응답을 기다릴 때 씁니다.
        /// </summary>
        public async Awaitable WaitUntilClosedAsync(CancellationToken cancellationToken = default)
        {
            while (this != null && !IsClosed)
                await Awaitable.NextFrameAsync(cancellationToken);
        }
    }
}
