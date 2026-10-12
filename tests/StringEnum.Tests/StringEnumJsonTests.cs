using System.Text.Json;
using System.Text.Json.Serialization;

using NUnit.Framework;

namespace StringEnum.Tests;

public class StringEnumJsonTests
{
    internal class Order
    {
        public StateEnum? State { get; set; }
    }

    internal class ExplicitlyAnnotatedOrder
    {
        [JsonConverter(typeof(StringEnumConverter<UnannotatedEnum>))]
        public UnannotatedEnum? State { get; set; }
    }

    // One attribute on the enum class covers every property, root value and dictionary key.
    [JsonConverter(typeof(StringEnumConverterFactory))]
    internal class StateEnum : StringEnum<StateEnum>
    {
        public static readonly StateEnum Created = new("created");
        public static readonly StateEnum Offered = new("offered");

        public StateEnum(string value) : base(value) { }
    }

    // Left unannotated on purpose, to prove the options-based registration path.
    internal class UnannotatedEnum : StringEnum<UnannotatedEnum>
    {
        public static readonly UnannotatedEnum Created = new("created");
        public static readonly UnannotatedEnum Offered = new("offered");

        public UnannotatedEnum(string value) : base(value) { }
    }

    private static readonly JsonSerializerOptions FactoryOptions =
        new() { Converters = { new StringEnumConverterFactory() } };

    [Test]
    public void StringEnumConverter_ShouldSerializeAsExpect()
    {
        var result = JsonSerializer.Serialize(new Order { State = StateEnum.Created });

        Assert.That(result, Is.EqualTo("{\"State\":\"created\"}"));
    }

    [Test]
    public void StringEnumConverter_ShouldDeserializeAsExpect()
    {
        var result = JsonSerializer.Deserialize<Order>("{\"State\":\"created\"}")!;

        Assert.That(result.State, Is.EqualTo(StateEnum.Created));
    }

    [Test]
    public void StringEnumConverter_ShouldRoundTripAtTheRoot()
    {
        var json = JsonSerializer.Serialize(StateEnum.Created);
        Assert.That(json, Is.EqualTo("\"created\""));

        Assert.That(JsonSerializer.Deserialize<StateEnum>(json), Is.EqualTo(StateEnum.Created));
    }

    [Test]
    public void StringEnumConverter_ShouldRoundTripNull()
    {
        var json = JsonSerializer.Serialize(new Order { State = null });
        Assert.That(json, Is.EqualTo("{\"State\":null}"));

        Assert.That(JsonSerializer.Deserialize<Order>(json)!.State, Is.Null);
    }

    [Test]
    public void StringEnumConverter_WithUndeclaredValue_ShouldThrowJsonException()
    {
        // Callers guard Deserialize with catch (JsonException); an ArgumentException would escape it.
        var exception = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<Order>("{\"State\":\"nope\"}"));

        Assert.That(exception!.Message, Does.Contain("nope"));
    }

    [Test]
    public void StringEnumConverter_WithWrongTokenType_ShouldThrowJsonException()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Order>("{\"State\":42}"));
    }

    [Test]
    public void StringEnumConverter_ShouldRoundTripAsDictionaryKey()
    {
        var source = new Dictionary<StateEnum, int> { [StateEnum.Created] = 1, [StateEnum.Offered] = 2 };

        var json = JsonSerializer.Serialize(source);
        Assert.That(json, Is.EqualTo("{\"created\":1,\"offered\":2}"));

        var result = JsonSerializer.Deserialize<Dictionary<StateEnum, int>>(json)!;
        Assert.That(result[StateEnum.Created], Is.EqualTo(1));
        Assert.That(result[StateEnum.Offered], Is.EqualTo(2));
    }

    [Test]
    public void StringEnumConverter_ShouldWorkWithExplicitPropertyAttribute()
    {
        var json = JsonSerializer.Serialize(new ExplicitlyAnnotatedOrder { State = UnannotatedEnum.Offered });
        Assert.That(json, Is.EqualTo("{\"State\":\"offered\"}"));

        var result = JsonSerializer.Deserialize<ExplicitlyAnnotatedOrder>(json)!;
        Assert.That(result.State, Is.EqualTo(UnannotatedEnum.Offered));
    }

    [Test]
    public void StringEnumConverterFactory_ShouldConvertEnumsWithNoAttribute()
    {
        Assert.That(JsonSerializer.Serialize(UnannotatedEnum.Created, FactoryOptions), Is.EqualTo("\"created\""));
        Assert.That(
            JsonSerializer.Deserialize<UnannotatedEnum>("\"offered\"", FactoryOptions),
            Is.EqualTo(UnannotatedEnum.Offered));
    }

    [Test]
    public void WithoutAnyRegistration_ShouldNotSerializeAsAString()
    {
        // Documents the boundary: an unannotated enum falls back to the default object converter.
        Assert.That(JsonSerializer.Serialize(UnannotatedEnum.Created), Is.EqualTo("{\"Value\":\"created\"}"));
    }

    [Test]
    public void StringEnumConverterFactory_ShouldNotClaimUnrelatedTypes()
    {
        var factory = new StringEnumConverterFactory();

        Assert.That(factory.CanConvert(typeof(StateEnum)), Is.True);
        Assert.That(factory.CanConvert(typeof(UnannotatedEnum)), Is.True);
        Assert.That(factory.CanConvert(typeof(string)), Is.False);
        Assert.That(factory.CanConvert(typeof(Order)), Is.False);
    }
}