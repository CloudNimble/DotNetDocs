# ClassWithProperties

## Definition

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios
**Assembly:** CloudNimble.DotNetDocs.Tests.Shared
**Inheritance:** System.Object

## Syntax

```csharp
public class ClassWithProperties : System.Object
```

## Description

A class demonstrating various property documentation scenarios.

## Constructors

### .ctor

#### Syntax

```csharp
public .ctor()
```

## Properties

### Id

Gets the read-only identifier.

#### Syntax

```csharp
public int Id { get; }
```

#### Property Value

Type: `int`

### Name

Gets or sets the name.

#### Syntax

```csharp
public string Name { get; set; }
```

#### Property Value

Type: `string`

### Value

Gets or sets the value with a private setter.

#### Syntax

```csharp
public double Value { get; private set; }
```

#### Property Value

Type: `double`

## Methods

### UpdateValue

Updates the value property.

#### Syntax

```csharp
public void UpdateValue(double newValue)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `newValue` | `double` | The new value to set. |

