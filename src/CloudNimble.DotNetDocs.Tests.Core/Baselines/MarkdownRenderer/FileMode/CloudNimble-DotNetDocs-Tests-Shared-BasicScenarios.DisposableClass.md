# DisposableClass

## Definition

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios
**Assembly:** CloudNimble.DotNetDocs.Tests.Shared
**Inheritance:** System.Object
**Implements:** System.IDisposable

## Syntax

```csharp
public class DisposableClass : System.Object, System.IDisposable
```

## Description

A class that implements IDisposable for testing interface documentation.

## Examples

using (var disposable = new DisposableClass())
            {
                disposable.UseResource();
            }

## Constructors

### .ctor

#### Syntax

```csharp
public .ctor()
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

### UseResource

Uses the resource.

#### Syntax

```csharp
public void UseResource()
```

## Related APIs

- System.IDisposable

