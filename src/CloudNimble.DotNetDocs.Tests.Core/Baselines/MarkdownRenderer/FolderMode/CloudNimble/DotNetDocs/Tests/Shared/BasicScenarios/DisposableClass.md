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


```csharp
using (var disposable = new DisposableClass())
{
    disposable.UseResource();
}
```


## Constructors

### .ctor

#### Syntax

```csharp
public DisposableClass()
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

