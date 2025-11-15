using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpVitamins;

namespace Raytha.Domain.JsonConverters;

public class ShortGuidConverter : JsonConverter<ShortGuid>
{
    public override ShortGuid Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var value = reader.GetString();
        return new ShortGuid(value);
    }

    public override void Write(
        Utf8JsonWriter writer,
        ShortGuid value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStringValue(value.ToString());
    }

    public override ShortGuid ReadAsPropertyName(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) => new ShortGuid(reader.GetString());

    public override void WriteAsPropertyName(
        Utf8JsonWriter writer,
        ShortGuid value,
        JsonSerializerOptions options
    ) => writer.WritePropertyName(value.ToString());
}
