# MixedAccessClass

## Definition

**Assembly:** CloudNimble.DotNetDocs.Tests.Shared.dll

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.AccessModifiers

**Inheritance:** System.Object

## Syntax

```csharp
CloudNimble.DotNetDocs.Tests.Shared.AccessModifiers.MixedAccessClass
```

## Summary

A class with members of various access modifiers for testing filtering.

## Remarks

This class tests the IncludedMembers filtering functionality.

## Constructors

### .ctor

#### Syntax

```csharp
public MixedAccessClass()
```

### .ctor

#### Syntax

```csharp
public Object()
```

## Properties

### PublicProperty

Gets or sets the public property.

#### Syntax

```csharp
public string PublicProperty { get; set; }
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

### PublicMethod

A public method.

#### Syntax

```csharp
public string PublicMethod()
```

#### Returns

Type: `string`
A string indicating this is a public method.

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

