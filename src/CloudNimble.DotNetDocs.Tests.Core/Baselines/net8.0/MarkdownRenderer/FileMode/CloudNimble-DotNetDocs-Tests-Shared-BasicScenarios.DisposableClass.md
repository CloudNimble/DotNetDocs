# DisposableClass

## Definition

**Assembly:** CloudNimble.DotNetDocs.Tests.Shared.dll

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios

**Inheritance:** System.Object

## Syntax

```csharp
CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.DisposableClass
```

## Summary

A class that implements IDisposable for testing interface documentation.

## Remarks

This class demonstrates how interface implementation is documented.

## Examples

<code>
using (var disposable = new DisposableClass())
{
    disposable.UseResource();
}
</code>

## Constructors

### .ctor

#### Syntax

```csharp
public DisposableClass()
```

### .ctor

#### Syntax

```csharp
public Object()
```

## Properties

### ResourceName

Gets or sets the resource name.

#### Syntax

```csharp
public string ResourceName { get; set; }
```

#### Property Value

Type: `string`

## Methods

### Dispose

Disposes the resources used by this instance.

#### Syntax

```csharp
public void Dispose()
```

#### Remarks

Implements the IDisposable pattern.

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

### UseResource

Uses the resource.

#### Syntax

```csharp
public void UseResource()
```

#### Exceptions

| Exception | Description |
|-----------|-------------|
| `ObjectDisposedException` | Thrown if the object has been disposed. |

## Related APIs

- System.IDisposable

