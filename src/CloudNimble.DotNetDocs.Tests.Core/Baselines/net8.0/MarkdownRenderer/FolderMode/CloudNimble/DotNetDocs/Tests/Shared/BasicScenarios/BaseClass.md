# BaseClass

## Definition

**Assembly:** CloudNimble.DotNetDocs.Tests.Shared.dll

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios

**Inheritance:** System.Object

## Syntax

```csharp
CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.BaseClass
```

## Summary

A base class for testing inheritance documentation.

## Remarks

This class serves as the base for DerivedClass.

## Constructors

### .ctor

#### Syntax

```csharp
public BaseClass()
```

### .ctor

#### Syntax

```csharp
public Object()
```

## Properties

### BaseProperty

Gets or sets the base property.

#### Syntax

```csharp
public virtual string BaseProperty { get; set; }
```

#### Property Value

Type: `string`

## Methods

### BaseMethod

A method in the base class.

#### Syntax

```csharp
public void BaseMethod()
```

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

### VirtualMethod

A virtual method that can be overridden.

#### Syntax

```csharp
public virtual string VirtualMethod()
```

#### Returns

Type: `string`
A string value.

