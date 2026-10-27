using System;
using System.Collections;
using UnityEngine;

namespace Nyonyo.Framework.Save
{
    /// <summary>
    /// Unity <see cref="JsonUtility"/>로 저장합니다. WebGL, IL2CPP에서 코드 스트리핑 걱정 없이 동작합니다.
    /// </summary>
    /// <remarks>
    /// JsonUtility 규칙을 따릅니다.
    /// 데이터는 [Serializable] 클래스나 구조체여야 하고, public 필드나 [SerializeField] 필드만 저장됩니다.
    /// Dictionary와 프로퍼티는 저장되지 않습니다. int, string, 배열 같은 값은 클래스 필드로 감싸서 저장하세요.
    /// </remarks>
    public sealed class JsonSaveSerializer : ISaveSerializer
    {
        private readonly bool _prettyPrint;

        public JsonSaveSerializer(bool prettyPrint = false)
        {
            _prettyPrint = prettyPrint;
        }

        public string Serialize(object data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var type = data.GetType();
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) ||
                typeof(IEnumerable).IsAssignableFrom(type))
                throw new ArgumentException(
                    $"{type.Name}은(는) JsonUtility로 바로 저장할 수 없습니다. [Serializable] 클래스의 필드로 감싸서 저장하세요.",
                    nameof(data));

            return JsonUtility.ToJson(data, _prettyPrint);
        }

        public T Deserialize<T>(string text) => JsonUtility.FromJson<T>(text);
    }
}
