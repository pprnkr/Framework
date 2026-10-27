using System;
using Nyonyo.Framework.Save;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Nyonyo.Framework.Ads
{
    /// <summary>
    /// 에디터에서 광고 흐름을 확인하는 가짜 광고입니다. 실제 광고 대신 화면에 "TEST" 배너와 전체 화면 창을 띄웁니다.
    /// </summary>
    /// <remarks>
    /// <para>전체 화면 창의 버튼으로 "보상 받고 닫기", "건너뛰기"를 골라 보상 처리를 확인할 수 있습니다. 창이 떠 있는 동안 아래 UI는 눌리지 않습니다.</para>
    /// <para><see cref="SimulateNoFill"/>을 켜면 광고가 없는 상황을 확인할 수 있습니다. 출시 빌드에서는 쓰지 마세요.</para>
    /// </remarks>
    public sealed class FakeAdService : AdServiceBase
    {
        // 실제 배너(320x50dp)와 비슷한 높이입니다.
        private const float BannerHeightDp = 50f;

        private readonly GameObject _root;
        private readonly GameObject _inputBlocker;
        private AwaitableCompletionSource<AdShowResult> _pending;
        private AdFormat _pendingFormat;
        private bool _bannerShown;

        /// <param name="parent">테스트 광고 화면을 붙일 부모입니다. 보통 <see cref="Bootstrapper.Root"/>를 넘깁니다.</param>
        public FakeAdService(Transform parent, AdPolicy policy = null, SaveService save = null)
            : this(parent, policy, save, null)
        {
        }

        // 테스트용입니다. 시계를 직접 넘기고, parent가 null이고 Play 모드가 아니면 화면을 만들지 않습니다.
        internal FakeAdService(Transform parent, AdPolicy policy, SaveService save, Func<double> clock)
            : base(policy, save, clock)
        {
            if (parent != null || Application.isPlaying)
            {
                _root = new GameObject("[Fake Ads]");
                if (parent != null)
                    _root.transform.SetParent(parent, false);
                else
                    Object.DontDestroyOnLoad(_root);

                _inputBlocker = CreateInputBlocker(_root.transform);
                _root.AddComponent<FakeAdView>().Service = this;
            }

            MarkInitialized();
        }

        /// <summary>true면 광고를 불러오지 못한 상황(no fill)을 흉내 냅니다. 모든 광고가 준비되지 않은 상태가 됩니다.</summary>
        public bool SimulateNoFill { get; set; }

        /// <summary>테스트 전체 화면 광고가 떠 있으면 true입니다.</summary>
        internal bool IsPresenting => _pending != null;

        internal AdFormat PresentingFormat => _pendingFormat;

        internal float BannerHeightPixels => Mathf.Round(BannerHeightDp * GetDensity());

        /// <summary>
        /// 떠 있는 테스트 광고를 닫습니다. 테스트 광고 화면의 버튼이 호출하고, 자동 테스트에서도 직접 호출합니다.
        /// </summary>
        /// <param name="rewarded">보상형 광고면 보상 조건을 채운 것으로 칩니다.</param>
        internal void Respond(bool rewarded)
        {
            if (_pending == null)
                return;

            var source = _pending;
            _pending = null;
            if (_inputBlocker != null)
                _inputBlocker.SetActive(false);
            source.SetResult(_pendingFormat == AdFormat.Rewarded && rewarded ? AdShowResult.Rewarded : AdShowResult.Closed);
        }

        /// <summary>앱이 백그라운드에서 돌아온 상황을 흉내 냅니다. 테스트에서 씁니다.</summary>
        internal void SimulateAppForeground() => OnAppForeground();

        public override void Dispose()
        {
            if (_pending != null)
            {
                var source = _pending;
                _pending = null;
                source.SetResult(AdShowResult.Failed);
            }

            if (_root != null)
                Object.Destroy(_root);
        }

        protected override bool IsLoaded(AdFormat format) => !SimulateNoFill;

        protected override Awaitable<AdShowResult> PresentAsync(AdFormat format)
        {
            Debug.Log($"[Nyonyo Framework] 테스트 광고({format})를 띄웁니다.");
            _pending = new AwaitableCompletionSource<AdShowResult>();
            _pendingFormat = format;
            if (_inputBlocker != null)
                _inputBlocker.SetActive(true);
            return _pending.Awaitable;
        }

        protected override void ShowBannerView(BannerPosition position) => _bannerShown = true;

        protected override void HideBannerView() => _bannerShown = false;

        protected override float GetBannerHeight() => _bannerShown && !SimulateNoFill ? BannerHeightPixels : 0f;

        private float GetDensity() => Screen.dpi > 0f ? Screen.dpi / 160f : 1f;

        // 테스트 광고 창은 IMGUI로 그리므로, 그 아래 UGUI가 눌리지 않게 맨 위에 투명한 막을 둡니다.
        private GameObject CreateInputBlocker(Transform parent)
        {
            var blocker = new GameObject("Input Blocker", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            blocker.transform.SetParent(parent, false);

            var canvas = blocker.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            blocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            blocker.SetActive(false);
            return blocker;
        }
    }
}
