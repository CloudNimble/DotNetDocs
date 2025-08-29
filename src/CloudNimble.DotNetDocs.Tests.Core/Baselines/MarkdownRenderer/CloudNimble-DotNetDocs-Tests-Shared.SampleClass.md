# SampleClass

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared

**Base Type:** System.Object

## Overview

A sample class for testing documentation generation.

## Constructors

### .ctor

```csharp
public .ctor()
```

## Properties

### Name

```csharp
public string Name { get; set; }
```

Gets or sets the name.

### Value

```csharp
public int Value { get; set; }
```

Gets or sets the value.

## Methods

### DoSomething

```csharp
public string DoSomething(string input)
```

Performs a sample operation.

**Parameters:**

- `input`: The input parameter.

### GetDisplay

```csharp
public string GetDisplay()
```

Gets the display value.

### MethodWithOptional

```csharp
public string MethodWithOptional(string required, int optional)
```

Method with optional parameter.

**Parameters:**

- `required`: Required parameter.
- `optional`: Optional parameter with default value.

### MethodWithParams

```csharp
public int MethodWithParams(int[] values)
```

Method with params array.

**Parameters:**

- `values`: Variable number of values.

