using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace StringEnum;

/// <summary>
/// Base class that turns a set of <c>public static readonly</c> fields into a string-valued enum,
/// keeping the members that <c>enum</c> would have given you: <see cref="GetValues"/>,
/// <see cref="Parse"/>, <see cref="TryParse"/>, value equality, ordering and hashing.
/// </summary>
/// <typeparam name="TEnum">
/// The deriving type itself, e.g. <c>class State : StringEnum&lt;State&gt;</c>. Passing any other type
/// is rejected when the first member is constructed, because the members would otherwise be read off
/// an unrelated enum.
/// </typeparam>
public abstract class StringEnum<TEnum> : IEquatable<TEnum>, IComparable<TEnum>, IComparable
    where TEnum : StringEnum<TEnum>
{
    // Reflection is the only way to find the declared fields, so it runs once per closed generic type
    // and every later lookup is a dictionary hit. Deferred rather than a static initializer: reading
    // those fields runs TEnum's own static constructor, and driving that from ours would be a cycle.
    private static readonly Lazy<Members> LazyMembers = new(Discover);

    /// <summary>The underlying string value, and the form used for JSON and <see cref="ToString"/>.</summary>
    public string Value { get; }

    /// <summary>Declares one enum member.</summary>
    /// <param name="value">The underlying string. Must be unique within <typeparamref name="TEnum"/>.</param>
    protected StringEnum(string value)
    {
        // A derived type that closes the base over someone else's enum would silently inherit that
        // enum's values, so refuse it here rather than hand back wrong data later.
        if (this is not TEnum)
        {
            throw new InvalidOperationException(
                $"'{GetType()}' must close its base class over itself: StringEnum<{GetType().Name}>, not StringEnum<{typeof(TEnum).Name}>.");
        }

        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>All declared members, in declaration order.</summary>
    public static IReadOnlyList<TEnum> GetValues() => LazyMembers.Value.Values;

    /// <summary>Returns the member with the given value.</summary>
    /// <exception cref="ArgumentException">No member declares <paramref name="value"/>.</exception>
    public static TEnum Parse(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return LazyMembers.Value.ByValue.TryGetValue(value, out var found)
            ? found
            : throw new ArgumentException(
                $"The parameter '{value}' it is not defined within the possible values of the enum", nameof(value));
    }

    /// <summary>Returns the member with the given value, or <see langword="false"/> if there is none.</summary>
    public static bool TryParse(string? value, [NotNullWhen(true)] out TEnum? stringEnum)
    {
        if (value is null)
        {
            stringEnum = null;
            return false;
        }

        return LazyMembers.Value.ByValue.TryGetValue(value, out stringEnum);
    }

    /// <summary>Compares by <see cref="Value"/>, so a parsed member equals the declared one.</summary>
    public bool Equals(TEnum? other) =>
        other is not null && (ReferenceEquals(this, other) || string.Equals(Value, other.Value, StringComparison.Ordinal));

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TEnum other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    /// <summary>Orders by <see cref="Value"/> using an ordinal comparison.</summary>
    public int CompareTo(TEnum? other) =>
        other is null ? 1 : string.Compare(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Unwraps the member to its underlying string.</summary>
    public static implicit operator string(StringEnum<TEnum> input) =>
        input is null ? throw new ArgumentNullException(nameof(input)) : input.Value;

    /// <summary>Compares by <see cref="Value"/>. Both operands may be <see langword="null"/>.</summary>
    public static bool operator ==(StringEnum<TEnum>? left, StringEnum<TEnum>? right) =>
        left is null ? right is null : left.Equals(right as TEnum);

    /// <summary>The negation of <see cref="op_Equality"/>.</summary>
    public static bool operator !=(StringEnum<TEnum>? left, StringEnum<TEnum>? right) => !(left == right);

    int IComparable.CompareTo(object? obj) =>
        obj switch
        {
            null => 1,
            TEnum other => CompareTo(other),
            _ => throw new ArgumentException($"Object must be of type {typeof(TEnum).Name}.", nameof(obj)),
        };

    private static Members Discover()
    {
        var enumType = typeof(TEnum);

        // Ordering by MetadataToken restores declaration order; Type.GetFields makes no such promise.
        var values = enumType
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => enumType.IsAssignableFrom(field.FieldType))
            .OrderBy(field => field.MetadataToken)
            .Select(field => field.GetValue(null))
            .OfType<TEnum>()
            .ToList();

        var byValue = new Dictionary<string, TEnum>(values.Count, StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (byValue.ContainsKey(value.Value))
            {
                throw new InvalidOperationException(
                    $"'{enumType.Name}' declares the value '{value.Value}' more than once, so it cannot be parsed unambiguously.");
            }

            byValue.Add(value.Value, value);
        }

        return new Members(new ReadOnlyCollection<TEnum>(values), byValue);
    }

    private sealed class Members
    {
        public Members(IReadOnlyList<TEnum> values, Dictionary<string, TEnum> byValue)
        {
            Values = values;
            ByValue = byValue;
        }

        public IReadOnlyList<TEnum> Values { get; }

        public Dictionary<string, TEnum> ByValue { get; }
    }
}