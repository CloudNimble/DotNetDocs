# DerivedClass

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios

**Base Type:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.BaseClass

## Overview

A derived class for testing inheritance documentation.

## Examples

var derived = new DerivedClass();
            var result = derived.VirtualMethod();

## Constructors

### .ctor

```csharp
public .ctor()
```

## Properties

### BaseProperty

```csharp
public override string BaseProperty { get; set; }
```

Gets or sets the base property with overridden behavior.

### DerivedProperty

```csharp
public string DerivedProperty { get; set; }
```

Gets or sets the derived property.

## Methods

### DerivedMethod

```csharp
public void DerivedMethod()
```

An additional method in the derived class.

### VirtualMethod

```csharp
public override string VirtualMethod()
```

Overrides the virtual method from the base class.

