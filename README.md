# String Enum

This small library provides an easy and clean way to create ```string``` based on ```enum``` without losing the standard functions provided by ```enum```.

## Usage example

```C#
public class Order
{
    [JsonConverter(typeof(StringEnumConverter<StateEnum>))]
    public StateEnum? State { get; set; }
}

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

### Parsing

```C#
var result = StateEnum.Parse("created");
Assert.AreEqual(StateEnum.Created, result);
```

### Printing

```C#
var myEnum = StateEnum.Created;
Assert.AreEqual("created", (string)myEnum);
```

### json

To have the correct behavior we had to add an attribute in the ```Order``` class

```C#
public class Order
{
    [JsonConverter(typeof(StringEnumConverter<StateEnum>))]
    public StateEnum State { get; set; } = null!;
}
```

but now we can serialize and deserialize to ```json string``` easily

```C#
var result = JsonSerializer.Serialize(new Order { State = StateEnum.Created });
Assert.AreEqual("{\"State\":\"created\"}", result);
```

```C#
var result = JsonSerializer.Deserialize<Order>("{\"State\":\"created\"}")!;
Assert.AreEqual(result.State, StateEnum.Created);
```

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

I intentionally used ```static readonly``` instead of ```const```. According with [dotnet/aspnetcore/HttpMethods.cs](https://github.com/dotnet/aspnetcore/blob/665ea2f868d916693ad1e243959349fa8e6d9647/src/Http/Http.Abstractions/src/HttpMethods.cs):

The ```const``` values would be embedded into each assembly that used them and each consuming assembly would have a different ```string``` instance.
Using ```static readonly``` means that all consumers get these exact same ```string``` instance, which means the **ReferenceEquals** checks below work and allow us to optimize comparisons when these constants are used.
