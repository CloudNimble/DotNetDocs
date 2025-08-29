# ClassWithProperties

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios

**Base Type:** System.Object

## Overview

A class demonstrating various property documentation scenarios.

## Constructors

### .ctor

```csharp
public .ctor()
```

## Properties

### Id

```csharp
public int Id { get; }
```

Gets the read-only identifier.

### Name

```csharp
public string Name { get; set; }
```

Gets or sets the name.

### Value

```csharp
public double Value { get; private set; }
```

Gets or sets the value with a private setter.

## Methods

### UpdateValue

```csharp
public void UpdateValue(double newValue)
```

Updates the value property.

**Parameters:**

- `newValue`: The new value to set.

