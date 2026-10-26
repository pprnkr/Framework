namespace Nyonyo.Framework.Save
{
    /// <summary>
    /// 저장 데이터를 문자열로 바꿉니다. 기본 구현은 <see cref="JsonSaveSerializer"/>입니다.
    /// </summary>
    public interface ISaveSerializer
    {
        string Serialize(object data);

        T Deserialize<T>(string text);
    }
}
