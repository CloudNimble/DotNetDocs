# DerivedClass

## Definition

**Assembly:** CloudNimble.DotNetDocs.Tests.Shared.dll

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios

**Inheritance:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.BaseClass

## Syntax

```csharp
CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.DerivedClass
```

## Summary

A derived class for testing inheritance documentation.

## Remarks

This class inherits from BaseClass and overrides some members.

## Examples

<code>
var derived = new DerivedClass();
var result = derived.VirtualMethod();
</code>

## Constructors

### .ctor

#### Syntax

```csharp
public DerivedClass()
```

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

Gets or sets the base property with overridden behavior.

#### Syntax

```csharp
public override string BaseProperty { get; set; }
```

#### Property Value

Type: `string`

#### Remarks

This property overrides the base implementation.

### BaseProperty

Gets or sets the base property.

#### Syntax

```csharp
public virtual string BaseProperty { get; set; }
```

#### Property Value

Type: `string`

### DerivedProperty

Gets or sets the derived property.

#### Syntax

```csharp
public string DerivedProperty { get; set; }
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

### DerivedMethod

An additional method in the derived class.

#### Syntax

```csharp
public void DerivedMethod()
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

Overrides the virtual method from the base class.

#### Syntax

```csharp
public override string VirtualMethod()
```

#### Returns

Type: `string`
A string indicating the derived implementation.

#### Remarks

This method provides custom behavior for the derived class.

### VirtualMethod

A virtual method that can be overridden.

#### Syntax

```csharp
public virtual string VirtualMethod()
```

#### Returns

Type: `string`
A string value.

