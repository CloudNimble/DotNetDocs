# ClassWithProperties

## Definition

**Assembly:** CloudNimble.DotNetDocs.Tests.Shared.dll

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios

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

### .ctor

#### Syntax

```csharp
public Object()
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

#### Remarks

This property can only be read, not written to.

### Name

Gets or sets the name.

#### Syntax

```csharp
public string Name { get; set; }
```

#### Property Value

Type: `string`

#### Remarks

This is a standard public property with get and set accessors.

### Value

Gets or sets the value with a private setter.

#### Syntax

```csharp
public double Value { get; private set; }
```

#### Property Value

Type: `double`

#### Remarks

This property can be read publicly but only set within the class.

## Methods

### Equals

#### Syntax

```csharp
public virtual bool Equals(object obj)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `obj` | `object?` | - |

#### Returns

Type: `bool`

### Equals

#### Syntax

```csharp
public static bool Equals(object objA, object objB)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `objA` | `object?` | - |
| `objB` | `object?` | - |

#### Returns

Type: `bool`

### GetHashCode

#### Syntax

```csharp
public virtual int GetHashCode()
```

#### Returns

Type: `int`

### GetType

#### Syntax

```csharp
public System.Type GetType()
```

#### Returns

Type: `System.Type`

### MemberwiseClone

#### Syntax

```csharp
protected object MemberwiseClone()
```

#### Returns

Type: `object`

### ReferenceEquals

#### Syntax

```csharp
public static bool ReferenceEquals(object objA, object objB)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `objA` | `object?` | - |
| `objB` | `object?` | - |

#### Returns

Type: `bool`

### ToString

#### Syntax

```csharp
public virtual string ToString()
```

#### Returns

Type: `string?`

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

