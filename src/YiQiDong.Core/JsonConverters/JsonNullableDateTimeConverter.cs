using System.Text.Json;
using System.Text;
using System;
using System.Text.Json.Serialization;

namespace YiQiDong.Core.JsonConverters
{
    public class JsonNullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.String:
                    var str = reader.GetString();
                    if (string.IsNullOrEmpty(str))
                        return null;
                    return DateTime.Parse(str);
                default:
                    throw new FormatException($"值[{Encoding.UTF8.GetString(reader.ValueSpan)}]无法转换为DateTime?");
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteStringValue(value.Value.ToString("O"));
        }
    }
}