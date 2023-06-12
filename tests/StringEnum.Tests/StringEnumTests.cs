using System.Text.Json;
using System.Text.Json.Serialization;

using NUnit.Framework;
using NUnit.Framework.Internal;

namespace StringEnum.Tests;

public class StringEnumTests
{
    internal class Order
    {
        [JsonConverter(typeof(StringEnumConverter<StateEnum>))]
        public StateEnum? State { get; set; }
    }

    internal class StateEnum : StringEnum<StateEnum>
    {
        public static readonly StateEnum Created = new("created");
        public static readonly StateEnum CustomerActionRequired = new("customer_action_required");
        public static readonly StateEnum InternalActionRequired = new("internal_action_required");
        public static readonly StateEnum Offered = new("offered");
        public static readonly StateEnum Accepted = new("accepted");
        public static readonly StateEnum Declined = new("declined");
        public static readonly StateEnum Revoked = new("revoked");
        public static readonly StateEnum Expired = new("expired");

        public StateEnum(string value) : base(value) { }
    }

    [Test]
    public void Equals_ShouldWorkAsExpected()
    {
        var enum1 = StateEnum.Created;
        var enum2 = StateEnum.Created;
        Assert.AreEqual(enum1, enum2);
        Assert.IsTrue(enum1 == enum2);
        var enum3 = StateEnum.Offered;
        Assert.AreNotEqual(enum1, enum3);
        Assert.AreNotEqual(enum2, enum3);
    }

    [Test]
    public void ImplicitString_ShouldWorkAsExpected()
    {
        var myEnum = StateEnum.Created;
        Assert.AreEqual("created", (string)myEnum);
    }

    [Test]
    public void ToString_ShouldWorkAsExpected()
    {
        var myEnum = StateEnum.Created;
        Assert.AreEqual("created", myEnum.ToString());
    }

    [Test]
    public void GetValues_ShouldReturnAllEnumValues()
    {
        var values = StateEnum.GetValues().ToList();
        Assert.AreEqual(8, values.Count);
        Assert.AreEqual("created", (string)values[0]);
        Assert.AreEqual("customer_action_required", (string)values[1]);
        Assert.AreEqual("expired", (string)values[7]);
    }

    [Test]
    public void Parse_WithExistingField_ShouldReturnFieldInstance()
    {
        var result = StateEnum.Parse("created");
        Assert.AreEqual(StateEnum.Created, result);
    }

    [Test]
    public void Parse_WithNotExistingValue_ShouldThrowException()
    {
        Assert.Throws<ArgumentException>(
            () => StateEnum.Parse("wrong-enum"),
            "The parameter 'wrong-enum' it is not defined within the possible values of the enum");
    }

    [Test]
    public void TryParse_WithExistingField_ShouldReturnTrueAndFieldInstance()
    {
        var result = StateEnum.TryParse("created", out var myEnum);
        Assert.IsTrue(result);
        Assert.AreEqual(StateEnum.Created, myEnum);
    }

    [Test]
    public void TryParse_WithNotExistingValue_ShouldReturnFalse()
    {
        var result = StateEnum.TryParse("wrong-enum", out var myEnum);
        Assert.IsFalse(result);
        Assert.IsNull(myEnum);
    }

    [Test]
    public void GetHashCode_ShouldWorkAsExpected()
    {
        var hashCode1 = StateEnum.Created.GetHashCode();
        var hashCode2 = StateEnum.Offered.GetHashCode();
        Assert.AreEqual(StateEnum.Created.GetHashCode(), hashCode1);
        Assert.AreEqual(StateEnum.Offered.GetHashCode(), hashCode2);
        Assert.AreNotEqual(hashCode1, hashCode2);
    }

    [Test]
    public void StringEnumConverter_ShouldSerializeAsExpect()
    {
        var result = JsonSerializer.Serialize(new Order { State = StateEnum.Created });
        Assert.AreEqual("{\"State\":\"created\"}", result);
    }

    [Test]
    public void StringEnumConverter_ShouldDeserializeAsExpect()
    {
        var result = JsonSerializer.Deserialize<Order>("{\"State\":\"created\"}")!;
        Assert.AreEqual(result.State, StateEnum.Created);
    }
}