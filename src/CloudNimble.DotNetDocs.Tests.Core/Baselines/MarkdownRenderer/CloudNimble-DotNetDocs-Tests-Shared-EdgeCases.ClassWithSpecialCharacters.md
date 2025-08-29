# ClassWithSpecialCharacters

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.EdgeCases

**Base Type:** System.Object

## Overview

A class with special characters in documentation: <, >, &, ", '.

## Examples

// Using generics: List<string>
            var list = new List<string>();
            if (x > 0 && y < 10) { }

## Constructors

### .ctor

```csharp
public .ctor()
```

## Methods

### MethodWithSpecialChars

```csharp
public string MethodWithSpecialChars(string input)
```

A method with special characters in docs: <T> generics.

**Parameters:**

- `input`: An input with "quotes" and 'apostrophes'.

