using System.Text.Json;
using System.Text.Json.Serialization;

namespace StringEnum;

public abstract class StringEnum<EnumClass>
{
    public readonly string Value;

    protected StringEnum(string value)
    {
        Value = value;
    }

    public static implicit operator string(StringEnum<EnumClass> input) => input.Value;

    public override string ToString() => Value;

    public override int GetHashCode() => Value.GetHashCode();

    public static IEnumerable<EnumClass> GetValues() =>
        GetEnumClassFields(typeof(EnumClass))
            .Cast<EnumClass>();

    public static EnumClass Parse(string value)
    {
        var found = FoundInArray(value);
        return found is null
            ? throw new ArgumentException($"The parameter '{value}' it is not defined within the possible values of the enum")
            : (EnumClass)found;
    }

    public static bool TryParse(string value, out EnumClass? stringEnum)
    {
        stringEnum = default;
        var found = FoundInArray(value);
        if (found is not null)
        {
            stringEnum = (EnumClass)found;
            return true;
        }
        return false;
    }

    private static object? FoundInArray(string value) =>
        Array.Find(
            GetEnumClassFields(typeof(EnumClass)),
            _ => _ is not null && _ is EnumClass && value == _.ToString());

    private static object?[] GetEnumClassFields(Type type) =>
        type.GetFields()
            .Where(_ => type.IsAssignableFrom(_.FieldType))
            .Select(_ => _.GetValue(null))
            .ToArray();
}

public class StringEnumConverter<EnumClass> : JsonConverter<EnumClass> where EnumClass : StringEnum<EnumClass>
{
    public override EnumClass? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value is null ? null : StringEnum<EnumClass>.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, EnumClass value, JsonSerializerOptions options) =>
        writer.WriteStringValue((string)value);
}