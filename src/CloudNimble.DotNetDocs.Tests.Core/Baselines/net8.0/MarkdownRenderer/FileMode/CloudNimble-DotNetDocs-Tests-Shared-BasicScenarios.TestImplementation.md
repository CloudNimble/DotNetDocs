# TestImplementation

## Definition

**Assembly:** CloudNimble.DotNetDocs.Tests.Shared.dll

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios

**Inheritance:** System.Object

## Syntax

```csharp
CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.TestImplementation
```

## Summary

A test implementation of ITestInterface.

## Remarks

This class implements ITestInterface to demonstrate interface member inheritance
            in documentation generation.

## Constructors

### .ctor

#### Syntax

```csharp
public TestImplementation()
```

### .ctor

#### Syntax

```csharp
public Object()
```

## Properties

### TestValue

Gets the test value.

#### Syntax

```csharp
public string TestValue { get; }
```

#### Property Value

Type: `string`

## Methods

### AdditionalMethod

An additional method specific to the implementation.

#### Syntax

```csharp
public void AdditionalMethod()
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

### TestMethod

Performs a test operation.

#### Syntax

```csharp
public void TestMethod()
```

### ToString

#### Syntax

```csharp
public virtual string ToString()
```

#### Returns

Type: `string?`

## Related APIs

- CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.ITestInterface

