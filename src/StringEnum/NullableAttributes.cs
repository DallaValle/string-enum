#if NETSTANDARD2_0

// Polyfill so the netstandard2.0 build can carry the same nullability annotations as net8.0.
// Internal by design: the framework supplies the public versions everywhere else.
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class NotNullWhenAttribute : Attribute
{
    public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

    public bool ReturnValue { get; }
}

#endif