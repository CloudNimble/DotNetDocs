# ClassWithFullDocs

## Definition

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.EdgeCases

**Assembly:** CloudNimble.DotNetDocs.Tests.Shared.dll

**Inheritance:** System.Object

## Syntax

```csharp
CloudNimble.DotNetDocs.Tests.Shared.EdgeCases.ClassWithFullDocs
```

## Summary

A class with comprehensive XML documentation tags.

## Remarks

This class demonstrates all available XML documentation tags.It includes multiple paragraphs in the remarks section.

## Examples

var fullDocs = new ClassWithFullDocs();
            fullDocs.ComplexMethod("test", 42);

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

var result = ComplexMethod("hello", 5);
            Console.WriteLine(result);

#### Remarks

This method performs complex processing.First, it validates the input.Then, it processes the data.Finally, it returns the result.

#### See Also

- System.String.Format(System.String,System.Object[])

## See Also

- CloudNimble.DotNetDocs.Tests.Shared.EdgeCases.ClassWithMinimalDocs
- System.String

