using System;
using UnityEngine;

namespace Nyonyo.Framework
{
    // 확장 메서드는 C#에서 static 클래스에만 둘 수 있습니다.
    static class AwaitableExtensions
    {
        /// <summary>
        /// 결과를 기다리지 않고 실행합니다. 결과 값은 버리고, 예외 처리는 <see cref="Forget(Awaitable)"/>과 같습니다.
        /// </summary>
        public static async void Forget<T>(this Awaitable<T> awaitable)
        {
            try
            {
                await awaitable;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// 결과를 기다리지 않고 실행합니다. 취소는 무시하고, 다른 예외는 버려지지 않도록 <see cref="Debug.LogException(Exception)"/>으로 남깁니다.
        /// 이미 완료된 작업이면 그 자리에서 바로 끝납니다.
        /// </summary>
        public static async void Forget(this Awaitable awaitable)
        {
            try
            {
                await awaitable;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
