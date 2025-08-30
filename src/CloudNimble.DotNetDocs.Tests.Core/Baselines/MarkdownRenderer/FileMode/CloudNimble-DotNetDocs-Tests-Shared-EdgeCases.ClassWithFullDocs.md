# ClassWithFullDocs

## Definition

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.EdgeCases
**Assembly:** CloudNimble.DotNetDocs.Tests.Shared
**Inheritance:** System.Object

## Syntax

```csharp
public class ClassWithFullDocs : System.Object
```

## Description

A class with comprehensive XML documentation tags.

## Examples

var fullDocs = new ClassWithFullDocs();
            fullDocs.ComplexMethod("test", 42);

## Constructors

### .ctor

#### Syntax

```csharp
public .ctor()
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

#### Examples

var result = ComplexMethod("hello", 5);
            Console.WriteLine(result);

