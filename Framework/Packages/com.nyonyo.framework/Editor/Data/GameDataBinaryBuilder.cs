using System.Collections.Generic;
using Nyonyo.Framework.Data;

namespace Nyonyo.Framework.Editor.Data
{
    /// <summary>
    /// <see cref="SheetTable"/>을 런타임의 <see cref="DataTableBase"/>가 읽는 바이너리로 만듭니다(암호화 전).
    /// </summary>
    internal sealed class GameDataBinaryBuilder
    {
        public byte[] Build(SheetTable table)
        {
            var writer = new GameDataWriter();
            writer.WriteInt(GameDataFormat.Magic);
            writer.WriteInt(GameDataFormat.Version);
            writer.WriteLong(table.SchemaHash);
            writer.WriteInt(table.Rows.Count);

            foreach (var row in table.Rows)
            {
                for (var i = 0; i < table.Fields.Count; i++)
                {
                    var type = table.Fields[i].Type;
                    if (!type.IsArray)
                    {
                        WriteValue(writer, type.Kind, row[i]);
                        continue;
                    }

                    var values = (List<object>)row[i];
                    writer.WriteInt(values.Count);
                    foreach (var value in values)
                        WriteValue(writer, type.Kind, value);
                }
            }

            return writer.ToArray();
        }

        private void WriteValue(GameDataWriter writer, SheetValueKind kind, object value)
        {
            if (kind == SheetValueKind.Int || kind == SheetValueKind.Enum)
                writer.WriteInt((int)value);
            else if (kind == SheetValueKind.Long)
                writer.WriteLong((long)value);
            else if (kind == SheetValueKind.Float)
                writer.WriteFloat((float)value);
            else if (kind == SheetValueKind.Double)
                writer.WriteDouble((double)value);
            else if (kind == SheetValueKind.Bool)
                writer.WriteBool((bool)value);
            else
                writer.WriteString((string)value);
        }
    }
}
