using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Nyonyo.Framework.Assets;
using Nyonyo.Framework.Events;
using Nyonyo.Framework.Scenes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if NYONYO_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using Object = UnityEngine.Object;

namespace Nyonyo.Framework.UI
{
    /// <summary>
    /// <see cref="UILayer"/>별 Canvas를 만들고, 팝업 프리팹을 불러와 생성해서 층에 배치합니다.
    /// </summary>
    /// <remarks>
    /// 서비스로 등록해서 사용합니다. 등록한 뒤에는 <see cref="Instance"/>로 어디서든 꺼낼 수 있습니다.
    /// 프리팹은 팝업 클래스의 <see cref="PopupAttribute"/>에 적힌 경로와 방식으로 처음 띄울 때 불러오고, 그 뒤로는 재사용합니다.
    /// Play 중에 씬에 EventSystem이 없으면 직접 만들어서 유지합니다. 그러니 씬에는 EventSystem을 두지 마세요.
    /// </remarks>
    public sealed class PopupManager : IAsyncInitializable, IDisposable
    {
        /// <summary>가장 아래 층(<see cref="UILayer.Screen"/>) Canvas의 정렬 순서입니다.</summary>
        public const int BaseSortingOrder = 1000;

        /// <summary>층 사이의 정렬 순서 간격입니다. 씬 전환 페이드(30000)는 모든 층보다 위에 그려집니다.</summary>
        public const int SortingOrderStep = 1000;

        static readonly int s_layerCount = Enum.GetValues(typeof(UILayer)).Length;
        static readonly Predicate<PopupBase> s_isDestroyed = popup => popup == null;

        readonly Dictionary<Type, PopupBase> _prefabs = new();
        readonly List<PopupAttribute> _loadedPrefabs = new();
        readonly RectTransform[] _layerRoots = new RectTransform[s_layerCount];
        readonly List<PopupBase>[] _openPopups = new List<PopupBase>[s_layerCount];
        readonly GameObject _root;
        readonly IDisposable _sceneLoadedSubscription;
        readonly IAssetLoader _loader;
        readonly bool _ownsLoader;

        /// <summary>
        /// 서비스로 등록된 PopupManager입니다. <c>Services.Get&lt;PopupManager&gt;()</c>와 같습니다.
        /// </summary>
        public static PopupManager Instance => Services.Get<PopupManager>();

        /// <param name="settings">Canvas와 뒤로가기 설정입니다. null이면 기본값을 씁니다.</param>
        /// <param name="parent">UI 루트를 붙일 부모입니다. 보통 <see cref="Bootstrapper.Root"/>를 넘깁니다. null이면 DontDestroyOnLoad 오브젝트로 만듭니다.</param>
        /// <param name="eventBus">넘기면 <see cref="SceneLoaded"/> 이벤트를 받아, 씬이 바뀔 때 팝업을 자동으로 닫습니다.</param>
        /// <param name="loader">팝업 프리팹을 불러올 로더입니다. 서비스로 등록한 <see cref="AssetLoader"/>를 넘기세요. null이면 직접 하나 만들어 씁니다.</param>
        public PopupManager(UISettings settings, Transform parent, IEventBus eventBus = null, IAssetLoader loader = null)
        {
            _ownsLoader = loader == null;
            _loader = loader ?? new AssetLoader();

            var referenceResolution = settings != null ? settings.ReferenceResolution : new Vector2(1920, 1080);
            var match = settings != null ? settings.MatchWidthOrHeight : 0.5f;

            _root = new GameObject("[UI]");
            if (parent != null)
                _root.transform.SetParent(parent, false);
            else if (Application.isPlaying)
                Object.DontDestroyOnLoad(_root);

            for (var i = 0; i < s_layerCount; i++)
            {
                _layerRoots[i] = CreateLayerCanvas((UILayer)i, referenceResolution, match);
                _openPopups[i] = new List<PopupBase>();
            }

            if (settings != null && settings.HandleBackKey)
                _root.AddComponent<PopupBackKeyHandler>().Manager = this;

            if (eventBus != null)
                _sceneLoadedSubscription = eventBus.Subscribe<SceneLoaded>(OnSceneLoaded);

            EnsureEventSystem();
        }

