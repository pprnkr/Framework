namespace Nyonyo.Framework.Save
{
    /// <summary>
    /// 저장 데이터를 실제로 기록하는 곳입니다. 키 하나에 문자열 하나를 저장합니다.
    /// </summary>
    /// <remarks>
    /// 기본 구현은 <see cref="FileSaveStorage"/>(파일)와 <see cref="PlayerPrefsSaveStorage"/>(WebGL용)입니다.
    /// 암호화나 클라우드 저장이 필요하면 이 인터페이스를 구현하세요.
    /// </remarks>
    public interface ISaveStorage
    {
        bool Exists(string key);

        bool TryRead(string key, out string data);

        void Write(string key, string data);

        void Delete(string key);
    }
}
