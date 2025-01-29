using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Common.Extensions;
/// <summary>
/// AutoNumberToStringConverter class
/// </summary>
internal sealed class AutoNumberToStringConverter : JsonConverter<object>
{
    /// <summary>
    /// CanConvert
    /// </summary>
    /// <param name="typeToConvert"></param>
    /// <returns></returns>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(string) == typeToConvert;
    }

    /// <summary>
    /// Read
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.TryGetInt64(out long l) ?
                l.ToString() :
                reader.GetDouble().ToString();
        }
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }
        using (JsonDocument document = JsonDocument.ParseValue(ref reader))
        {
            return document.RootElement.Clone().ToString();
        }
    }

    /// <summary>
    /// Write
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
