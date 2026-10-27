using System;

namespace Nyonyo.Framework.Editor.Data
{
    /// <summary>
    /// 시트 자료형에 적힌 이름으로 C# enum 형식을 찾습니다. 찾지 못하거나 여러 개면 null과 이유를 돌려줍니다.
    /// </summary>
    internal delegate Type EnumFinder(string name, out string error);
}
