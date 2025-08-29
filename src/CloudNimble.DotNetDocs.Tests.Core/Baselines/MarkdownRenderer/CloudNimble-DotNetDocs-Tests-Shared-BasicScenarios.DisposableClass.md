# DisposableClass

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios

**Base Type:** System.Object

## Overview

A class that implements IDisposable for testing interface documentation.

## Examples

using (var disposable = new DisposableClass())
            {
                disposable.UseResource();
            }

## Constructors

### .ctor

```csharp
public .ctor()
```

## Properties

### ResourceName

```csharp
public string ResourceName { get; set; }
```

Gets or sets the resource name.

## Methods

### Dispose

```csharp
public void Dispose()
```

Disposes the resources used by this instance.

### UseResource

```csharp
public void UseResource()
```

Uses the resource.

## Related APIs

- System.IDisposable