        /// <summary>
        /// 뒤로가기 키를 눌렀는데 닫을 팝업이 없을 때 호출됩니다. 게임 종료 확인창을 띄울 때 씁니다.
        /// </summary>
        public event Action BackKeyUnhandled;

        /// <summary>
        /// 부트스트랩 초기화 단계에서 <c>[Popup(Preload = true)]</c> 팝업 프리팹을 미리 불러 둡니다.
        /// 하나가 실패해도 에러를 남기고 나머지는 계속 불러옵니다.
        /// </summary>
        public Awaitable InitializeAsync(CancellationToken cancellationToken) =>
            PreloadTypesAsync(FindPreloadPopupTypes(), cancellationToken);

        internal async Awaitable PreloadTypesAsync(IEnumerable<Type> types, CancellationToken cancellationToken)
        {
            foreach (var type in types)
            {
                try
                {
                    await LoadPrefabAsync(type, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// 로드된 게임 어셈블리에서 <c>[Popup(Preload = true)]</c>가 붙은 팝업 타입을 찾습니다.
        /// Unity와 .NET 어셈블리, 그리고 에디터에서 함께 로드되는 테스트 어셈블리는 건너뜁니다.
        /// </summary>
        internal static List<Type> FindPreloadPopupTypes()
        {
            var assemblies = new List<Assembly>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!IsEngineAssembly(assembly.GetName().Name) && !IsTestAssembly(assembly))
                    assemblies.Add(assembly);
            }

            return FindPreloadPopupTypes(assemblies);
        }

        internal static List<Type> FindPreloadPopupTypes(IEnumerable<Assembly> assemblies)
        {
            var result = new List<Type>();
            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract || !typeof(PopupBase).IsAssignableFrom(type))
                        continue;

                    var info = type.GetCustomAttribute<PopupAttribute>();
                    if (info != null && info.Preload)
                        result.Add(type);
                }
            }

            return result;
        }

        static bool IsEngineAssembly(string name) =>
            name == "mscorlib" || name == "netstandard" ||
            name.StartsWith("System", StringComparison.Ordinal) ||
            name.StartsWith("Unity", StringComparison.Ordinal) ||
            name.StartsWith("Mono.", StringComparison.Ordinal) ||
            name.StartsWith("nunit", StringComparison.OrdinalIgnoreCase);

