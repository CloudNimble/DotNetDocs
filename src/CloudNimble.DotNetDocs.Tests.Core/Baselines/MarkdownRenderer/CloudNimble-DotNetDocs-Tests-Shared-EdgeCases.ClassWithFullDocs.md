# ClassWithFullDocs

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.EdgeCases

**Base Type:** System.Object

## Overview

A class with comprehensive XML documentation tags.

## Examples

var fullDocs = new ClassWithFullDocs();
            fullDocs.ComplexMethod("test", 42);

## Constructors

### .ctor

```csharp
public .ctor()
```

## Properties

### Value

```csharp
public string Value { get; set; }
```

Gets or sets the value property.

## Methods

### ComplexMethod

```csharp
public string ComplexMethod(string text, int number)
```

A complex method with full documentation.

**Parameters:**

- `text`: The text parameter to process.
- `number`: The number to use in processing.

**Examples:**

var result = ComplexMethod("hello", 5);
            Console.WriteLine(result);

