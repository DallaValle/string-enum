using NUnit.Framework;

namespace StringEnum.Tests;

public class StringEnumDeclarationTests
{
    internal class GoodEnum : StringEnum<GoodEnum>
    {
        public static readonly GoodEnum Only = new("only");

        public GoodEnum(string value) : base(value) { }
    }

    internal class DuplicateEnum : StringEnum<DuplicateEnum>
    {
        public static readonly DuplicateEnum First = new("same");
        public static readonly DuplicateEnum Second = new("same");

        public DuplicateEnum(string value) : base(value) { }
    }

    // Closes the base over an unrelated enum: the CRTP constraint alone cannot reject this.
    internal class MisdeclaredEnum : StringEnum<GoodEnum>
    {
        public MisdeclaredEnum(string value) : base(value) { }
    }

    internal class InstanceFieldEnum : StringEnum<InstanceFieldEnum>
    {
        public static readonly InstanceFieldEnum One = new("one");

        // A public instance field of the enum type used to break reflection over the declared members.
        public InstanceFieldEnum? Sibling = null;

        public InstanceFieldEnum(string value) : base(value) { }
    }

    [Test]
    public void DuplicateValues_ShouldThrowOnFirstUse()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => DuplicateEnum.GetValues());

        Assert.That(exception!.Message, Does.Contain("declares the value 'same' more than once"));
    }

    [Test]
    public void MisdeclaredEnum_ShouldThrowInsteadOfBorrowingAnotherEnumsValues()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new MisdeclaredEnum("x"));

        Assert.That(exception!.Message, Does.Contain("must close its base class over itself"));
    }

    [Test]
    public void PublicInstanceField_ShouldBeIgnoredByDiscovery()
    {
        var values = InstanceFieldEnum.GetValues();

        Assert.That(values.Count, Is.EqualTo(1));
        Assert.That(values[0].Value, Is.EqualTo("one"));
    }

    [Test]
    public void GetValues_ShouldReturnTheSameCachedInstanceEveryCall()
    {
        Assert.That(ReferenceEquals(GoodEnum.GetValues(), GoodEnum.GetValues()), Is.True);
    }
}