using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpVitamins;
using Raytha.Application.Common.Models;

namespace Raytha.Web.Middlewares;

public class ShortGuidConverter : JsonConverter<ShortGuid>
{
    public override ShortGuid Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) => new ShortGuid(reader.GetString());

    public override void Write(
        Utf8JsonWriter writer,
        ShortGuid shortGuid,
        JsonSerializerOptions options
    )
    {
        if (shortGuid.Value == ShortGuid.Empty)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(shortGuid.Value);
        }
    }
}

public class AuditableUserDtoConverter : JsonConverter<AuditableUserDto>
{
    public override AuditableUserDto Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) => throw new NotImplementedException();

    public override void Write(
        Utf8JsonWriter writer,
        AuditableUserDto user,
        JsonSerializerOptions options
    )
    {
        if (user.Id.Value == ShortGuid.Empty)
        {
            writer.WriteNullValue();
        }
        else
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };
            jsonOptions.Converters.Add(new ShortGuidConverter());
            JsonSerializer.Serialize(writer, user, user.GetType(), jsonOptions);
        }
    }
}
