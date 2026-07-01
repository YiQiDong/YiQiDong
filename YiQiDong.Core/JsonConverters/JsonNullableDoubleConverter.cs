using System.Text.Json;
using System.Text;
using System;
using System.Text.Json.Serialization;

namespace YiQiDong.Core.JsonConverters
{
    public class JsonNullableDoubleConverter : JsonConverter<double?>
    {
        public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.Number:
                    return reader.GetDouble();
                case JsonTokenType.String:
                    var str = reader.GetString();
                    if (string.IsNullOrEmpty(str))
                        return null;
                    return double.Parse(str);
                default:
                    throw new FormatException($"值[{Encoding.UTF8.GetString(reader.ValueSpan)}]无法转换为double?");
            }
        }

        public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteNumberValue(value.Value);
        }
    }
}