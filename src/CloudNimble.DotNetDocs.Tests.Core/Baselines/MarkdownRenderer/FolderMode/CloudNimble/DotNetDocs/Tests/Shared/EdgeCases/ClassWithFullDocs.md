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



This class demonstrates all available XML documentation tags.



It includes multiple paragraphs in the remarks section.



## Examples


```csharp
var fullDocs = new ClassWithFullDocs();
fullDocs.ComplexMethod("test", 42);
```


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


```csharp
var result = ComplexMethod("hello", 5);
Console.WriteLine(result);
```


#### Remarks



This method performs complex processing.




#### See Also

- [Object[])](https://learn.microsoft.com/dotnet/api/system.string.format(system.string,system.object[]))

## See Also

- [ClassWithMinimalDocs](ClassWithMinimalDocs)
- [String](https://learn.microsoft.com/dotnet/api/system.string)

