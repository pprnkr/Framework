using System.IO;

namespace Nyonyo.Framework.Data
{
    /// <summary>
    /// Config 시트(1행이 <c>Type | Key | Value</c>)를 담는 테이블입니다. 시트의 Key마다 속성이 하나씩 생깁니다.
    /// 데이터 가져오기가 시트마다 상속 클래스를 만듭니다.
    /// </summary>
    /// <example>
    /// <code>
    /// int maxStamina = GameData.Instance.Config.MaxStamina;
    /// string notice = GameData.Instance.Config.NoticeText;
    /// </code>
    /// </example>
    public abstract class ConfigTableBase : DataTableBase
    {
        /// <summary>값을 시트 순서대로 읽습니다. 자동 생성 코드가 구현합니다.</summary>
        protected abstract void ReadValues(GameDataReader reader);

        private protected sealed override void LoadRows(GameDataReader reader, int count)
        {
            if (count != 1)
                throw new InvalidDataException($"{Name}: Config 데이터 파일이 아닙니다. 데이터를 다시 가져오세요.");

            ReadValues(reader);
        }
    }
}
