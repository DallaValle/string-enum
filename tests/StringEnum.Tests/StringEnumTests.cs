using NUnit.Framework;

namespace StringEnum.Tests;

public class StringEnumTests
{
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
        Assert.That(enum2, Is.EqualTo(enum1));
        Assert.That(enum1 == enum2, Is.True);
        var enum3 = StateEnum.Offered;
        Assert.That(enum3, Is.Not.EqualTo(enum1));
        Assert.That(enum3, Is.Not.EqualTo(enum2));
    }

    [Test]
    public void Equals_WithDistinctInstanceOfSameValue_ShouldBeEqual()
    {
        var declared = StateEnum.Created;
        var constructed = new StateEnum("created");

        Assert.That(ReferenceEquals(declared, constructed), Is.False, "the test needs two distinct instances");
        Assert.That(constructed, Is.EqualTo(declared));
        Assert.That(declared == constructed, Is.True);
        Assert.That(declared != constructed, Is.False);
        Assert.That(declared.Equals(constructed), Is.True);
        Assert.That(declared.Equals((object)constructed), Is.True);
    }

    [Test]
    public void GetHashCode_ShouldMatchEqualsForDistinctInstances()
    {
        // Equal values must hash alike, or set and dictionary lookups miss.
        Assert.That(new StateEnum("created").GetHashCode(), Is.EqualTo(StateEnum.Created.GetHashCode()));
        Assert.That(StateEnum.Offered.GetHashCode(), Is.Not.EqualTo(StateEnum.Created.GetHashCode()));
    }

    [Test]
    public void HashSet_ShouldFindMemberByValue()
    {
        var set = new HashSet<StateEnum> { StateEnum.Created };

        Assert.That(set.Contains(StateEnum.Created), Is.True);
        Assert.That(set.Contains(new StateEnum("created")), Is.True);
        Assert.That(set.Contains(StateEnum.Offered), Is.False);
    }

    [Test]
    public void Dictionary_ShouldUseMemberAsKey()
    {
        var counts = new Dictionary<StateEnum, int> { [StateEnum.Created] = 1 };

        Assert.That(counts[new StateEnum("created")], Is.EqualTo(1));
    }

    [Test]
    public void Operators_WithNull_ShouldNotThrow()
    {
        StateEnum? left = null;
        StateEnum? right = null;

        Assert.That(left == right, Is.True);
        Assert.That(left == StateEnum.Created, Is.False);
        Assert.That(StateEnum.Created == right, Is.False);
        Assert.That(StateEnum.Created != right, Is.True);
        Assert.That(StateEnum.Created.Equals(null), Is.False);
    }

    [Test]
    public void CompareTo_ShouldOrderByValue()
    {
        var sorted = new[] { StateEnum.Offered, StateEnum.Accepted, StateEnum.Created }
            .OrderBy(state => state)
            .Select(state => state.Value);

        Assert.That(sorted, Is.EqualTo(new[] { "accepted", "created", "offered" }));
    }

    [Test]
    public void ImplicitString_ShouldWorkAsExpected()
    {
        var myEnum = StateEnum.Created;
        Assert.That((string)myEnum, Is.EqualTo("created"));
    }

    [Test]
    public void ComparisonAgainstRawString_ShouldMatchValue()
    {
        Assert.That(StateEnum.Created == "created", Is.True);
        Assert.That(StateEnum.Created == "offered", Is.False);
    }

    [Test]
    public void ToString_ShouldWorkAsExpected()
    {
        var myEnum = StateEnum.Created;
        Assert.That(myEnum.ToString(), Is.EqualTo("created"));
    }

    [Test]
    public void Value_ShouldExposeUnderlyingString()
    {
        Assert.That(StateEnum.Created.Value, Is.EqualTo("created"));
    }

    [Test]
    public void Constructor_WithNullValue_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new StateEnum(null!));
    }

    [Test]
    public void GetValues_ShouldReturnAllEnumValuesInDeclarationOrder()
    {
        var values = StateEnum.GetValues();

        Assert.That(values.Count, Is.EqualTo(8));
        Assert.That(
            values.Select(state => state.Value),
            Is.EqualTo(new[]
            {
                "created",
                "customer_action_required",
                "internal_action_required",
                "offered",
                "accepted",
                "declined",
                "revoked",
                "expired",
            }));
    }

    [Test]
    public void Parse_WithExistingField_ShouldReturnFieldInstance()
    {
        var result = StateEnum.Parse("created");

        Assert.That(result, Is.EqualTo(StateEnum.Created));
        Assert.That(ReferenceEquals(result, StateEnum.Created), Is.True, "Parse should hand back the declared instance");
    }

    [Test]
    public void Parse_WithNotExistingValue_ShouldThrowException()
    {
        var exception = Assert.Throws<ArgumentException>(() => StateEnum.Parse("wrong-enum"));

        Assert.That(
            exception!.Message,
            Does.Contain("The parameter 'wrong-enum' it is not defined within the possible values of the enum"));
    }

    [Test]
    public void Parse_WithNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => StateEnum.Parse(null!));
    }

    [Test]
    public void TryParse_WithExistingField_ShouldReturnTrueAndFieldInstance()
    {
        var result = StateEnum.TryParse("created", out var myEnum);

        Assert.That(result, Is.True);
        Assert.That(myEnum, Is.EqualTo(StateEnum.Created));
    }

    [Test]
    public void TryParse_WithNotExistingValue_ShouldReturnFalse()
    {
        var result = StateEnum.TryParse("wrong-enum", out var myEnum);

        Assert.That(result, Is.False);
        Assert.That(myEnum, Is.Null);
    }

    [Test]
    public void TryParse_WithNull_ShouldReturnFalse()
    {
        var result = StateEnum.TryParse(null, out var myEnum);

        Assert.That(result, Is.False);
        Assert.That(myEnum, Is.Null);
    }

    [Test]
    public void Parse_IsCaseSensitive()
    {
        Assert.That(StateEnum.TryParse("CREATED", out _), Is.False);
    }
}