        static bool IsTestAssembly(Assembly assembly)
        {
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name == "nunit.framework")
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 이미 불러온 팝업 프리팹을 직접 등록합니다. 등록된 프리팹은 <see cref="PopupAttribute"/>보다 우선합니다.
        /// 같은 타입이 이미 등록되어 있으면 경고를 띄우고 무시합니다.
        /// </summary>
        public void RegisterPrefab(PopupBase prefab)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[Nyonyo Framework] 비어 있는 팝업 프리팹 항목이 있습니다.");
                return;
            }

            var type = prefab.GetType();
            if (!_prefabs.TryAdd(type, prefab))
                Debug.LogWarning($"[Nyonyo Framework] {type.Name} 팝업 프리팹이 이미 등록되어 있어서 {prefab.name}을(를) 무시합니다.", prefab);
        }

        /// <summary>
        /// <typeparamref name="T"/> 팝업을 생성해서 띄웁니다. 같은 층에 이미 팝업이 있으면 그 위에 그려집니다.
        /// </summary>
        /// <remarks>
        /// 프리팹을 아직 불러오지 않았다면 그 자리에서 불러옵니다(Addressables는 WaitForCompletion).
        /// WebGL은 동기 로드를 지원하지 않으므로, WebGL에서 Addressables 팝업을 Show로 띄우려면
        /// <c>[Popup(Preload = true)]</c>로 미리 불러 두거나 <see cref="ShowAsync{T}"/>를 쓰세요.
        /// </remarks>
        /// <param name="layer">팝업을 띄울 층입니다. null이면 프리팹에 설정된 층을 씁니다.</param>
        /// <returns>생성된 팝업입니다. 내용을 채우거나 닫힐 때까지 기다릴 때 씁니다.</returns>
        public T Show<T>(UILayer? layer = null) where T : PopupBase
        {
            if (!_prefabs.TryGetValue(typeof(T), out var prefab))
            {
                var info = GetPopupAttribute(typeof(T));
                prefab = CachePrefab(typeof(T), info, _loader.Load<GameObject>(info.Path, info.LoadType));
            }

            return Spawn<T>(prefab, layer);
        }

        /// <summary>
        /// 프리팹을 비동기로 불러온 뒤 <typeparamref name="T"/> 팝업을 띄웁니다.
        /// </summary>
        public async Awaitable<T> ShowAsync<T>(UILayer? layer = null, CancellationToken cancellationToken = default) where T : PopupBase
        {
            var prefab = await LoadPrefabAsync(typeof(T), cancellationToken);
            return Spawn<T>(prefab, layer);
        }

        /// <summary>
        /// <typeparamref name="T"/> 팝업 프리팹을 미리 불러 둡니다. 그 뒤의 <see cref="Show{T}"/>는 바로 뜹니다.
        /// </summary>
        public async Awaitable PreloadAsync<T>(CancellationToken cancellationToken = default) where T : PopupBase =>
            await LoadPrefabAsync(typeof(T), cancellationToken);

        /// <summary>
        /// 열려 있는 <typeparamref name="T"/> 팝업 중 가장 위에 있는 것을 찾습니다. 없으면 null입니다.
        /// </summary>
        public T Find<T>() where T : PopupBase
        {
            for (var layer = s_layerCount - 1; layer >= 0; layer--)
            {
                var popups = GetOpenPopupList(layer);
                for (var i = popups.Count - 1; i >= 0; i--)
                {
                    if (popups[i] is T popup)
                        return popup;
                }
            }

            return null;
        }

        public bool IsOpen<T>() where T : PopupBase => Find<T>() != null;

        /// <summary>
        /// <paramref name="layer"/>에 열려 있는 팝업을 아래에서 위 순서로 돌려줍니다.
        /// </summary>
        public IReadOnlyList<PopupBase> GetOpenPopups(UILayer layer) => GetOpenPopupList((int)layer);

        /// <summary>
        /// 층의 Canvas입니다. 팝업이 아닌 UI(HUD 등)를 같은 층 규칙으로 그리고 싶을 때 자식으로 붙이세요.
        /// </summary>
        public RectTransform GetLayerRoot(UILayer layer) => _layerRoots[(int)layer];

        /// <summary>
        /// 팝업을 닫습니다. <paramref name="layer"/>를 지정하면 그 층만 닫습니다.
        /// </summary>
        public void CloseAll(UILayer? layer = null)
        {
            for (var i = s_layerCount - 1; i >= 0; i--)
            {
                if (!layer.HasValue || (int)layer.Value == i)
                    CloseWhere(i, _ => true);
            }
        }

        /// <summary>
        /// 뒤로가기를 처리합니다. 보이는 팝업 중 가장 위 팝업부터 <see cref="PopupBase.BackKey"/> 설정에 따라 닫거나 막거나 건너뜁니다.
        /// </summary>
        /// <returns>팝업이 뒤로가기를 처리했으면 true, 처리할 팝업이 없으면 false입니다.</returns>
        public bool HandleBack()
        {
            for (var layer = s_layerCount - 1; layer >= 0; layer--)
            {
                var popups = GetOpenPopupList(layer);
                for (var i = popups.Count - 1; i >= 0; i--)
                {
                    // Hide()로 숨겨 둔 팝업은 뒤로가기 대상에서 빠집니다.
                    if (!popups[i].IsVisible)
                        continue;

                    switch (popups[i].BackKey)
                    {
                        case BackKeyBehavior.Ignore:
                            continue;
                        case BackKeyBehavior.Block:
                            return true;
                        default:
                            popups[i].Close();
                            return true;
                    }
                }
            }

            BackKeyUnhandled?.Invoke();
            return false;
        }

        /// <summary>
        /// 이벤트 구독을 끊고, <see cref="PopupAttribute"/>로 불러온 프리팹을 로더에 반환합니다.
        /// </summary>
        public void Dispose()
        {
            _sceneLoadedSubscription?.Dispose();
            BackKeyUnhandled = null;

            foreach (var info in _loadedPrefabs)
                _loader.Release(info.Path, info.LoadType);
            _loadedPrefabs.Clear();

            if (_ownsLoader && _loader is IDisposable disposable)
                disposable.Dispose();
        }

        async Awaitable<PopupBase> LoadPrefabAsync(Type type, CancellationToken cancellationToken)
        {
            if (_prefabs.TryGetValue(type, out var prefab))
                return prefab;

            var info = GetPopupAttribute(type);
            var loaded = await _loader.LoadAsync<GameObject>(info.Path, info.LoadType, cancellationToken);

            // 기다리는 동안 다른 호출이 먼저 불러왔다면, 이번에 불러온 것은 반환합니다.
            if (_prefabs.TryGetValue(type, out prefab))
            {
                _loader.Release(info.Path, info.LoadType);
                return prefab;
            }

            return CachePrefab(type, info, loaded);
        }

        PopupBase CachePrefab(Type type, PopupAttribute info, GameObject loaded)
        {
            var prefab = loaded.GetComponent(type) as PopupBase;
            if (prefab == null)
            {
                _loader.Release(info.Path, info.LoadType);
                throw new InvalidOperationException(
                    $"{info.Path} 프리팹 루트에 {type.Name} 컴포넌트가 없습니다. 프리팹과 [Popup] 경로를 확인하세요.");
            }

            _prefabs.Add(type, prefab);
            _loadedPrefabs.Add(info);
            return prefab;
        }

        T Spawn<T>(PopupBase prefab, UILayer? layer) where T : PopupBase
        {
            var targetLayer = layer ?? prefab.Layer;
            var popup = (T)Object.Instantiate(prefab, _layerRoots[(int)targetLayer], false);
            popup.name = prefab.name;
            popup.transform.SetAsLastSibling();

            _openPopups[(int)targetLayer].Add(popup);
            popup.Open(this, targetLayer);
            return popup;
        }

        static PopupAttribute GetPopupAttribute(Type type)
        {
            var info = type.GetCustomAttribute<PopupAttribute>();
            if (info == null || string.IsNullOrEmpty(info.Path))
                throw new InvalidOperationException(
                    $"{type.Name}에 [Popup(Path = ..., LoadType = ...)] 어트리뷰트가 없어서 프리팹을 불러올 수 없습니다. " +
                    "어트리뷰트를 추가하거나 RegisterPrefab으로 등록하세요.");

            return info;
        }

        internal void Remove(PopupBase popup)
        {
            foreach (var popups in _openPopups)
            {
                if (popups.Remove(popup))
                    return;
            }
        }

        // Close를 거치지 않고 Destroy된 팝업이 목록에 남지 않도록 꺼낼 때마다 정리합니다.
        List<PopupBase> GetOpenPopupList(int layer)
        {
            var popups = _openPopups[layer];
            popups.RemoveAll(s_isDestroyed);
            return popups;
        }

        void OnSceneLoaded(SceneLoaded evt)
        {
            for (var layer = s_layerCount - 1; layer >= 0; layer--)
                CloseWhere(layer, popup => popup.CloseOnSceneChange);
        }

        // 닫히는 팝업의 Closed 핸들러가 다른 팝업을 닫아도 안전하도록 복사본을 위에서부터 순회합니다.
        void CloseWhere(int layer, Predicate<PopupBase> match)
        {
            var snapshot = GetOpenPopupList(layer).ToArray();
            for (var i = snapshot.Length - 1; i >= 0; i--)
            {
                var popup = snapshot[i];
                if (popup != null && !popup.IsClosed && match(popup))
                    popup.Close();
            }
        }

        RectTransform CreateLayerCanvas(UILayer layer, Vector2 referenceResolution, float match)
        {
            var go = new GameObject($"{layer} Layer", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(_root.transform, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = BaseSortingOrder + (int)layer * SortingOrderStep;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = match;

            return (RectTransform)go.transform;
        }

        void EnsureEventSystem()
        {
            if (!Application.isPlaying || EventSystem.current != null)
                return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.transform.SetParent(_root.transform, false);
#if NYONYO_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
