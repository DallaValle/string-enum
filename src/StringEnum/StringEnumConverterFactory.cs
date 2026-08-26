using System.Text.Json;
using System.Text.Json.Serialization;

namespace StringEnum;

/// <summary>
/// Supplies the right <see cref="StringEnumConverter{TEnum}"/> for any <see cref="StringEnum{TEnum}"/>,
/// so no enum has to name its own converter. Register it once per application through
/// <see cref="JsonSerializerOptions.Converters"/>, or annotate the enum class itself with
/// <c>[JsonConverter(typeof(StringEnumConverterFactory))]</c>.
/// </summary>
public sealed class StringEnumConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => FindStringEnumBase(typeToConvert) is not null;

    /// <inheritdoc />
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var stringEnumBase = FindStringEnumBase(typeToConvert);
        if (stringEnumBase is null)
        {
            return null;
        }

        // StringEnum<TEnum> is constrained to close over the deriving type, so its single type
        // argument is the type the converter needs.
        var enumType = stringEnumBase.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(typeof(StringEnumConverter<>).MakeGenericType(enumType))!;
    }

    private static Type? FindStringEnumBase(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(StringEnum<>))
            {
                return current;
            }
        }

        return null;
    }
}