# SimpleClass

## Definition

**Assembly:** CloudNimble.DotNetDocs.Tests.Shared.dll

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios

**Inheritance:** System.Object

## Syntax

```csharp
CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.SimpleClass
```

## Summary

A simple class for testing basic documentation extraction.

## Remarks

These are remarks about the SimpleClass. They provide additional context
            and information beyond what's in the summary.

## Examples

<code>
var simple = new SimpleClass();
simple.DoWork();
</code>

## Constructors

### .ctor

#### Syntax

```csharp
public SimpleClass()
```

### .ctor

#### Syntax

```csharp
public Object()
```

## Methods

### DoWork

Performs some work.

#### Syntax

```csharp
public void DoWork()
```

#### Remarks

This method doesn't actually do anything, but it has documentation.

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

### IsValid

Checks if a SimpleClass instance is valid.

#### Syntax

```csharp
public static bool IsValid(CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.SimpleClass instance)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `instance` | `CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.SimpleClass` | The SimpleClass instance. |

#### Returns

Type: `bool`
True if valid, otherwise false.

#### Remarks

This is a simple validation extension for demonstration purposes.

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

### ToDisplayString

Converts a SimpleClass instance to a display string.

#### Syntax

```csharp
public static string ToDisplayString(CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.SimpleClass instance)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `instance` | `CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.SimpleClass` | The SimpleClass instance. |

#### Returns

Type: `string`
A formatted string representation.

#### Examples

<code>
var simple = new SimpleClass();
var display = simple.ToDisplayString();
</code>

### ToString

#### Syntax

```csharp
public virtual string ToString()
```

#### Returns

Type: `string?`

