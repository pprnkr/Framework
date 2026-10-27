using System;
using System.Text;

namespace Nyonyo.Framework.Editor.Data
{
    /// <summary>
    /// 열 하나(또는 같은 이름으로 묶인 배열 열들)의 자료형입니다.
    /// </summary>
    internal sealed class SheetColumnType
    {
        public SheetColumnType(SheetValueKind kind, bool isArray, Type enumType = null)
        {
            Kind = kind;
            IsArray = isArray;
            EnumType = enumType;
        }

        public SheetValueKind Kind { get; }

        public bool IsArray { get; }

        /// <summary><see cref="SheetValueKind.Enum"/>일 때의 C# enum 형식입니다.</summary>
        public Type EnumType { get; }

        /// <summary>생성 코드에 쓰는 원소 형식 이름입니다(예: int, global::Game.ItemGrade).</summary>
        public string ElementTypeName
        {
            get
            {
                if (Kind == SheetValueKind.Int)
                    return "int";
                if (Kind == SheetValueKind.Long)
                    return "long";
                if (Kind == SheetValueKind.Float)
                    return "float";
                if (Kind == SheetValueKind.Double)
                    return "double";
                if (Kind == SheetValueKind.Bool)
                    return "bool";
                if (Kind == SheetValueKind.String)
                    return "string";
                return "global::" + EnumType.FullName.Replace('+', '.');
            }
        }

        /// <summary>생성 코드에 쓰는 형식 이름입니다(예: int[]).</summary>
        public string TypeName => IsArray ? ElementTypeName + "[]" : ElementTypeName;

        /// <summary>
        /// 열 구성 해시에 넣는 설명입니다. enum은 멤버 이름과 값까지 넣어서, enum이 바뀌면 데이터를 다시 가져오게 합니다.
        /// </summary>
        public string Describe()
        {
            var text = new StringBuilder(TypeName);
            if (Kind == SheetValueKind.Enum)
            {
                foreach (var name in Enum.GetNames(EnumType))
                    text.Append(';').Append(name).Append('=').Append(Convert.ToInt64(Enum.Parse(EnumType, name)));
            }
            return text.ToString();
        }
    }
}
