#if NYONYO_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using System.Threading;
using Nyonyo.Framework.Save;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nyonyo.Framework.Inputs
{
    /// <summary>
    /// Input System 액션맵 전환과 키 리바인딩을 관리합니다. Input System 패키지가 있을 때만 사용할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 한 번에 하나의 액션맵만 켭니다. 팝업을 열 때 <see cref="PushMap"/>("UI"), 닫을 때 <see cref="PopMap"/>을 부르면
    /// 이전 액션맵으로 돌아갑니다. <see cref="SaveService"/>를 넘기면 리바인딩 결과를 전역 데이터로 저장하고 불러옵니다.
    /// </remarks>
    /// <example>
    /// <code>
    /// var input = InputService.Instance;
    /// input.SwitchMap("Player");
    /// if (input.GetAction("Jump").WasPressedThisFrame()) Jump();
    ///
    /// // 설정 화면: 키 다시 지정(ESC로 취소)
    /// var changed = await input.RebindAsync(input.GetAction("Jump"));
    /// label.text = input.GetBindingDisplay(input.GetAction("Jump"));
    /// </code>
    /// </example>
    public sealed class InputService : IDisposable
    {
        /// <summary>리바인딩 결과를 저장하는 전역 저장 키입니다.</summary>
        public const string BindingsSaveKey = "input.bindings";

        [Serializable]
        sealed class BindingData
        {
            public string json;
        }

        readonly List<InputActionMap> _mapStack = new();
        readonly SaveService _save;
        InputActionRebindingExtensions.RebindingOperation _rebinding;

        /// <summary>
        /// 서비스로 등록된 InputService입니다. <c>Services.Get&lt;InputService&gt;()</c>와 같습니다.
        /// </summary>
        public static InputService Instance => Services.Get<InputService>();

        /// <param name="actions">사용할 액션 에셋입니다. null이면 프로젝트 전체 액션(<c>InputSystem.actions</c>)을 씁니다.</param>
        /// <param name="save">넘기면 리바인딩 결과를 저장하고, 저장된 결과를 불러옵니다.</param>
        /// <param name="initialMap">처음 켤 액션맵 이름입니다. null이면 액션맵을 바꾸지 않습니다.</param>
        public InputService(InputActionAsset actions = null, SaveService save = null, string initialMap = null)
        {
            Actions = actions != null ? actions : InputSystem.actions;
            if (Actions == null)
                throw new InvalidOperationException(
                    "사용할 InputActionAsset이 없습니다. Project Settings > Input System Package에서 프로젝트 액션을 지정하거나 에셋을 넘기세요.");

            _save = save;
            LoadBindings();

            if (!string.IsNullOrEmpty(initialMap))
                SwitchMap(initialMap);
        }

        public InputActionAsset Actions { get; }

        /// <summary>지금 켜져 있는 액션맵입니다. 없으면 null입니다.</summary>
        public InputActionMap CurrentMap => _mapStack.Count > 0 ? _mapStack[_mapStack.Count - 1] : null;

        /// <summary>리바인딩 중이면 true입니다.</summary>
        public bool IsRebinding => _rebinding != null;

        /// <summary>
        /// 액션을 찾습니다. "Jump"처럼 이름만 쓰거나 "Player/Jump"처럼 액션맵을 함께 쓸 수 있습니다.
        /// </summary>
        public InputAction GetAction(string nameOrPath) => Actions.FindAction(nameOrPath, true);

        /// <summary>
        /// 다른 액션맵을 모두 끄고 <paramref name="mapName"/>만 켭니다. 쌓아 둔 액션맵 기록은 지웁니다.
        /// </summary>
        public void SwitchMap(string mapName)
        {
            _mapStack.Clear();
            _mapStack.Add(Actions.FindActionMap(mapName, true));
            ApplyMaps();
        }

        /// <summary>
        /// <paramref name="mapName"/>을 켜고, 이전 액션맵은 기억해 둡니다. 팝업을 열 때 씁니다.
        /// </summary>
        public void PushMap(string mapName)
        {
            _mapStack.Add(Actions.FindActionMap(mapName, true));
            ApplyMaps();
        }

        /// <summary>
        /// <see cref="PushMap"/>하기 전의 액션맵으로 돌아갑니다. 팝업을 닫을 때 씁니다.
        /// </summary>
        public void PopMap()
        {
            if (_mapStack.Count == 0)
                return;

            _mapStack.RemoveAt(_mapStack.Count - 1);
            ApplyMaps();
        }

        /// <summary>
        /// 다음에 누르는 키로 바인딩을 바꿉니다. ESC를 누르면 취소됩니다. 바뀌면 저장하고 true를 돌려줍니다.
        /// </summary>
        /// <param name="bindingIndex">바꿀 바인딩 순서입니다. 복합 바인딩(WASD 등)은 각 방향의 순서를 넘기세요.</param>
        public async Awaitable<bool> RebindAsync(InputAction action, int bindingIndex = 0, CancellationToken cancellationToken = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (_rebinding != null)
                throw new InvalidOperationException("이미 다른 키를 다시 지정하는 중입니다.");

            var completion = new AwaitableCompletionSource<bool>();
            var wasEnabled = action.enabled;
            action.Disable();

            _rebinding = action.PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(_ => completion.TrySetResult(true))
                .OnCancel(_ => completion.TrySetResult(false))
                .Start();

            using var registration = cancellationToken.Register(() => _rebinding?.Cancel());
            bool changed;
            try
            {
                changed = await completion.Awaitable;
            }
            finally
            {
                _rebinding?.Dispose();
                _rebinding = null;
                if (wasEnabled)
                    action.Enable();
            }

            if (changed)
                SaveBindings();
            return changed;
        }

        /// <summary>
        /// 화면에 표시할 바인딩 이름입니다(예: "Space", "A").
        /// </summary>
        public string GetBindingDisplay(InputAction action, int bindingIndex = 0) => action.GetBindingDisplayString(bindingIndex);

        /// <summary>
        /// 리바인딩을 기본값으로 되돌리고 저장합니다. <paramref name="action"/>이 null이면 모든 액션을 되돌립니다.
        /// </summary>
        public void ResetBindings(InputAction action = null)
        {
            if (action != null)
                action.RemoveAllBindingOverrides();
            else
                Actions.RemoveAllBindingOverrides();

            SaveBindings();
        }

        /// <summary>
        /// 지금 리바인딩 상태를 저장합니다. <see cref="RebindAsync"/>와 <see cref="ResetBindings"/>는 자동으로 저장합니다.
        /// </summary>
        public void SaveBindings() =>
            _save?.Save(BindingsSaveKey, new BindingData { json = Actions.SaveBindingOverridesAsJson() }, SaveScope.Global);

        public void Dispose()
        {
            _rebinding?.Cancel();
            _rebinding?.Dispose();
            _rebinding = null;
        }

        void LoadBindings()
        {
            if (_save == null || !_save.TryLoad(BindingsSaveKey, out BindingData data, SaveScope.Global) ||
                string.IsNullOrEmpty(data.json))
                return;

            Actions.LoadBindingOverridesFromJson(data.json);
        }

        void ApplyMaps()
        {
            var current = CurrentMap;
            foreach (var map in Actions.actionMaps)
            {
                if (map != current)
                    map.Disable();
            }

            current?.Enable();
        }
    }
}
#endif
