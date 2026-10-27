using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Nyonyo.Framework
{
    /// <summary>
    /// 서비스 인스턴스를 타입별로 보관하고 초기화와 정리를 관리합니다.
    /// 서비스는 등록한 순서대로 초기화되고, 그 역순으로 정리됩니다.
    /// </summary>
    /// <remarks>
    /// 같은 인스턴스를 여러 타입으로 등록해도 초기화와 정리는 한 번만 실행됩니다.
    /// 초기화 중에 다른 서비스를 사용한다면, 그 서비스를 먼저 등록해야 합니다.
    /// </remarks>
    public sealed class ServiceRegistry : IDisposable
    {
        private enum Phase
        {
            Registering,
            Initializing,
            Ready,
            Failed,
            Disposed,
        }

        private readonly Dictionary<Type, object> _services = new();
        private readonly List<object> _instances = new();
        private readonly List<object> _initialized = new();
        private Phase _phase = Phase.Registering;

        /// <summary>
        /// 서비스를 <typeparamref name="TService"/> 타입으로 등록합니다. 초기화가 시작되기 전에만 호출할 수 있습니다.
        /// </summary>
        public void Register<TService>(TService instance) where TService : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var type = typeof(TService);
            if (_phase != Phase.Registering)
                throw new InvalidOperationException($"{type.Name}: 서비스는 초기화가 시작되기 전에만 등록할 수 있습니다.");
            if (_services.ContainsKey(type))
                throw new InvalidOperationException($"{type.Name}은(는) 이미 등록된 서비스입니다.");

            _services.Add(type, instance);
            if (!ContainsInstance(instance))
                _instances.Add(instance);
        }

        public TService Get<TService>() where TService : class
        {
            if (TryGet(out TService service))
                return service;

            throw new InvalidOperationException(
                $"{typeof(TService).Name} 서비스가 등록되지 않았습니다. GameInstaller에서 등록했는지 확인하세요.");
        }

        public bool TryGet<TService>(out TService service) where TService : class
        {
            if (_services.TryGetValue(typeof(TService), out var instance))
            {
                service = (TService)instance;
                return true;
            }

            service = null;
            return false;
        }

        public bool Contains<TService>() where TService : class => _services.ContainsKey(typeof(TService));

        /// <summary>
        /// 등록된 서비스를 등록 순서대로 초기화합니다. 한 번만 호출할 수 있습니다.
        /// 서비스 하나가 실패하면 그 뒤의 서비스는 초기화하지 않고 예외를 그대로 던집니다.
        /// </summary>
        public async Awaitable InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_phase != Phase.Registering)
                throw new InvalidOperationException("ServiceRegistry는 한 번만 초기화할 수 있습니다.");

            _phase = Phase.Initializing;
            try
            {
                for (var i = 0; i < _instances.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var instance = _instances[i];
                    if (instance is IInitializable initializable)
                        initializable.Initialize();
                    if (instance is IAsyncInitializable asyncInitializable)
                        await asyncInitializable.InitializeAsync(cancellationToken);

                    // 비동기 초기화를 기다리는 동안 Dispose가 호출됐다면 방금 초기화한 서비스도 정리합니다.
                    if (_phase == Phase.Disposed)
                    {
                        DisposeSafely(instance);
                        throw new OperationCanceledException("초기화 도중 ServiceRegistry가 정리되었습니다.");
                    }

                    _initialized.Add(instance);
                }
            }
            catch
            {
                if (_phase == Phase.Initializing)
                    _phase = Phase.Failed;
                throw;
            }

            _phase = Phase.Ready;
        }

        /// <summary>
        /// 초기화가 끝난 서비스를 역순으로 정리하고 모든 등록을 해제합니다.
        /// 서비스 하나가 정리 중에 예외를 던져도 나머지 서비스는 계속 정리합니다.
        /// </summary>
        public void Dispose()
        {
            if (_phase == Phase.Disposed)
                return;

            _phase = Phase.Disposed;
            for (var i = _initialized.Count - 1; i >= 0; i--)
                DisposeSafely(_initialized[i]);

            _initialized.Clear();
            _instances.Clear();
            _services.Clear();
        }

        private bool ContainsInstance(object instance)
        {
            foreach (var existing in _instances)
            {
                if (ReferenceEquals(existing, instance))
                    return true;
            }

            return false;
        }

        private void DisposeSafely(object instance)
        {
            if (instance is not IDisposable disposable)
                return;

            try
            {
                disposable.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
