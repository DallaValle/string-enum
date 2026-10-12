# String Enum

This small library provides an easy and clean way to create ```string``` based on ```enum``` without losing the standard functions provided by ```enum```.

[![CI](https://github.com/DallaValle/string-enum/actions/workflows/ci.yml/badge.svg)](https://github.com/DallaValle/string-enum/actions/workflows/ci.yml)

## Install

```
dotnet add package DallaValle.StringEnum
```

Targets ```netstandard2.0``` and ```net8.0```, so it runs on .NET Framework 4.6.1+, .NET Core 2.0+ and every modern .NET.

## Usage example

```C#
using System.Text.Json.Serialization;

public class Order
{
    public StateEnum? State { get; set; }
}

[JsonConverter(typeof(StringEnumConverterFactory))]
public class StateEnum : StringEnum<StateEnum>
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
```

The type argument has to be the deriving type itself.
Closing the base class over a different enum is rejected when the first member is constructed, because otherwise the type would silently inherit that other enum's values.

### Parsing

```C#
var result = StateEnum.Parse("created");
Assert.AreEqual(StateEnum.Created, result);
```

```Parse``` throws ```ArgumentException``` for an undeclared value.
```TryParse``` returns ```false``` instead.
Both are case sensitive and both are backed by a dictionary that is built once per enum type, so parsing does not pay for reflection on every call.

```C#
if (StateEnum.TryParse(input, out var state))
{
    // state is not null here
}
```

### Printing

```C#
var myEnum = StateEnum.Created;
Assert.AreEqual("created", (string)myEnum);
Assert.AreEqual("created", myEnum.Value);
Assert.AreEqual("created", myEnum.ToString());
```

### Comparing

Equality is by value, not by reference, so an instance you parsed or constructed yourself compares equal to the declared member.
That also makes the members safe to use as ```Dictionary``` keys and in a ```HashSet```.

```C#
Assert.IsTrue(StateEnum.Parse("created") == StateEnum.Created);
Assert.IsTrue(new StateEnum("created") == StateEnum.Created);
Assert.IsTrue(StateEnum.Created == "created");

var seen = new HashSet<StateEnum> { StateEnum.Created };
Assert.IsTrue(seen.Contains(StateEnum.Parse("created")));
```

```StringEnum<T>``` also implements ```IComparable<T>``` by ordinal comparison of the underlying string, so the members sort and can be used in a ```SortedDictionary```.

### Listing the values

```GetValues()``` returns every declared member in declaration order.

```C#
foreach (var state in StateEnum.GetValues())
{
    Console.WriteLine(state.Value);
}
```

### json

Register the converter once on the enum class and every property, root value and dictionary key serializes as a plain string:

```C#
[JsonConverter(typeof(StringEnumConverterFactory))]
public class StateEnum : StringEnum<StateEnum> { /* ... */ }
```

```C#
var result = JsonSerializer.Serialize(new Order { State = StateEnum.Created });
Assert.AreEqual("{\"State\":\"created\"}", result);
```

```C#
var result = JsonSerializer.Deserialize<Order>("{\"State\":\"created\"}")!;
Assert.AreEqual(result.State, StateEnum.Created);
```

If you would rather not annotate the enum classes at all, register the factory once on your options instead:

```C#
var options = new JsonSerializerOptions { Converters = { new StringEnumConverterFactory() } };
```

```StringEnumConverter<T>``` is still available for a single property when you need to override whatever is registered further up:

```C#
public class Order
{
    [JsonConverter(typeof(StringEnumConverter<StateEnum>))]
    public StateEnum? State { get; set; }
}
```

An undeclared value throws ```JsonException``` rather than ```ArgumentException```, so the usual ```catch (JsonException)``` around ```Deserialize``` behaves as expected.
```null``` round trips as ```null```.

## Why? What is missing in C\#

From scratch with ```enum``` we don't have an easy way to assign a list of string values.

Unfortunately in csharp [enum](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum) cannot be strings:

```C#
public enum State
{
    Created = "created", //Cannot implicitly convert type 'string' to 'int'
...
```

Compile time error!

### Goal

- Have a standard way for enum fields that are **a defined string list**
- Clean code: No spread custom mapping logic extension in different locations
- Save enum in a readable way
- Serialize deserialize as json correctly

#### Solutions

- [x] Static readonly **+ base class**
- [ ] Custom Enumeration class: in this scenario enum doesn't have any order
- [ ] Attribute and extension method (with custom mapping): can be the solution but not in clean standard way

The base class ```StringEnum<T>``` provide the methods and functionality to have the same enum functionality

#### Why this implementation

I intentionally used ```static readonly``` instead of ```const```.
According with [dotnet/aspnetcore/HttpMethods.cs](https://github.com/dotnet/aspnetcore/blob/main/src/Http/Http.Abstractions/src/HttpMethods.cs):

The ```const``` values would be embedded into each assembly that used them and each consuming assembly would have a different ```string``` instance.
Using ```static readonly``` means that all consumers get these exact same ```string``` instance, which means the **ReferenceEquals** checks below work and allow us to optimize comparisons when these constants are used.

Equality here does not depend on that, though.
```StringEnum<T>``` compares by value with a reference-equality fast path, so a member you parsed or constructed yourself still equals the declared one.

### The same pattern in .NET itself: HTTP methods and status codes

[Microsoft.AspNetCore.Http.Abstractions](https://www.nuget.org/packages/Microsoft.AspNetCore.Http.Abstractions) is a good illustration of when this library is needed and when it is not, because it holds both cases side by side.

**HTTP methods are strings, so they get exactly this pattern.**
[HttpMethods.cs](https://github.com/dotnet/aspnetcore/blob/main/src/Http/Http.Abstractions/src/HttpMethods.cs) declares them as ```public static readonly string``` and then hand rolls the enum-like members around them:

```C#
public static readonly string Get = "GET";
public static readonly string Post = "POST";
```

alongside ```IsGet(method)```, ```Equals(methodA, methodB)``` and ```GetCanonicalizedValue(method)```.
That is the same shape as ```StringEnum<T>```, written out by hand for one specific set of values.
Every string-valued set you own needs that boilerplate written again, which is exactly what this library removes.

**HTTP status codes are integers, so they do not need it.**
[StatusCodes.cs](https://github.com/dotnet/aspnetcore/blob/main/src/Http/Http.Abstractions/src/StatusCodes.cs) uses ```public const int``` and [System.Net.HttpStatusCode](https://learn.microsoft.com/en-us/dotnet/api/system.net.httpstatuscode) is a plain ```enum```:

```C#
public const int Status200OK = 200;
public const int Status404NotFound = 404;
```

A C\# ```enum``` already backs onto an integer, so status codes have a first class language feature and ```const int``` is enough.
The gap this library fills is the string case, and the contrast between those two files is the clearest statement of it in the framework itself.

## Contributing

```
dotnet build
dotnet test
```

CI runs the same two commands on every push and pull request, plus ```dotnet pack```.