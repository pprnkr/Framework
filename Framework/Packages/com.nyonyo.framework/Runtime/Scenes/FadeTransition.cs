using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Nyonyo.Framework.Scenes
{
    /// <summary>
    /// 화면을 단색으로 페이드 아웃/인하는 전환 연출입니다. 필요한 오브젝트는 코드로 만들기 때문에 에셋이 필요 없습니다.
    /// </summary>
    /// <remarks>
    /// 화면을 가리는 동안에는 UI 클릭을 막습니다.
    /// <see cref="Time.timeScale"/>의 영향을 받지 않으므로, 일시정지 중에도 동작합니다.
    /// </remarks>
    public sealed class FadeTransition : ISceneTransition
    {
        private readonly Canvas _canvas;
        private readonly CanvasGroup _canvasGroup;
        private readonly float _duration;

        /// <param name="parent">페이드 오브젝트를 붙일 부모입니다. 보통 <see cref="Bootstrapper.Root"/>를 넘깁니다. null이면 DontDestroyOnLoad 오브젝트로 만듭니다.</param>
        /// <param name="color">화면을 가릴 색입니다. null이면 검은색입니다.</param>
        /// <param name="duration">페이드 아웃과 페이드 인에 각각 걸리는 시간(초)입니다.</param>
        /// <param name="sortingOrder">다른 UI보다 위에 그려지도록 하는 Canvas 정렬 순서입니다.</param>
        public FadeTransition(Transform parent, Color? color = null, float duration = 0.3f, int sortingOrder = 30000)
        {
            _duration = duration;

            var root = new GameObject("Scene Fade", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
            if (parent != null)
                root.transform.SetParent(parent, false);
            else
                Object.DontDestroyOnLoad(root);

            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = sortingOrder;
            _canvas.enabled = false;

            _canvasGroup = root.GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;

            var image = new GameObject("Image", typeof(RectTransform), typeof(Image));
            image.transform.SetParent(root.transform, false);
            var rect = (RectTransform)image.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            image.GetComponent<Image>().color = color ?? Color.black;
        }

        public async Awaitable ShowAsync(CancellationToken cancellationToken)
        {
            _canvas.enabled = true;
            _canvasGroup.blocksRaycasts = true;
            await FadeAsync(1f, cancellationToken);
        }

        public void SetProgress(float progress)
        {
        }

        public async Awaitable HideAsync(CancellationToken cancellationToken)
        {
            await FadeAsync(0f, cancellationToken);
            _canvasGroup.blocksRaycasts = false;
            _canvas.enabled = false;
        }

        private async Awaitable FadeAsync(float targetAlpha, CancellationToken cancellationToken)
        {
            var startAlpha = _canvasGroup.alpha;
            for (var elapsed = 0f; elapsed < _duration; elapsed += Time.unscaledDeltaTime)
            {
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / _duration);
                await Awaitable.NextFrameAsync(cancellationToken);
            }

            _canvasGroup.alpha = targetAlpha;
        }
    }
}
