# ClassWithFullDocs

## Definition

**Assembly:** CloudNimble.DotNetDocs.Tests.Shared.dll

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.EdgeCases

**Inheritance:** System.Object

## Syntax

```csharp
CloudNimble.DotNetDocs.Tests.Shared.EdgeCases.ClassWithFullDocs
```

## Summary

A class with comprehensive XML documentation tags.

## Remarks

<para>This class demonstrates all available XML documentation tags.</para><para>It includes multiple paragraphs in the remarks section.</para>

## Examples

<code>
var fullDocs = new ClassWithFullDocs();
fullDocs.ComplexMethod("test", 42);
</code>

## Constructors

### .ctor

#### Syntax

```csharp
public ClassWithFullDocs()
```

## Properties

### Value

Gets or sets the value property.

#### Syntax

```csharp
public string Value { get; set; }
```

#### Property Value

Type: `string`
The current value as a string.

#### Remarks

This property stores important data.

## Methods

### ComplexMethod

A complex method with full documentation.

#### Syntax

```csharp
public string ComplexMethod(string text, int number)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `text` | `string` | The text parameter to process. |
| `number` | `int` | The number to use in processing. |

#### Returns

Type: `string`
A processed result string.

#### Exceptions

| Exception | Description |
|-----------|-------------|
| `ArgumentNullException` | Thrown when text is null. |
| `ArgumentOutOfRangeException` | Thrown when number is negative. |

#### Examples

<code>
var result = ComplexMethod("hello", 5);
Console.WriteLine(result);
</code>

#### Remarks

<para>This method performs complex processing.</para><list type="bullet">
  <item>
    <description>First, it validates the input.</description>
  </item>
  <item>
    <description>Then, it processes the data.</description>
  </item>
  <item>
    <description>Finally, it returns the result.</description>
  </item>
</list>

#### See Also

- `Object[])`

## See Also

- `ClassWithMinimalDocs`
- `String`

