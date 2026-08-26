using System.Text.Json;
using System.Text.Json.Serialization;

namespace StringEnum;

/// <summary>
/// Reads and writes a <see cref="StringEnum{TEnum}"/> as a JSON string, including as an object key.
/// Applied automatically by <see cref="StringEnumConverterFactory"/>; naming it explicitly is only
/// needed to override a converter registered further up the chain.
/// </summary>
/// <typeparam name="TEnum">The enum type to convert.</typeparam>
public class StringEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : StringEnum<TEnum>
{
    /// <inheritdoc />
    public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => Convert(reader.GetString()),
            _ => throw new JsonException(
                $"Cannot convert {reader.TokenType} to '{typeof(TEnum).Name}': expected a JSON string."),
        };

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }

    /// <inheritdoc />
    public override TEnum ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Convert(reader.GetString())!;

    /// <inheritdoc />
    public override void WriteAsPropertyName(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            throw new JsonException($"'{typeof(TEnum).Name}' cannot be null when used as an object key.");
        }

        writer.WritePropertyName(value.Value);
    }

    // An undeclared value is malformed input, so it has to surface as JsonException: callers wrap
    // Deserialize in catch (JsonException) and would otherwise miss it.
    private static TEnum? Convert(string? value) =>
        value is null ? null
            : StringEnum<TEnum>.TryParse(value, out var found) ? found
            : throw new JsonException(
                $"The value '{value}' is not defined within the possible values of '{typeof(TEnum).Name}'.");
}