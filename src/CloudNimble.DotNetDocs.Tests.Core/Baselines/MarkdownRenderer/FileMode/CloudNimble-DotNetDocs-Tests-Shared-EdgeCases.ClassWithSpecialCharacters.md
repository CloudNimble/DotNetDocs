# ClassWithSpecialCharacters

## Definition

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.EdgeCases
**Assembly:** CloudNimble.DotNetDocs.Tests.Shared
**Inheritance:** System.Object

## Syntax

```csharp
public class ClassWithSpecialCharacters : System.Object
```

## Description

A class with special characters in documentation: <, >, &, ", '.

## Examples

// Using generics: List<string>
            var list = new List<string>();
            if (x > 0 && y < 10) { }

## Constructors

### .ctor

#### Syntax

```csharp
public .ctor()
```

## Methods

### MethodWithSpecialChars

A method with special characters in docs: <T> generics.

#### Syntax

```csharp
public string MethodWithSpecialChars(string input)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `input` | `string` | An input with "quotes" and 'apostrophes'. |

#### Returns

Type: `string`

