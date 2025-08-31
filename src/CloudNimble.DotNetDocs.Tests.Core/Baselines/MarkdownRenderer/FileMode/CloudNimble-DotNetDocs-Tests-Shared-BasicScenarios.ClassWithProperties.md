# ClassWithProperties

## Definition

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios
**Assembly:** CloudNimble.DotNetDocs.Tests.Shared
**Inheritance:** System.Object

## Syntax

```csharp
CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.ClassWithProperties
```

## Summary

A class demonstrating various property documentation scenarios.

## Remarks

This class contains properties with different access modifiers and documentation styles.

## Constructors

### .ctor

#### Syntax

```csharp
public ClassWithProperties()
```

## Properties

### Id

Gets the read-only identifier.

#### Syntax

```csharp
public int Id
```

#### Returns

Type: `int`

#### Property Value

Type: `int`

#### Remarks

This property can only be read, not written to.

### Name

Gets or sets the name.

#### Syntax

```csharp
public string Name
```

#### Returns

Type: `string`

#### Property Value

Type: `string`

#### Remarks

This is a standard public property with get and set accessors.

### Value

Gets or sets the value with a private setter.

#### Syntax

```csharp
public double Value
```

#### Returns

Type: `double`

#### Property Value

Type: `double`

#### Remarks

This property can be read publicly but only set within the class.

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

