using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Nyonyo.Framework.UI
{
    /// <summary>
    /// 모든 팝업의 기본 클래스입니다. 팝업 층, 뒤로가기, 열기와 닫기를 담당합니다.
    /// 상속한 컴포넌트를 프리팹 루트에 붙이고, 클래스에 <see cref="PopupAttribute"/>로 프리팹 경로를 지정하세요.
    /// </summary>
    /// <remarks>
    /// 팝업은 <see cref="PopupManager.Open{T}"/>로 생성되고 <see cref="Close"/>로 즉시 Destroy됩니다. 숨겨 두고 다시 켜는 상태는 없습니다.
    /// 자식으로 둔 <see cref="UIView"/>도 팝업과 함께 파괴됩니다.
    /// 열고 닫을 때 할 일은 <see cref="PrevOpen"/>, <see cref="PostOpen"/>, <see cref="PrevClose"/>, <see cref="PostClose"/>를 재정의하세요.
    /// 팝업을 연 쪽에서 닫힌 뒤 할 일은 <see cref="OnClosed"/>로 등록하세요.
    /// <code>
    /// 열기: 생성 → PrevOpen → 열린 팝업 목록에 추가 → PostOpen
    /// 닫기: Close → PrevClose → 열린 팝업 목록에서 제거 → PostClose → OnClosed 콜백 → Destroy
    /// </code>
    /// 닫기 버튼은 OnClick에 <see cref="Close"/>를 직접 연결하거나, <see cref="UIBase.OnButtonClick"/>에서 처리하세요.
    /// </remarks>
    public abstract class PopupBase : UIBase
    {
        [Tooltip("팝업이 그려질 층입니다. Open에서 층을 직접 지정하면 그 값이 우선합니다.")]
        [SerializeField] private UILayer _layer = UILayer.Popup;

        [Tooltip("뒤로가기 키를 눌렀을 때의 동작입니다.")]
        [SerializeField] private BackKeyBehavior _backKey = BackKeyBehavior.Close;

        [Tooltip("씬이 바뀌면 자동으로 닫을지 여부입니다.")]
        [SerializeField] private bool _closeOnSceneChange = true;

        private readonly List<Action> _closedCallbacks = new();

        public UILayer Layer => _layer;

        public BackKeyBehavior BackKey => _backKey;

        public bool CloseOnSceneChange => _closeOnSceneChange;

        public bool IsClosed { get; private set; }

        /// <summary>
        /// 이 팝업을 띄운 <see cref="PopupManager"/>입니다.
        /// </summary>
        protected PopupManager Manager { get; private set; }

        internal void BeginOpen(PopupManager manager, UILayer layer)
        {
            Manager = manager;
            _layer = layer;
            PrevOpen();
        }

        internal void EndOpen() => PostOpen();

        /// <summary>
        /// 팝업이 닫힌 뒤 실행할 일을 등록합니다. 팝업을 연 쪽에서 씁니다.
        /// </summary>
        /// <remarks>
        /// <see cref="PostClose"/> 다음, Destroy 직전에 등록한 순서대로 실행됩니다. 여러 번 등록할 수 있습니다.
        /// 이미 닫힌 팝업에 등록하면 바로 실행합니다.
        /// </remarks>
        /// <example>
        /// <code>
        /// PopupManager.Instance.Open&lt;PopupShop&gt;().OnClosed(() => RefreshGold());
        /// </code>
        /// </example>
        public void OnClosed(Action onClosed)
        {
            if (onClosed == null)
                return;

            if (IsClosed)
                Run(onClosed);
            else
                _closedCallbacks.Add(onClosed);
        }

        /// <summary>
        /// 팝업이 생성되어 층에 놓인 직후, 열린 팝업 목록에 들어가기 전에 호출됩니다.
        /// 이 시점에는 <see cref="PopupManager.Find{T}"/>와 <see cref="PopupManager.GetOpenPopups"/>에 아직 나오지 않습니다.
        /// </summary>
        protected virtual void PrevOpen()
        {
        }

        /// <summary>
        /// 팝업이 열린 팝업 목록에 들어간 뒤 호출됩니다. 보통의 초기화 코드는 여기에 두세요.
        /// </summary>
        protected virtual void PostOpen()
        {
        }

        /// <summary>
        /// <see cref="Close"/>가 불리면 가장 먼저 호출됩니다. 이 시점에는 아직 열린 팝업 목록에 남아 있습니다.
        /// </summary>
        protected virtual void PrevClose()
        {
        }

        /// <summary>
        /// 열린 팝업 목록에서 빠진 뒤, <see cref="OnClosed"/>로 등록한 일과 Destroy보다 먼저 호출됩니다.
        /// </summary>
        protected virtual void PostClose()
        {
        }

        /// <summary>
        /// 팝업을 닫고 오브젝트를 즉시 Destroy합니다. 여러 번 호출해도 한 번만 닫힙니다.
        /// 닫을 때 할 일은 이 함수가 아니라 <see cref="PrevClose"/>나 <see cref="PostClose"/>를 재정의하세요.
        /// </summary>
        public void Close()
        {
            if (IsClosed)
                return;

            IsClosed = true;
            Run(PrevClose);
            Manager?.Remove(this);
            Run(PostClose);

            foreach (var callback in _closedCallbacks)
                Run(callback);
            _closedCallbacks.Clear();

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

        // 하나가 예외를 던져도 나머지 정리와 Destroy는 계속되도록 로그만 남깁니다.
        private void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
