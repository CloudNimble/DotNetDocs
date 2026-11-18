# InternalClass

## Definition

**Assembly:** CloudNimble.DotNetDocs.Tests.Shared.dll

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.AccessModifiers

**Inheritance:** System.Object

## Syntax

```csharp
CloudNimble.DotNetDocs.Tests.Shared.AccessModifiers.InternalClass
```

## Summary

An internal class for testing internal type visibility.

## Remarks

This class should only be visible when internal members are included.

## Constructors

### .ctor

#### Syntax

```csharp
public InternalClass()
```

### .ctor

#### Syntax

```csharp
public Object()
```

## Properties

### InternalValue

Gets or sets the internal value.

#### Syntax

```csharp
internal int InternalValue { get; set; }
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

### GetDescription

A public method in an internal class.

#### Syntax

```csharp
public string GetDescription()
```

#### Returns

Type: `string`
A description string.

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
protected internal object MemberwiseClone()
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

