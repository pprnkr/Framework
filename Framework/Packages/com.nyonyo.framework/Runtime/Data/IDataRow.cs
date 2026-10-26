namespace Nyonyo.Framework.Data
{
    /// <summary>
    /// <see cref="DataTable{TRow}"/>의 한 행입니다. <see cref="Id"/>로 행을 찾습니다.
    /// </summary>
    public interface IDataRow
    {
        string Id { get; }
    }
}
